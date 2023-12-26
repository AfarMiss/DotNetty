using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using DotNetty.Common.Concurrency;
namespace DotNetty.Transport.Channels
{
    public partial class DefaultChannelPipeline
    {
        private AbstractChannelHandlerContext NewContext(IEventExecutorGroup group, string name, IChannelHandler handler) => new DefaultChannelHandlerContext(this, this.GetChildExecutor(group), name, handler);

        private AbstractChannelHandlerContext NewContext(IEventExecutor executor, string name, IChannelHandler handler) => new DefaultChannelHandlerContext(this, executor, name, handler);

        private static void CheckMultiplicity(IChannelHandler handler)
        {
            if (handler is ChannelHandlerAdapter adapter)
            {
                if (!adapter.IsSharable && adapter.Added)
                {
                    throw new ChannelPipelineException(
                        adapter.GetType().Name + " is not a @Sharable handler, so can't be added or removed multiple times.");
                }
                adapter.Added = true;
            }
        }
        
        private string FilterName(string name, IChannelHandler handler)
        {
            if (name == null)
            {
                return this.GenerateName(handler);
            }
            this.CheckDuplicateName(name);
            return name;
        }

        private void CheckDuplicateName(string name)
        {
            if (this.Context0(name) != null)
            {
                throw new ArgumentException("Duplicate handler name: " + name, nameof(name));
            }
        }

        private AbstractChannelHandlerContext GetContextOrThrow(string name)
        {
            var ctx = (AbstractChannelHandlerContext)this.Context(name);
            if (ctx == null)
            {
                throw new ArgumentException($"Not Found Context: `{name}`");
            }

            return ctx;
        }

        private AbstractChannelHandlerContext GetContextOrThrow(IChannelHandler handler)
        {
            var ctx = (AbstractChannelHandlerContext)this.Context(handler);
            if (ctx == null)
            {
                throw new ArgumentException($"Not Found Context: `{handler.GetType().Name}`");
            }

            return ctx;
        }

        private AbstractChannelHandlerContext GetContextOrThrow<T>() where T : class, IChannelHandler
        {
            var ctx = (AbstractChannelHandlerContext)this.Context<T>();
            if (ctx == null)
            {
                throw new ArgumentException($"Not Found Context: `{typeof(T).Name}`");
            }

            return ctx;
        }
        
        IEnumerator<IChannelHandler> IEnumerable<IChannelHandler>.GetEnumerator()
        {
            var current = this.head;
            while (current != null)
            {
                yield return current.Handler;
                current = current.Next;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IChannelHandler>)this).GetEnumerator();
        
        public IChannelHandlerContext FirstContext()
        {
            var first = this.head.Next;
            return first == this.tail ? null : first;
        }

        public IChannelHandlerContext LastContext()
        {
            var last = this.tail.Prev;
            return last == this.head ? null : last;
        }

        public IChannelHandlerContext Context(string name)
        {
            Contract.Requires(name != null);

            return this.Context0(name);
        }
        
        public IChannelHandlerContext Context(IChannelHandler handler)
        {
            Contract.Requires(handler != null);

            var ctx = this.head.Next;
            while (true)
            {
                if (ctx == null)
                {
                    return null;
                }

                if (ctx.Handler == handler)
                {
                    return ctx;
                }

                ctx = ctx.Next;
            }
        }

        public IChannelHandlerContext Context<T>() where T : class, IChannelHandler
        {
            var ctx = this.head.Next;
            while (true)
            {
                if (ctx == null)
                {
                    return null;
                }
                if (ctx.Handler is T)
                {
                    return ctx;
                }
                ctx = ctx.Next;
            }
        }
        
        private AbstractChannelHandlerContext Context0(string name)
        {
            var context = this.head.Next;
            while (context != this.tail)
            {
                if (context.Name.Equals(name, StringComparison.Ordinal))
                {
                    return context;
                }
                context = context.Next;
            }
            return null;
        }

        public void AddFirst(string name, IChannelHandler handler) => this.AddFirst(null, name, handler);

        public void AddFirst(IEventExecutorGroup group, string name, IChannelHandler handler)
        {
            Contract.Requires(handler != null);

            AbstractChannelHandlerContext newCtx;
            lock (this)
            {
                CheckMultiplicity(handler);

                newCtx = this.NewContext(group, this.FilterName(name, handler), handler);
                var executor = this.ExecutorSafe(newCtx.executor);

                this.AddFirst0(newCtx);

                if (executor == null)
                {
                    this.CallHandlerCallbackLater(newCtx, true);
                    return;
                }

                if (!executor.InEventLoop)
                {
                    executor.Execute(CallHandlerAddedAction, this, newCtx);
                    return;
                }
            }

            this.CallHandlerAdded0(newCtx);
        }

        private void AddFirst0(AbstractChannelHandlerContext newCtx)
        {
            var nextCtx = this.head.Next;
            newCtx.Prev = this.head;
            newCtx.Next = nextCtx;
            this.head.Next = newCtx;
            nextCtx.Prev = newCtx;
        }

