using System;
using System.Linq;

namespace DotNetty.Transport.Channels.Groups
{
    public static class ChannelMatchers
    {
        private static readonly IChannelMatcher AllMatcher = new AllChannelMatcher();
        private static readonly IChannelMatcher ServerChannelMatcher = IsInstanceOf(typeof(IServerChannel));
        private static readonly IChannelMatcher NonServerChannelMatcher = IsNotInstanceOf(typeof(IServerChannel));

        public static IChannelMatcher IsServerChannel() => ServerChannelMatcher;

        public static IChannelMatcher IsNonServerChannel() => NonServerChannelMatcher;

        public static IChannelMatcher All() => AllMatcher;

        public static IChannelMatcher IsNot(IChannel channel) => Invert(Is(channel));

        public static IChannelMatcher Is(IChannel channel) => new InstanceMatcher(channel);

        public static IChannelMatcher IsInstanceOf(Type type) => new TypeMatcher(type);

        public static IChannelMatcher IsNotInstanceOf(Type type) => Invert(IsInstanceOf(type));

        public static IChannelMatcher Invert(IChannelMatcher matcher) => new InvertMatcher(matcher);

        public static IChannelMatcher Compose(params IChannelMatcher[] matchers)
        {
            if (matchers.Length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(matchers));
            }
            if (matchers.Length == 1)
            {
                return matchers[0];
            }
            return new CompositeMatcher(matchers);
        }

        private sealed class AllChannelMatcher : IChannelMatcher
        {
            public bool Matches(IChannel channel) => true;
        }

        private sealed class CompositeMatcher : IChannelMatcher
        {
            private readonly IChannelMatcher[] matchers;

            public CompositeMatcher(params IChannelMatcher[] matchers)
            {
                this.matchers = matchers;
            }

            public bool Matches(IChannel channel)
            {
                return this.matchers.All(matcher => matcher.Matches(channel));
            }
        }

        private sealed class InvertMatcher : IChannelMatcher
        {
            private readonly IChannelMatcher matcher;

            public InvertMatcher(IChannelMatcher matcher)
            {
                this.matcher = matcher;
            }

            public bool Matches(IChannel channel) => !this.matcher.Matches(channel);
        }

        private sealed class InstanceMatcher : IChannelMatcher
        {
            private readonly IChannel channel;

            public InstanceMatcher(IChannel channel)
            {
                this.channel = channel;
            }

            public bool Matches(IChannel ch) => this.channel == ch;
        }

        private sealed class TypeMatcher : IChannelMatcher
        {
            private readonly Type type;

            public TypeMatcher(Type clazz)
            {
                this.type = clazz;
            }

            public bool Matches(IChannel channel) => this.type.IsInstanceOfType(channel);
        }
    }
}