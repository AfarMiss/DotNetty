using System;

namespace DotNetty.Common.Internal.Logging
{
    internal sealed class InternalDebugLogger : AbstractInternalLogger
    {
        public override bool TraceEnabled => true;
        public override bool DebugEnabled => true;
        public override bool InfoEnabled => true;
        public override bool WarnEnabled => true;
        public override bool ErrorEnabled => true;
        
        public InternalDebugLogger(string name) : base(name) { }

        public override void Trace(string msg)
        {
            var message = MessageFormatter.Format(InternalLogLevel.TRACE, this.Name, msg);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Trace(string format, object arg)
        {
            if (this.TraceEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.TRACE, this.Name, format, arg);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Trace(string format, object argA, object argB)
        {
            if (this.TraceEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.TRACE, this.Name, format, argA, argB);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Trace(string format, params object[] arguments)
        {
            if (this.TraceEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.TRACE, this.Name, format, arguments);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Trace(string msg, Exception t)
        {
            var message = MessageFormatter.Format(InternalLogLevel.TRACE, this.Name, msg, t);
            System.Diagnostics.Debug.WriteLine(message);
        }
        
        public override void Debug(string msg)
        {
            var message = MessageFormatter.Format(InternalLogLevel.DEBUG, this.Name, msg);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Debug(string format, object arg)
        {
            if (this.DebugEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.DEBUG, this.Name, format, arg);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Debug(string format, object argA, object argB)
        {
            if (this.DebugEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.DEBUG, this.Name, format, argA, argB);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Debug(string format, params object[] arguments)
        {
            if (this.DebugEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.DEBUG, this.Name, format, arguments);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Debug(string msg, Exception t)
        {
            var message = MessageFormatter.Format(InternalLogLevel.DEBUG, this.Name, msg, t);
            System.Diagnostics.Debug.WriteLine(message);
        }
        
        public override void Info(string msg)
        {
            var message = MessageFormatter.Format(InternalLogLevel.INFO, this.Name, msg);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Info(string format, object arg)
        {
            if (this.InfoEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.INFO, this.Name, format, arg);
                System.Diagnostics.Trace.WriteLine(message);
            }
        }

        public override void Info(string format, object argA, object argB)
        {
            if (this.InfoEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.INFO, this.Name, format, argA, argB);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Info(string format, params object[] arguments)
        {
            if (this.InfoEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.INFO, this.Name, format, format, arguments);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Info(string msg, Exception t)
        {
            var message = MessageFormatter.Format(InternalLogLevel.INFO, this.Name, msg, t);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Warn(string msg)
        {
            var message = MessageFormatter.Format(InternalLogLevel.WARN, this.Name, msg);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Warn(string format, object arg)
        {
            if (this.WarnEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.WARN, this.Name, format, arg);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Warn(string format, object argA, object argB)
        {
            if (this.WarnEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.WARN, this.Name, format, argA, argB);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Warn(string format, params object[] arguments)
        {
            if (this.WarnEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.WARN, this.Name, format, arguments);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Warn(string msg, Exception t)
        {
            var message = MessageFormatter.Format(InternalLogLevel.WARN, this.Name, msg, t);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Error(string msg)
        {
            var message = MessageFormatter.Format(InternalLogLevel.ERROR, this.Name, msg);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public override void Error(string format, object arg)
        {
            if (this.ErrorEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.ERROR, this.Name, format, arg);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Error(string format, object argA, object argB)
        {
            if (this.ErrorEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.ERROR, this.Name, format, argA, argB);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Error(string format, params object[] arguments)
        {
            if (this.ErrorEnabled)
            {
                var message = MessageFormatter.Format(InternalLogLevel.ERROR, this.Name, format, arguments);
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        public override void Error(string msg, Exception t)
        {
            var message = MessageFormatter.Format(InternalLogLevel.ERROR, this.Name, msg, t);
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}