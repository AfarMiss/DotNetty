namespace DotNetty.Common.Concurrency
{
    public interface ICallable<out T>
    {
        T Call();
    }
}