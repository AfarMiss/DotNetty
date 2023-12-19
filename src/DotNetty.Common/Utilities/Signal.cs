using System;

namespace DotNetty.Common.Utilities
{
    public sealed class Signal : Exception, IComparable, IComparable<Signal>
    {
        public static Signal ValueOf(string name) => new Signal(name);
        private readonly SignalConstant constant;

        private Signal(string name) => this.constant = SignalConstant.ValueOf(name);

        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => this.constant.Id;

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

        public override string ToString() => this.constant.Name;

        public class SignalConstantPool : ConstantPool
        {

        }
        
        public class SignalConstant : AbstractConstant<SignalConstantPool, SignalConstant>
        {
            
        }
    }
}
