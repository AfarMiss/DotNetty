﻿using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using DotNetty.Common.Internal;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    public class CompositeByteBuffer : AbstractRefByteBuffer, IEnumerable<IByteBuffer>
    {
        #region IByteBuffer

        private static readonly IList<IByteBuffer> EmptyList = new ReadOnlyCollection<IByteBuffer>(System.Array.Empty<IByteBuffer>());

        private class ComponentEntry
        {
            public readonly IByteBuffer Buffer;
            public readonly int Length;
            public int Offset;
            public int EndOffset;

            public ComponentEntry(IByteBuffer buffer)
            {
                this.Buffer = buffer;
                this.Length = buffer.ReadableBytes;
            }

            public void FreeIfNecessary() => this.Buffer.Release();
        }

        private static readonly ArraySegment<byte> EmptyNioBuffer = Unpooled.Empty.GetIoBuffer();

        private readonly IByteBufferAllocator allocator;
        private readonly List<ComponentEntry> components;
        private readonly int maxNumComponents;
        private bool freed;

        public CompositeByteBuffer(IByteBufferAllocator allocator, int maxNumComponents)
            : base(AbstractByteBufferAllocator.DefaultMaxCapacity)
        {
            Contract.Requires(allocator != null);
            Contract.Requires(maxNumComponents >= 2);

            this.allocator = allocator;
            this.maxNumComponents = maxNumComponents;
            this.components = NewList(maxNumComponents);
        }

        public CompositeByteBuffer(IByteBufferAllocator allocator, int maxNumComponents, params IByteBuffer[] buffers)
            : this(allocator, maxNumComponents, buffers, 0, buffers.Length)
        {
        }

        internal CompositeByteBuffer(IByteBufferAllocator allocator, int maxNumComponents, IByteBuffer[] buffers, int offset, int length)
            : base(AbstractByteBufferAllocator.DefaultMaxCapacity)
        {
            Contract.Requires(allocator != null);
            Contract.Requires(maxNumComponents >= 2);

            this.allocator = allocator;
            this.maxNumComponents = maxNumComponents;
            this.components = NewList(maxNumComponents);

            this.AddComponents0(false, 0, buffers, offset, length);
            this.ConsolidateIfNeeded();
            this.SetIndex0(0, GetCapacity(this.components));
        }

        public CompositeByteBuffer(IByteBufferAllocator allocator, int maxNumComponents, IEnumerable<IByteBuffer> buffers)
            : base(AbstractByteBufferAllocator.DefaultMaxCapacity)
        {
            Contract.Requires(allocator != null);
            Contract.Requires(maxNumComponents >= 2);

            this.allocator = allocator;
            this.maxNumComponents = maxNumComponents;
            this.components = NewList(maxNumComponents);

            this.AddComponents0(false, 0, buffers);
            this.ConsolidateIfNeeded();
            this.SetIndex0(0, GetCapacity(this.components));
        }

        private static List<ComponentEntry> NewList(int maxNumComponents) => new List<ComponentEntry>(Math.Min(AbstractByteBufferAllocator.DefaultMaxComponents, maxNumComponents));

        // Special constructor used by WrappedCompositeByteBuf
        internal CompositeByteBuffer(IByteBufferAllocator allocator) : base(int.MaxValue)
        {
            this.allocator = allocator;
            this.maxNumComponents = 0;
            this.components = new List<ComponentEntry>(0);
        }

        /// <summary>
        ///     Add the given {@link IByteBuffer}.
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param buffer the {@link IByteBuffer} to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponent(IByteBuffer buffer) => this.AddComponent(false, buffer);

        /// <summary>
        ///     Add the given {@link IByteBuffer}s.
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param buffers the {@link IByteBuffer}s to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponents(params IByteBuffer[] buffers) => this.AddComponents(false, buffers);

        /// <summary>
        ///     Add the given {@link IByteBuffer}s.
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param buffers the {@link IByteBuffer}s to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponents(IEnumerable<IByteBuffer> buffers) => this.AddComponents(false, buffers);

        /// <summary>
        ///     Add the given {@link IByteBuffer} on the specific index.
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param cIndex the index on which the {@link IByteBuffer} will be added
        ///     @param buffer the {@link IByteBuffer} to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponent(int cIndex, IByteBuffer buffer) => this.AddComponent(false, cIndex, buffer);

        public virtual CompositeByteBuffer AddComponent(bool increaseWriterIndex, IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);
            this.AddComponent0(increaseWriterIndex, this.components.Count, buffer);
            this.ConsolidateIfNeeded();
            return this;
        }

        public virtual CompositeByteBuffer AddComponents(bool increaseWriterIndex, params IByteBuffer[] buffers)
        {
            this.AddComponents0(increaseWriterIndex, this.components.Count, buffers, 0, buffers.Length);
            this.ConsolidateIfNeeded();
            return this;
        }

        public virtual CompositeByteBuffer AddComponents(bool increaseWriterIndex, IEnumerable<IByteBuffer> buffers)
        {
            this.AddComponents0(increaseWriterIndex, this.components.Count, buffers);
            this.ConsolidateIfNeeded();
            return this;
        }

        public virtual CompositeByteBuffer AddComponent(bool increaseWriterIndex, int cIndex, IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);
            this.AddComponent0(increaseWriterIndex, cIndex, buffer);
            this.ConsolidateIfNeeded();
            return this;
        }

        private int AddComponent0(bool increaseWriterIndex, int cIndex, IByteBuffer buffer)
        {
            bool wasAdded = false;
            try
            {
                this.CheckComponentIndex(cIndex);

                int readableBytes = buffer.ReadableBytes;

                // No need to consolidate - just add a component to the list.
                var c = new ComponentEntry(buffer.Slice());
                if (cIndex == this.components.Count)
                {
                    this.components.Add(c);
                    wasAdded = true;
                    if (cIndex == 0)
                    {
                        c.EndOffset = readableBytes;
                    }
                    else
                    {
                        ComponentEntry prev = this.components[cIndex - 1];
                        c.Offset = prev.EndOffset;
                        c.EndOffset = c.Offset + readableBytes;
                    }
                }
                else
                {
                    this.components.Insert(cIndex, c);
                    wasAdded = true;
                    if (readableBytes != 0)
                    {
                        this.UpdateComponentOffsets(cIndex);
                    }
                }
                if (increaseWriterIndex)
                {
                    this.SetWriterIndex(this.WriterIndex + buffer.ReadableBytes);
                }
                return cIndex;
            }
            finally
            {
                if (!wasAdded)
                {
                    buffer.Release();
                }
            }
        }

        /// <summary>
        ///     Add the given {@link IByteBuffer}s on the specific index
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param cIndex the index on which the {@link IByteBuffer} will be added.
        ///     @param buffers the {@link IByteBuffer}s to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponents(int cIndex, params IByteBuffer[] buffers)
        {
            this.AddComponents0(false, cIndex, buffers, 0, buffers.Length);
            this.ConsolidateIfNeeded();
            return this;
        }

        private int AddComponents0(bool increaseWriterIndex, int cIndex, IByteBuffer[] buffers, int offset, int len)
        {
            Contract.Requires(buffers != null);
            int i = offset;
            try
            {
                this.CheckComponentIndex(cIndex);

                // No need for consolidation
                while (i < len)
                {
                    // Increment i now to prepare for the next iteration and prevent a duplicate release (addComponent0
                    // will release if an exception occurs, and we also release in the finally block here).
                    var buffer = buffers[i++];
                    if (buffer == null) break;

                    cIndex = this.AddComponent0(increaseWriterIndex, cIndex, buffer) + 1;
                    int size = this.components.Count;
                    if (cIndex > size)
                    {
                        cIndex = size;
                    }
                }

                return cIndex;
            }
            finally
            {
                for (; i < len; ++i)
                {
                    var buffer = buffers[i];
                    if (buffer != null)
                    {
                        try
                        {
                            buffer.Release();
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Add the given {@link ByteBuf}s on the specific index
        ///     Be aware that this method does not increase the {@code writerIndex} of the {@link CompositeByteBuffer}.
        ///     If you need to have it increased you need to handle it by your own.
        ///     @param cIndex the index on which the {@link IByteBuffer} will be added.
        ///     @param buffers the {@link IByteBuffer}s to add
        /// </summary>
        public virtual CompositeByteBuffer AddComponents(int cIndex, IEnumerable<IByteBuffer> buffers)
        {
            this.AddComponents0(false, cIndex, buffers);
            this.ConsolidateIfNeeded();
            return this;
        }

        private int AddComponents0(bool increaseIndex, int cIndex, IEnumerable<IByteBuffer> buffers)
        {
            Contract.Requires(buffers != null);

            if (buffers is IByteBuffer buffer)
            {
                // If buffers also implements ByteBuf (e.g. CompositeByteBuf), it has to go to addComponent(ByteBuf).
                return this.AddComponent0(increaseIndex, cIndex, buffer);
            }

            var array = buffers.ToArray();
            return this.AddComponents0(increaseIndex, cIndex, array, 0, array.Length);
        }

        /// <summary>
        ///     This should only be called as last operation from a method as this may adjust the underlying
        ///     array of components and so affect the index etc.
        /// </summary>
        void ConsolidateIfNeeded()
        {
            // Consolidate if the number of components will exceed the allowed maximum by the current
            // operation.
            int numComponents = this.components.Count;
            if (numComponents > this.MaxNumComponents)
            {
                int capacity = this.components[numComponents - 1].EndOffset;

                IByteBuffer consolidated = this.AllocateBuffer(capacity);

                // We're not using foreach to avoid creating an iterator.
                for (int i = 0; i < numComponents; i++)
                {
                    var component = this.components[i];
                    var buffer = component.Buffer;
                    consolidated.WriteBytes(buffer);
                    component.FreeIfNecessary();
                }
                var newComponent = new ComponentEntry(consolidated);
                newComponent.EndOffset = newComponent.Length;
                this.components.Clear();
                this.components.Add(newComponent);
            }
        }

        private void CheckComponentIndex(int cIndex)
        {
            this.EnsureAccessible();
            if (cIndex < 0 || cIndex > this.components.Count)
            {
                throw new ArgumentOutOfRangeException($"cIndex: {cIndex} (expected: >= 0 && <= numComponents({this.components.Count}))");
            }
        }

        private void CheckComponentIndex(int cIndex, int numComponents)
        {
            this.EnsureAccessible();
            if (cIndex < 0 || cIndex + numComponents > this.components.Count)
            {
                throw new ArgumentOutOfRangeException($"cIndex: {cIndex}, numComponents: {numComponents} " + $"(expected: cIndex >= 0 && cIndex + numComponents <= totalNumComponents({this.components.Count}))");
            }
        }

        private void UpdateComponentOffsets(int cIndex)
        {
            int size = this.components.Count;
            if (size <= cIndex)
            {
                return;
            }

            var component = this.components[cIndex];
            if (cIndex == 0)
            {
                component.Offset = 0;
                component.EndOffset = component.Length;
                cIndex++;
            }

            for (int i = cIndex; i < size; i++)
            {
                ComponentEntry prev = this.components[i - 1];
                ComponentEntry cur = this.components[i];
                cur.Offset = prev.EndOffset;
                cur.EndOffset = cur.Offset + cur.Length;
            }
        }

        /// <summary>
        ///     Remove the {@link IByteBuffer} from the given index.
        ///     @param cIndex the index on from which the {@link IByteBuffer} will be remove
        /// </summary>
        public virtual CompositeByteBuffer RemoveComponent(int cIndex)
        {
            this.CheckComponentIndex(cIndex);
            ComponentEntry comp = this.components[cIndex];
            this.components.RemoveAt(cIndex);
            comp.FreeIfNecessary();
            if (comp.Length > 0)
            {
                // Only need to call updateComponentOffsets if the length was > 0
                this.UpdateComponentOffsets(cIndex);
            }
            return this;
        }

        /// <summary>
        ///     Remove the number of {@link IByteBuffer}s starting from the given index.
        ///     @param cIndex the index on which the {@link IByteBuffer}s will be started to removed
        ///     @param numComponents the number of components to remove
        /// </summary>
        public virtual CompositeByteBuffer RemoveComponents(int cIndex, int numComponents)
        {
            this.CheckComponentIndex(cIndex, numComponents);

            if (numComponents == 0)
            {
                return this;
            }
            bool needsUpdate = false;
            for (int i = cIndex + numComponents; i >= cIndex; i--)
            {
                ComponentEntry c = this.components[i];
                needsUpdate |= c.Length > 0;
                c.FreeIfNecessary();
                this.components.RemoveAt(i);
            }

            if (needsUpdate)
            {
                // Only need to call updateComponentOffsets if the length was > 0
                this.UpdateComponentOffsets(cIndex);
            }
            return this;
        }

        public virtual IEnumerator<IByteBuffer> GetEnumerator()
        {
            this.EnsureAccessible();
            foreach (ComponentEntry c in this.components)
            {
                yield return c.Buffer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        ///     Same with {@link #slice(int, int)} except that this method returns a list.
        /// </summary>
        public virtual IList<IByteBuffer> Decompose(int offset, int length)
        {
            this.CheckIndex(offset, length);
            if (length == 0)
            {
                return EmptyList;
            }

            int componentId = this.ToComponentIndex(offset);
            var slice = new List<IByteBuffer>(this.components.Count);

            // The first component
            ComponentEntry firstC = this.components[componentId];
            IByteBuffer first = firstC.Buffer.Duplicate();
            first.SetReaderIndex(offset - firstC.Offset);

            IByteBuffer buffer = first;
            int bytesToSlice = length;
            do
            {
                int readableBytes = buffer.ReadableBytes;
                if (bytesToSlice <= readableBytes)
                {
                    // Last component
                    buffer.SetWriterIndex(buffer.ReaderIndex + bytesToSlice);
                    slice.Add(buffer);
                    break;
                }
                else
                {
                    // Not the last component
                    slice.Add(buffer);
                    bytesToSlice -= readableBytes;
                    componentId++;

                    // Fetch the next component.
                    buffer = this.components[componentId].Buffer.Duplicate();
                }
            }
            while (bytesToSlice > 0);

            // Slice all components because only readable bytes are interesting.
            for (int i = 0; i < slice.Count; i++)
            {
                slice[i] = slice[i].Slice();
            }

            return slice;
        }

        public override int IoBufferCount
        {
            get
            {
                switch (this.components.Count)
                {
                    case 0:
                        return 1;
                    case 1:
                        return this.components[0].Buffer.IoBufferCount;
                    default:
                        int count = 0;
                        int componentsCount = this.components.Count;
                        for (int i = 0; i < componentsCount; i++)
                        {
                            ComponentEntry c = this.components[i];
                            count += c.Buffer.IoBufferCount;
                        }
                        return count;
                }
            }
        }

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex(index, length);

            switch (this.components.Count)
            {
                case 0:
                    return EmptyNioBuffer;
                case 1:
                    IByteBuffer buf = this.components[0].Buffer;
                    if (buf.IoBufferCount == 1)
                    {
                        return this.components[0].Buffer.GetIoBuffer(index, length);
                    }
                    break;
            }

            var merged = new byte[length];
            ArraySegment<byte>[] buffers = this.GetIoBuffers(index, length);

            int offset = 0;
            foreach (ArraySegment<byte> buf in buffers)
            {
                Contract.Assert(merged.Length - offset >= buf.Count);

                PlatformDependent.CopyMemory(buf.Array, buf.Offset, merged, offset, buf.Count);
                offset += buf.Count;
            }

            return new ArraySegment<byte>(merged);
        }

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length)
        {
            this.CheckIndex(index, length);
            if (length == 0)
            {
                return new[] { EmptyNioBuffer };
            }

            var buffers = new List<ArraySegment<byte>>(this.components.Count);
            int i = this.ToComponentIndex(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                switch (s.IoBufferCount)
                {
                    case 0:
                        throw new NotSupportedException();
                    case 1:
                        buffers.Add(s.GetIoBuffer(index - adjustment, localLength));
                        break;
                    default:
                        buffers.AddRange(s.GetIoBuffers(index - adjustment, localLength));
                        break;
                }

                index += localLength;
                length -= localLength;
                i++;
            }

            return buffers.ToArray();
        }

        public override bool HasArray
        {
            get
            {
                switch (this.components.Count)
                {
                    case 0:
                        return true;
                    case 1:
                        return this.components[0].Buffer.HasArray;
                    default:
                        return false;
                }
            }
        }

        public override byte[] Array
        {
            get
            {
                switch (this.components.Count)
                {
                    case 0:
                        return ArrayExtensions.ZeroBytes;
                    case 1:
                        return this.components[0].Buffer.Array;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public override int ArrayOffset
        {
            get
            {
                switch (this.components.Count)
                {
                    case 0:
                        return 0;
                    case 1:
                        return this.components[0].Buffer.ArrayOffset;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public override bool HasMemoryAddress
        {
            get
            {
                switch (this.components.Count)
                {
                    case 1:
                        return this.components[0].Buffer.HasMemoryAddress;
                    default:
                        return false;
                }
            }
        }

        public override ref byte GetPinnableMemoryAddress()
        {
            switch (this.components.Count)
            {
                case 1:
                    return ref this.components[0].Buffer.GetPinnableMemoryAddress();
                default:
                    throw new NotSupportedException();
            }
        }

        public override IntPtr AddressOfPinnedMemory()
        {
            switch (this.components.Count)
            {
                case 1:
                    return this.components[0].Buffer.AddressOfPinnedMemory();
                default:
                    throw new NotSupportedException();
            }
        }

        public override int Capacity => GetCapacity(this.components);

        static int GetCapacity(List<ComponentEntry> components)
        {
            int numComponents = components.Count;
            if (numComponents == 0)
            {
                return 0;
            }

            return components[numComponents - 1].EndOffset;
        }

        public override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.CheckNewCapacity(newCapacity);

            int oldCapacity = this.Capacity;
            if (newCapacity > oldCapacity)
            {
                int paddingLength = newCapacity - oldCapacity;
                IByteBuffer padding;
                int nComponents = this.components.Count;
                if (nComponents < this.MaxNumComponents)
                {
                    padding = this.AllocateBuffer(paddingLength);
                    padding.SetIndex(0, paddingLength);
                    this.AddComponent0(false, this.components.Count, padding);
                }
                else
                {
                    padding = this.AllocateBuffer(paddingLength);
                    padding.SetIndex(0, paddingLength);
                    // FIXME: No need to create a padding buffer and consolidate.
                    // Just create a big single buffer and put the current content there.
                    this.AddComponent0(false, this.components.Count, padding);
                    this.ConsolidateIfNeeded();
                }
            }
            else if (newCapacity < oldCapacity)
            {
                int bytesToTrim = oldCapacity - newCapacity;
                for (int i = this.components.Count - 1; i >= 0; i--)
                {
                    ComponentEntry c = this.components[i];
                    if (bytesToTrim >= c.Length)
                    {
                        bytesToTrim -= c.Length;
                        this.components.RemoveAt(i);
                        continue;
                    }

                    // Replace the last component with the trimmed slice.
                    var newC = new ComponentEntry(c.Buffer.Slice(0, c.Length - bytesToTrim));
                    newC.Offset = c.Offset;
                    newC.EndOffset = newC.Offset + newC.Length;
                    this.components[i] = newC;
                    break;
                }

                if (this.ReaderIndex > newCapacity)
                {
                    this.SetIndex(newCapacity, newCapacity);
                }
                else if (this.WriterIndex > newCapacity)
                {
                    this.SetWriterIndex(newCapacity);
                }
            }
            return this;
        }

        public override IByteBufferAllocator Allocator => this.allocator;

        /// <summary>
        ///     Return the current number of {@link IByteBuffer}'s that are composed in this instance
        /// </summary>
        public virtual int NumComponents => this.components.Count;

        /// <summary>
        ///     Return the max number of {@link IByteBuffer}'s that are composed in this instance
        /// </summary>
        public virtual int MaxNumComponents => this.maxNumComponents;

        /// <summary>
        ///     Return the index for the given offset
        /// </summary>
        public virtual int ToComponentIndex(int offset)
        {
            this.CheckIndex(offset);

            for (int low = 0, high = this.components.Count; low <= high;)
            {
                int mid = (low + high).RightUShift(1);
                ComponentEntry c = this.components[mid];
                if (offset >= c.EndOffset)
                {
                    low = mid + 1;
                }
                else if (offset < c.Offset)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            throw new Exception("should not reach here");
        }

        public virtual int ToByteIndex(int cIndex)
        {
            this.CheckComponentIndex(cIndex);
            return this.components[cIndex].Offset;
        }

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            IByteBuffer dst = this.AllocateBuffer(length);
            if (length != 0)
            {
                this.CopyTo(index, length, this.ToComponentIndex(index), dst);
            }
            return dst;
        }

        private void CopyTo(int index, int length, int componentId, IByteBuffer dst)
        {
            int dstIndex = 0;
            int i = componentId;

            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                s.GetBytes(index - adjustment, dst, dstIndex, localLength);
                index += localLength;
                dstIndex += localLength;
                length -= localLength;
                i++;
            }

            dst.SetWriterIndex(dst.Capacity);
        }

        /// <summary>
        ///     Return the {@link IByteBuffer} on the specified index
        ///     @param cIndex the index for which the {@link IByteBuffer} should be returned
        ///     @return buffer the {@link IByteBuffer} on the specified index
        /// </summary>
        public virtual IByteBuffer this[int cIndex] => this.InternalComponent(cIndex).Duplicate();

        /// <summary>
        ///     Return the {@link IByteBuffer} on the specified index
        ///     @param offset the offset for which the {@link IByteBuffer} should be returned
        ///     @return the {@link IByteBuffer} on the specified index
        /// </summary>
        public virtual IByteBuffer ComponentAtOffset(int offset) => this.InternalComponentAtOffset(offset).Duplicate();

        /// <summary>
        ///     Return the internal {@link IByteBuffer} on the specified index. Note that updating the indexes of the returned
        ///     buffer will lead to an undefined behavior of this buffer.
        ///     @param cIndex the index for which the {@link IByteBuffer} should be returned
        /// </summary>
        public virtual IByteBuffer InternalComponent(int cIndex)
        {
            this.CheckComponentIndex(cIndex);
            return this.components[cIndex].Buffer;
        }

        /// <summary>
        ///     Return the internal {@link IByteBuffer} on the specified offset. Note that updating the indexes of the returned
        ///     buffer will lead to an undefined behavior of this buffer.
        ///     @param offset the offset for which the {@link IByteBuffer} should be returned
        /// </summary>
        public virtual IByteBuffer InternalComponentAtOffset(int offset) => this.FindComponent(offset).Buffer;

        private ComponentEntry FindComponent(int offset)
        {
            this.CheckIndex(offset);

            for (int low = 0, high = this.components.Count; low <= high;)
            {
                int mid = (low + high).RightUShift(1);
                ComponentEntry c = this.components[mid];
                if (offset >= c.EndOffset)
                {
                    low = mid + 1;
                }
                else if (offset < c.Offset)
                {
                    high = mid - 1;
                }
                else
                {
                    Contract.Assert(c.Length != 0);
                    return c;
                }
            }

            throw new Exception("should not reach here");
        }

        /// <summary>
        ///     Consolidate the composed {@link IByteBuffer}s
        /// </summary>
        public virtual CompositeByteBuffer Consolidate()
        {
            this.EnsureAccessible();
            int numComponents = this.NumComponents;
            if (numComponents <= 1)
            {
                return this;
            }

            ComponentEntry last = this.components[numComponents - 1];
            int capacity = last.EndOffset;
            IByteBuffer consolidated = this.AllocateBuffer(capacity);

            for (int i = 0; i < numComponents; i++)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer b = c.Buffer;
                consolidated.WriteBytes(b);
                c.FreeIfNecessary();
            }

            this.components.Clear();
            this.components.Add(new ComponentEntry(consolidated));
            this.UpdateComponentOffsets(0);
            return this;
        }

        /// <summary>
        ///     Consolidate the composed {@link IByteBuffer}s
        ///     @param cIndex the index on which to start to compose
        ///     @param numComponents the number of components to compose
        /// </summary>
        public virtual CompositeByteBuffer Consolidate(int cIndex, int numComponents)
        {
            this.CheckComponentIndex(cIndex, numComponents);
            if (numComponents <= 1)
            {
                return this;
            }

            int endCIndex = cIndex + numComponents;
            ComponentEntry last = this.components[endCIndex - 1];
            int capacity = last.EndOffset - this.components[cIndex].Offset;
            IByteBuffer consolidated = this.AllocateBuffer(capacity);

            for (int i = cIndex; i < endCIndex; i++)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer b = c.Buffer;
                consolidated.WriteBytes(b);
                c.FreeIfNecessary();
            }

            this.components.RemoveRange(cIndex, numComponents);
            this.components.Insert(cIndex,new ComponentEntry(consolidated));
            this.UpdateComponentOffsets(cIndex);
            return this;
        }

        /// <summary>
        ///     Discard all {@link IByteBuffer}s which are read.
        /// </summary>
        public virtual CompositeByteBuffer DiscardReadComponents()
        {
            this.EnsureAccessible();
            int readerIndex = this.ReaderIndex;
            if (readerIndex == 0)
            {
                return this;
            }

            // Discard everything if (readerIndex = writerIndex = capacity).
            int writerIndex = this.WriterIndex;
            if (readerIndex == writerIndex && writerIndex == this.Capacity)
            {
                foreach (ComponentEntry c in this.components)
                {
                    c.FreeIfNecessary();
                }
                this.components.Clear();
                this.SetIndex(0, 0);
                this.AdjustMarkers(readerIndex);
                return this;
            }

            // Remove read components.
            int firstComponentId = this.ToComponentIndex(readerIndex);
            for (int i = 0; i < firstComponentId; i++)
            {
                this.components[i].FreeIfNecessary();
            }
            this.components.RemoveRange(0, firstComponentId);

            // Update indexes and markers.
            ComponentEntry first = this.components[0];
            int offset = first.Offset;
            this.UpdateComponentOffsets(0);
            this.SetIndex(readerIndex - offset, writerIndex - offset);
            this.AdjustMarkers(offset);
            return this;
        }

        public override IByteBuffer DiscardReadBytes()
        {
            this.EnsureAccessible();
            int readerIndex = this.ReaderIndex;
            if (readerIndex == 0)
            {
                return this;
            }

            // Discard everything if (readerIndex = writerIndex = capacity).
            int writerIndex = this.WriterIndex;
            if (readerIndex == writerIndex && writerIndex == this.Capacity)
            {
                foreach (ComponentEntry c1 in this.components)
                {
                    c1.FreeIfNecessary();
                }
                this.components.Clear();
                this.SetIndex(0, 0);
                this.AdjustMarkers(readerIndex);
                return this;
            }

            // Remove read components.
            int firstComponentId = this.ToComponentIndex(readerIndex);
            for (int i = 0; i < firstComponentId; i++)
            {
                this.components[i].FreeIfNecessary();
            }
            this.components.RemoveRange(0, firstComponentId);

            // Remove or replace the first readable component with a new slice.
            ComponentEntry c = this.components[0];
            int adjustment = readerIndex - c.Offset;
            if (adjustment == c.Length)
            {
                // new slice would be empty, so remove instead
                this.components.RemoveAt(0);
            }
            else
            {
                var newC = new ComponentEntry(c.Buffer.Slice(adjustment, c.Length - adjustment));
                this.components[0] = newC;
            }

            // Update indexes and markers.
            this.UpdateComponentOffsets(0);
            this.SetIndex(0, writerIndex - readerIndex);
            this.AdjustMarkers(readerIndex);
            return this;
        }

        private IByteBuffer AllocateBuffer(int capacity) => this.Allocator.Buffer(capacity);

        public override string ToString()
        {
            string result = base.ToString();
            result = result.Substring(0, result.Length - 1);
            return $"{result}, components={this.components.Count})";
        }

        public override IByteBuffer DiscardSomeReadBytes() => this.DiscardReadComponents();

        protected internal override void Deallocate()
        {
            if (this.freed)
            {
                return;
            }

            this.freed = true;
            int size = this.components.Count;
            // We're not using foreach to avoid creating an iterator.
            // see https://github.com/netty/netty/issues/2642
            for (int i = 0; i < size; i++)
            {
                this.components[i].FreeIfNecessary();
            }
        }

        public override IByteBuffer Unwrap() => null;

        #endregion

        #region IByteBufferProvider

        protected internal override unsafe T _Get<T>(int index)
        {
            var c = this.FindComponent(index);
            if (index + sizeof(T) <= c.EndOffset)
            {
                return c.Buffer.Get<T>(index - c.Offset);
            }

            var bytes = new byte[sizeof(T)];
            this.GetBytes(index, bytes, sizeof(T));
            return MemoryMarshal.Read<T>(bytes);
        }

        protected internal override unsafe void _Set<T>(int index, T value)
        {
            ComponentEntry c = this.FindComponent(index);
            if (index + sizeof(T) <= c.EndOffset)
            {
                c.Buffer.Set<T>(index - c.Offset, value);
            }
            else
            {
                var span = MemoryMarshal.CreateSpan(ref value, 1);
                var asBytes = MemoryMarshal.AsBytes(span);
                this.SetBytes(index, asBytes.ToArray());
            }
        }

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Capacity);
            if (length == 0) return;

            int i = this.ToComponentIndex(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                s.GetBytes(index - adjustment, dst, dstIndex, localLength);
                index += localLength;
                dstIndex += localLength;
                length -= localLength;
                i++;
            }
        }

        public override void GetBytes(int index, Span<byte> dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Length);
            if (length == 0) return;

            int i = this.ToComponentIndex(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                s.GetBytes(index - adjustment, dst, dstIndex, localLength);
                index += localLength;
                dstIndex += localLength;
                length -= localLength;
                i++;
            }
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Capacity);
            if (length == 0) return;

            int i = this.ToComponentIndex(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                s.SetBytes(index - adjustment, src, srcIndex, localLength);
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
        }

        public override void SetBytes(int index, Span<byte> src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Length);
            if (length == 0) return;

            int i = this.ToComponentIndex(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                IByteBuffer s = c.Buffer;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                s.SetBytes(index - adjustment, src, srcIndex, localLength);
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
        }

        #endregion
    }
}