using System;
using System.Diagnostics.Contracts;

namespace DotNetty.Transport.Channels
{
    public sealed class ActionChannelInitializer<T> : ChannelInitializer<T> where T : IChannel
    {
        private readonly Action<T> initializationAction;

        public ActionChannelInitializer(Action<T> initializationAction)
        {
            this.initializationAction = initializationAction;
        }

        protected override void InitChannel(T channel) => this.initializationAction(channel);

        public override string ToString() => nameof(ActionChannelInitializer<T>) + "[" + typeof(T).Name + "]";
    }
}