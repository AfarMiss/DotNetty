namespace DotNetty.Buffers
{
    class UnpooledSlicedByteBuffer : AbstractUnpooledSlicedByteBuffer
    {
        public override int Capacity => this.MaxCapacity;

        internal UnpooledSlicedByteBuffer(AbstractByteBuffer buffer, int index, int length) : base(buffer, index, length)
        {
        }
        protected AbstractByteBuffer UnwrapCore() => (AbstractByteBuffer)this.Unwrap();

        protected internal override T _Get<T>(int index) => this.UnwrapCore()._Get<T>(this.Idx(index));
        protected internal override void _Set<T>(int index, T value) => this.UnwrapCore()._Set<T>(this.Idx(index), value);
    }
}
