using System;

namespace DotNetty.Common.Utilities
{
    public sealed class Signal : Exception, IConstant, IComparable, IComparable<Signal>
    {
        private static readonly SignalConstantPool Pool = new SignalConstantPool();

        public static Signal ValueOf(string name) => (Signal)Pool.ValueOf(name);

        private SignalConstant constant;

        private sealed class SignalConstantPool : ConstantPool<Signal>
        {
            protected override Signal GetInitialValue(in int id, string name)
            {
                var signal = new Signal();
                signal.constant = SignalConstant.ValueOf(signal.Name);
                return signal;
            }
        }
        public void Expect(Signal signal)
        {
            if (!ReferenceEquals(this, signal))
            {
                throw new InvalidOperationException($"unexpected signal: {signal}");
            }
        }

        public int Id => this.constant.Id;

        public string Name => this.constant.Name;
        
        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => this.Id;

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return 0;
            }
            if (!ReferenceEquals(obj, null) && obj is Signal)
            {
                return this.CompareTo((Signal)obj);
            }

            throw new Exception("failed to compare two different signal constants");
        }

        public int CompareTo(Signal other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            return this.constant.CompareTo(other.constant);
        }

        public override string ToString() => this.Name;

        private sealed class SignalConstant : AbstractConstant<SignalConstant>
        {

        }
    }
}
