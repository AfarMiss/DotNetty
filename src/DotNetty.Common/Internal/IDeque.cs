namespace DotNetty.Common.Internal
{
    public interface IDeque<T> : IQueue<T>
    {
        bool TryDequeueLast(out T item);
    }
}