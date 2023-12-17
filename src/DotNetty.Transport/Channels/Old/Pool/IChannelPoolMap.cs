namespace DotNetty.Transport.Channels.Pool
{
    public interface IChannelPoolMap<in TKey, out TPool> where TPool : IChannelPool
    {
        TPool Get(TKey key);
        bool Contains(TKey key);
    }
}