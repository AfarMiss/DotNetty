// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// namespace DotNetty.Buffers
// {
//     using System;
//     using System.IO;
//     using System.Text;
//     using System.Threading;
//     using System.Threading.Tasks;
//     using DotNetty.Common;
//     using DotNetty.Common.Internal;
//     using DotNetty.Common.Internal.Logging;
//     using DotNetty.Common.Utilities;
//
//     sealed class AdvancedLeakAwareByteBuffer : SimpleLeakAwareByteBuffer
//     {
//         const string PropAcquireAndReleaseOnly = "io.netty.leakDetection.acquireAndReleaseOnly";
//         static readonly bool AcquireAndReleaseOnly;
//
//         static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AdvancedLeakAwareByteBuffer>();
//
//         static AdvancedLeakAwareByteBuffer()
//         {
//             AcquireAndReleaseOnly = SystemPropertyUtil.Get<bool>(PropAcquireAndReleaseOnly, false);
//
//             if (Logger.DebugEnabled)
//             {
//                 Logger.Debug("-D{}: {}", PropAcquireAndReleaseOnly, AcquireAndReleaseOnly);
//             }
//         }
//
//         internal AdvancedLeakAwareByteBuffer(IByteBuffer buf, IResourceLeakTracker leak)
//             : base(buf, leak)
//         {
//         }
//
//         internal AdvancedLeakAwareByteBuffer(IByteBuffer wrapped, IByteBuffer trackedByteBuf, IResourceLeakTracker leak)
//             : base(wrapped, trackedByteBuf, leak)
//         {
//         }
//
//         internal static void RecordLeakNonRefCountingOperation(IResourceLeakTracker leak)
//         {
//             if (!AcquireAndReleaseOnly)
//             {
//                 leak.Record();
//             }
//         }
//
//         public override IByteBuffer Slice()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Slice();
//         }
//
//         public override IByteBuffer Slice(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Slice(index, length);
//         }
//
//         public override IByteBuffer Duplicate()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Duplicate();
//         }
//
//         public override IByteBuffer ReadSlice(int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadSlice(length);
//         }
//
//         public override IByteBuffer DiscardReadBytes()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.DiscardReadBytes();
//         }
//
//         public override IByteBuffer DiscardSomeReadBytes()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.DiscardSomeReadBytes();
//         }
//
//         public override IByteBuffer EnsureWritable(int minWritableBytes)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.EnsureWritable(minWritableBytes);
//         }
//
//         public override int EnsureWritable(int minWritableBytes, bool force)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.EnsureWritable(minWritableBytes, force);
//         }
//
//         public override bool GetBoolean(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<bool>(index);
//         }
//
//         public override byte GetByte(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<byte>(index);
//         }
//
//         public override int GetMedium(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetMedium(index);
//         }
//
//         public override int GetUnsignedMedium(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetUnsignedMedium(index);
//         }
//
//         public override short GetShort(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetShort(index);
//         }
//
//         public override ushort GetUnsignedShort(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<ushort>(index);
//         }
//
//         public override int GetInt(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetInt(index);
//         }
//
//         public override uint GetUnsignedInt(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetUnsignedInt(index);
//         }
//
//         public override long GetLong(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetLong(index);
//         }
//
//         public override char GetChar(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<char>(index);
//         }
//
//         public override float GetFloat(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<float>(index);
//         }
//
//         public override double GetDouble(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<double>(index);
//         }
//
//         public override IByteBuffer GetBytes(int index, IByteBuffer dst)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, dst);
//         }
//
//         public override IByteBuffer GetBytes(int index, IByteBuffer dst, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, dst, length);
//         }
//
//         public override IByteBuffer GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, dst, dstIndex, length);
//         }
//
//         public override IByteBuffer GetBytes(int index, byte[] dst)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, dst);
//         }
//
//         public override IByteBuffer GetBytes(int index, byte[] dst, int dstIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, dst, dstIndex, length);
//         }
//
//         public override IByteBuffer SetBoolean(int index, bool value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBoolean(index, value);
//         }
//
//         public override IByteBuffer SetByte(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetByte(index, value);
//         }
//
//         public override IByteBuffer SetMedium(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetMedium(index, value);
//         }
//
//         public override IByteBuffer SetShort(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<short>(index, value);
//         }
//
//         public override IByteBuffer SetInt(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<int>(index, value);
//         }
//
//         public override IByteBuffer SetLong(int index, long value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetLong(index, value);
//         }
//
//         public override IByteBuffer SetChar(int index, char value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<char>(index, value);
//         }
//
//         public override IByteBuffer SetFloat(int index, float value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<float>(index, value);
//         }
//
//         public override IByteBuffer SetDouble(int index, double value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<double>(index, value);
//         }
//
//         public override IByteBuffer SetBytes(int index, IByteBuffer src)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytes(index, src);
//         }
//
//         public override IByteBuffer SetBytes(int index, IByteBuffer src, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytes(index, src, length);
//         }
//
//         public override IByteBuffer SetBytes(int index, IByteBuffer src, int srcIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytes(index, src, srcIndex, length);
//         }
//
//         public override IByteBuffer SetBytes(int index, byte[] src)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytes(index, src);
//         }
//
//         public override IByteBuffer SetBytes(int index, byte[] src, int srcIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytes(index, src, srcIndex, length);
//         }
//
//         public override Task<int> SetBytesAsync(int index, Stream input, int length, CancellationToken cancellationToken)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetBytesAsync(index, input, length, cancellationToken);
//         }
//
//         public override IByteBuffer SetZero(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetZero(index, length);
//         }
//
//         public override bool ReadBoolean()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<bool>();
//         }
//
//         public override byte ReadByte()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadByte();
//         }
//
//         public override short ReadShort()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<short>();
//         }
//
//         public override ushort ReadUnsignedShort()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<ushort>();
//         }
//
//         public override int ReadMedium()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadMedium();
//         }
//
//         public override int ReadUnsignedMedium()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadUnsignedMedium();
//         }
//
//         public override int ReadInt()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<int>();
//         }
//
//         public override uint ReadUnsignedInt()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<uint>();
//         }
//
//         public override long ReadLong()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<long>();
//         }
//
//         public override char ReadChar()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<char>();
//         }
//
//         public override float ReadFloat()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<float>();
//         }
//
//         public override double ReadDouble()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<double>();
//         }
//
//         public override IByteBuffer ReadBytes(int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(length);
//         }
//
//         public override IByteBuffer ReadBytes(IByteBuffer dst)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(dst);
//         }
//
//         public override IByteBuffer ReadBytes(IByteBuffer dst, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(dst, length);
//         }
//
//         public override IByteBuffer ReadBytes(IByteBuffer dst, int dstIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(dst, dstIndex, length);
//         }
//
//         public override IByteBuffer ReadBytes(byte[] dst)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(dst);
//         }
//
//         public override IByteBuffer ReadBytes(byte[] dst, int dstIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(dst, dstIndex, length);
//         }
//
//         public override IByteBuffer ReadBytes(Stream output, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadBytes(output, length);
//         }
//
//         public override IByteBuffer SkipBytes(int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SkipBytes(length);
//         }
//
//         public override IByteBuffer WriteBoolean(bool value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<bool>(value);
//         }
//
//         public override IByteBuffer WriteByte(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteByte(value);
//         }
//
//         public override IByteBuffer WriteShort(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteShort(value);
//         }
//
//         public override IByteBuffer WriteInt(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<int>(value);
//         }
//
//         public override IByteBuffer WriteMedium(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteMedium(value);
//         }
//
//         public override IByteBuffer WriteLong(long value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<long>(value);
//         }
//
//         public override IByteBuffer WriteChar(char value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<char>(value);
//         }
//
//         public override IByteBuffer WriteFloat(float value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<float>(value);
//         }
//
//         public override IByteBuffer WriteDouble(double value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<double>(value);
//         }
//
//         public override IByteBuffer WriteBytes(IByteBuffer src)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytes(src);
//         }
//
//         public override IByteBuffer WriteBytes(IByteBuffer src, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytes(src, length);
//         }
//
//         public override IByteBuffer WriteBytes(IByteBuffer src, int srcIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytes(src, srcIndex, length);
//         }
//
//         public override IByteBuffer WriteBytes(byte[] src)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytes(src);
//         }
//
//         public override IByteBuffer WriteBytes(byte[] src, int srcIndex, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytes(src, srcIndex, length);
//         }
//
//         public override Task WriteBytesAsync(Stream input, int length, CancellationToken cancellationToken)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteBytesAsync(input, length, cancellationToken);
//         }
//
//         public override IByteBuffer WriteZero(int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteZero(length);
//         }
//
//         public override int IndexOf(int fromIndex, int toIndex, byte value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.IndexOf(fromIndex, toIndex, value);
//         }
//
//         public override int BytesBefore(byte value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.BytesBefore(value);
//         }
//
//         public override int BytesBefore(int length, byte value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.BytesBefore(length, value);
//         }
//
//         public override int BytesBefore(int index, int length, byte value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.BytesBefore(index, length, value);
//         }
//
//         public override int ForEachByte(IByteProcessor processor)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ForEachByte(processor);
//         }
//
//         public override int ForEachByte(int index, int length, IByteProcessor processor)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ForEachByte(index, length, processor);
//         }
//
//         public override int ForEachByteDesc(IByteProcessor processor)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ForEachByteDesc(processor);
//         }
//
//         public override int ForEachByteDesc(int index, int length, IByteProcessor processor)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ForEachByteDesc(index, length, processor);
//         }
//
//         public override IByteBuffer Copy()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Copy();
//         }
//
//         public override IByteBuffer Copy(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Copy(index, length);
//         }
//
//         public override int IoBufferCount
//         {
//             get
//             {
//                 RecordLeakNonRefCountingOperation(this.Leak);
//                 return base.IoBufferCount;
//             }
//         }
//
//         public override ArraySegment<byte> GetIoBuffer()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetIoBuffer();
//         }
//
//         public override ArraySegment<byte> GetIoBuffer(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetIoBuffer(index, length);
//         }
//
//         public override ArraySegment<byte>[] GetIoBuffers()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetIoBuffers();
//         }
//
//         public override ArraySegment<byte>[] GetIoBuffers(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetIoBuffers(index, length);
//         }
//
//         public override string ToString(Encoding encoding)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ToString(encoding);
//         }
//
//         public override string ToString(int index, int length, Encoding encoding)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ToString(index, length, encoding);
//         }
//
//         public override IByteBuffer AdjustCapacity(int newCapacity)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.AdjustCapacity(newCapacity);
//         }
//
//         public override short GetShortLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<short>(index);
//         }
//
//         public override ushort GetUnsignedShortLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<ushort>(index);
//         }
//
//         public override int GetMediumLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetMediumLE(index);
//         }
//
//         public override int GetUnsignedMediumLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetUnsignedMediumLE(index);
//         }
//
//         public override int GetIntLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<int>(index);
//         }
//
//         public override uint GetUnsignedIntLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<uint>(index);
//         }
//
//         public override long GetLongLE(int index)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Get<long>(index);
//         }
//
//         public override IByteBuffer SetShortLE(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<short>(index, value);
//         }
//
//         public override IByteBuffer SetIntLE(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<int>(index, value);
//         }
//
//         public override IByteBuffer SetMediumLE(int index, int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.SetMediumLE(index, value);
//         }
//
//         public override IByteBuffer SetLongLE(int index, long value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Set<long>(index, value);
//         }
//
//         public override short ReadShortLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<short>();
//         }
//
//         public override ushort ReadUnsignedShortLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<ushort>();
//         }
//
//         public override int ReadMediumLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadMediumLE();
//         }
//
//         public override int ReadUnsignedMediumLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadUnsignedMediumLE();
//         }
//
//         public override int ReadIntLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<int>();
//         }
//
//         public override uint ReadUnsignedIntLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<uint>();
//         }
//
//         public override long ReadLongLE()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Read<long>();
//         }
//
//         public override IByteBuffer WriteShortLE(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<short>(value);
//         }
//
//         public override IByteBuffer WriteMediumLE(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.WriteMediumLE(value);
//         }
//
//         public override IByteBuffer WriteIntLE(int value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<int>(value);
//         }
//
//         public override IByteBuffer WriteLongLE(long value)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.Write<long>(value);
//         }
//
//         public override IByteBuffer GetBytes(int index, Stream destination, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.GetBytes(index, destination, length);
//         }
//
//         public override IReferenceCounted Retain()
//         {
//             this.Leak.Record();
//             return base.Retain();
//         }
//
//         public override IReferenceCounted Retain(int increment)
//         {
//             this.Leak.Record();
//             return base.Retain(increment);
//         }
//
//         public override IByteBuffer RetainedSlice()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.RetainedSlice();
//         }
//
//         public override IByteBuffer RetainedSlice(int index, int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.RetainedSlice(index, length);
//         }
//
//         public override IByteBuffer RetainedDuplicate()
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.RetainedDuplicate();
//         }
//
//         public override IByteBuffer ReadRetainedSlice(int length)
//         {
//             RecordLeakNonRefCountingOperation(this.Leak);
//             return base.ReadRetainedSlice(length);
//         }
//
//         public override IReferenceCounted Touch()
//         {
//             this.Leak.Record();
//             return this;
//         }
//
//         public override IReferenceCounted Touch(object hint)
//         {
//             this.Leak.Record(hint);
//             return this;
//         }
//
//         public override bool Release()
//         {
//             this.Leak.Record();
//             return base.Release();
//         }
//
//         public override bool Release(int decrement)
//         {
//             this.Leak.Record();
//             return base.Release(decrement);
//         }
//
//         protected override SimpleLeakAwareByteBuffer NewLeakAwareByteBuffer(IByteBuffer buf, IByteBuffer trackedByteBuf, IResourceLeakTracker leakTracker) =>
//             new AdvancedLeakAwareByteBuffer(buf, trackedByteBuf, leakTracker);
//     }
// }