        public void AddLast(string name, IChannelHandler handler) => this.AddLast(null, name, handler);

        public void AddLast(IEventExecutorGroup group, string name, IChannelHandler handler)
        {
            Contract.Requires(handler != null);

            AbstractChannelHandlerContext newCtx;
            lock (this)
            {
                CheckMultiplicity(handler);

                newCtx = this.NewContext(group, this.FilterName(name, handler), handler);
                var executor = this.ExecutorSafe(newCtx.executor);

                this.AddLast0(newCtx);

                if (executor == null)
                {
                    this.CallHandlerCallbackLater(newCtx, true);
                    return;
                }

                if (!executor.InEventLoop)
                {
                    executor.Execute(CallHandlerAddedAction, this, newCtx);
                    return;
                }
            }
            this.CallHandlerAdded0(newCtx);
        }

        private void AddLast0(AbstractChannelHandlerContext newCtx)
        {
            var prev = this.tail.Prev;
            newCtx.Prev = prev;
            newCtx.Next = this.tail;
            prev.Next = newCtx;
            this.tail.Prev = newCtx;
        }

        public void AddBefore(string baseName, string name, IChannelHandler handler) => this.AddBefore(null, baseName, name, handler);

        public void AddBefore(IEventExecutorGroup group, string baseName, string name, IChannelHandler handler)
        {
            Contract.Requires(handler != null);

            AbstractChannelHandlerContext newCtx;
            lock (this)
            {
                CheckMultiplicity(handler);
                var ctx = this.GetContextOrThrow(baseName);

                newCtx = this.NewContext(group, this.FilterName(name, handler), handler);
                var executor = this.ExecutorSafe(newCtx.executor);

                AddBefore0(ctx, newCtx);

                if (executor == null)
                {
                    this.CallHandlerCallbackLater(newCtx, true);
                    return;
                }

                if (!executor.InEventLoop)
                {
                    executor.Execute(CallHandlerAddedAction, this, newCtx);
                    return;
                }
            }
            this.CallHandlerAdded0(newCtx);
        }

        private static void AddBefore0(AbstractChannelHandlerContext ctx, AbstractChannelHandlerContext newCtx)
        {
            newCtx.Prev = ctx.Prev;
            newCtx.Next = ctx;
            ctx.Prev.Next = newCtx;
            ctx.Prev = newCtx;
        }

        public void AddAfter(string baseName, string name, IChannelHandler handler) => this.AddAfter(null, baseName, name, handler);

        public void AddAfter(IEventExecutorGroup group, string baseName, string name, IChannelHandler handler)
        {
            Contract.Requires(handler != null);

            AbstractChannelHandlerContext newCtx;

            lock (this)
            {
                CheckMultiplicity(handler);
                var ctx = this.GetContextOrThrow(baseName);

                newCtx = this.NewContext(group, this.FilterName(name, handler), handler);
                var executor = this.ExecutorSafe(newCtx.executor);

                AddAfter0(ctx, newCtx);

                // If the executor is null it means that the channel was not registered on an eventloop yet.
                // In this case we remove the context from the pipeline and add a task that will call
                // ChannelHandler.handlerRemoved(...) once the channel is registered.
                if (executor == null)
                {
                    this.CallHandlerCallbackLater(newCtx, true);
                    return;
                }

                if (!executor.InEventLoop)
                {
                    executor.Execute(CallHandlerAddedAction, this, newCtx);
                    return;
                }
            }
            this.CallHandlerAdded0(newCtx);
        }

        private static void AddAfter0(AbstractChannelHandlerContext ctx, AbstractChannelHandlerContext newCtx)
        {
            newCtx.Prev = ctx;
            newCtx.Next = ctx.Next;
            ctx.Next.Prev = newCtx;
            ctx.Next = newCtx;
        }

        public void AddFirst(params IChannelHandler[] handlers) => this.AddFirst(null, handlers);

        public void AddFirst(IEventExecutorGroup group, params IChannelHandler[] handlers)
        {
            Contract.Requires(handlers != null);

            for (int i = handlers.Length - 1; i >= 0; i--)
            {
                this.AddFirst(group, (string)null, handlers[i]);
            }
        }

        public void AddLast(params IChannelHandler[] handlers) => this.AddLast(null, handlers);

        public void AddLast(IEventExecutorGroup group, params IChannelHandler[] handlers)
        {
            foreach (var handler in handlers)
            {
                this.AddLast(group, (string)null, handler);
            }
        }

        public IChannelPipeline Remove(IChannelHandler handler)
        {
            this.Remove(this.GetContextOrThrow(handler));
            return this;
        }

        public IChannelHandler Remove(string name) => this.Remove(this.GetContextOrThrow(name)).Handler;

        public T Remove<T>() where T : class, IChannelHandler => (T)this.Remove(this.GetContextOrThrow<T>()).Handler;

