using System;

namespace DotNetty.Common.Utilities
{
    public sealed class Signal : Exception
    {
        public static Signal ValueOf(string name) => new Signal(name);
        private readonly SignalConstant constant;

        private Signal(string name) => this.constant = SignalConstant.ValueOf(name);

        public override bool Equals(object obj) => ReferenceEquals(this, obj);



        public class SignalConstantPool : ConstantPool
        {

        }
        
        public class SignalConstant : AbstractConstant<SignalConstantPool, SignalConstant>
        {
            
        }
    }
}
