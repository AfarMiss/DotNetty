namespace DotNetty.Common.Utilities
{
    /// <summary>
    ///     Provides a mechanism to iterate over a collection of bytes.
    /// </summary>
    public interface IByteProcessor
    {
        bool Process(byte value);
    }

    public sealed class IndexOfProcessor : IByteProcessor
    {
        private readonly byte byteToFind;

        public IndexOfProcessor(byte byteToFind)
        {
            this.byteToFind = byteToFind;
        }

        public bool Process(byte value) => value != this.byteToFind;
    }

    public sealed class IndexNotOfProcessor : IByteProcessor
    {
        private readonly byte byteToNotFind;

        public IndexNotOfProcessor(byte byteToNotFind)
        {
            this.byteToNotFind = byteToNotFind;
        }

        public bool Process(byte value) => value == this.byteToNotFind;
    }
}