        private AbstractChannelHandlerContext Remove(AbstractChannelHandlerContext ctx)
        {
            Contract.Assert(ctx != this.head && ctx != this.tail);

            lock (this)
            {
                var executor = this.ExecutorSafe(ctx.executor);

                Remove0(ctx);

                if (executor == null)
                {
                    this.CallHandlerCallbackLater(ctx, false);
                    return ctx;
                }
                if (!executor.InEventLoop)
                {
                    executor.Execute((s, c) => ((DefaultChannelPipeline)s).CallHandlerRemoved0((AbstractChannelHandlerContext)c), this, ctx);
                    return ctx;
                }
            }
            this.CallHandlerRemoved0(ctx);
            return ctx;
        }

        private static void Remove0(AbstractChannelHandlerContext context)
        {
            var prev = context.Prev;
            var next = context.Next;
            prev.Next = next;
            next.Prev = prev;
        }

        public IChannelHandler RemoveFirst()
        {
            if (this.head.Next == this.tail)
            {
                throw new InvalidOperationException("Pipeline is empty.");
            }
            return this.Remove(this.head.Next).Handler;
        }

        public IChannelHandler RemoveLast()
        {
            if (this.head.Next == this.tail)
            {
                throw new InvalidOperationException("Pipeline is empty.");
            }
            return this.Remove(this.tail.Prev).Handler;
        }

        public void Replace(IChannelHandler oldHandler, string newName, IChannelHandler newHandler) => this.Replace(this.GetContextOrThrow(oldHandler), newName, newHandler);
        public void Replace(string oldName, string newName, IChannelHandler newHandler) => this.Replace(this.GetContextOrThrow(oldName), newName, newHandler);
        public T Replace<T>(string newName, IChannelHandler newHandler) where T : class, IChannelHandler => (T)this.Replace(this.GetContextOrThrow<T>(), newName, newHandler);

        private IChannelHandler Replace(AbstractChannelHandlerContext ctx, string newName, IChannelHandler newHandler)
        {
            Contract.Requires(newHandler != null);
            Contract.Assert(ctx != this.head && ctx != this.tail);

            AbstractChannelHandlerContext newCtx;
            lock (this)
            {
                CheckMultiplicity(newHandler);
                if (newName == null)
                {
                    newName = this.GenerateName(newHandler);
                }
                else
                {
                    bool sameName = ctx.Name.Equals(newName, StringComparison.Ordinal);
                    if (!sameName)
                    {
                        this.CheckDuplicateName(newName);
                    }
                }

                newCtx = this.NewContext(ctx.executor, newName, newHandler);
                var executor = this.ExecutorSafe(ctx.executor);

                Replace0(ctx, newCtx);

                // If the executor is null it means that the channel was not registered on an event loop yet.
                // In this case we replace the context in the pipeline
                // and add a task that will signal handler it was added or removed
                // once the channel is registered.
                if (executor == null)
                {
                    this.CallHandlerCallbackLater(newCtx, true);
                    this.CallHandlerCallbackLater(ctx, false);
                    return ctx.Handler;
                }

                if (!executor.InEventLoop)
                {
                    executor.Execute(() =>
                    {
                        // Indicate new handler was added first (i.e. before old handler removed)
                        // because "removed" will trigger ChannelRead() or Flush() on newHandler and
                        // those event handlers must be called after handler was signaled "added".
                        this.CallHandlerAdded0(newCtx);
                        this.CallHandlerRemoved0(ctx);
                    });
                    return ctx.Handler;
                }
            }
            // Indicate new handler was added first (i.e. before old handler removed)
            // because "removed" will trigger ChannelRead() or Flush() on newHandler and
            // those event handlers must be called after handler was signaled "added".
            this.CallHandlerAdded0(newCtx);
            this.CallHandlerRemoved0(ctx);
            return ctx.Handler;
        }

        private static void Replace0(AbstractChannelHandlerContext oldCtx, AbstractChannelHandlerContext newCtx)
        {
            var prev = oldCtx.Prev;
            var next = oldCtx.Next;
            newCtx.Prev = prev;
            newCtx.Next = next;

            // Finish the replacement of oldCtx with newCtx in the linked list.
            // Note that this doesn't mean events will be sent to the new handler immediately
            // because we are currently at the event handler thread and no more than one handler methods can be invoked
            // at the same time (we ensured that in replace().)
            prev.Next = newCtx;
            next.Prev = newCtx;

            // update the reference to the replacement so forward of buffered content will work correctly
            oldCtx.Prev = newCtx;
            oldCtx.Next = newCtx;
        }

        public IChannelHandler First() => this.FirstContext()?.Handler;

        public IChannelHandler Last() => this.LastContext()?.Handler;

        public IChannelHandler Get(string name) => this.Context(name)?.Handler;

        public T Get<T>() where T : class, IChannelHandler => (T)this.Context<T>()?.Handler;
    }
}