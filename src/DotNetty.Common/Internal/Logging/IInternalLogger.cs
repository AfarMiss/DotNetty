using System;

namespace DotNetty.Common.Internal.Logging
{
    public interface IInternalLogger
    {
        string Name { get; }

        bool TraceEnabled { get; }
        bool DebugEnabled { get; }
        bool InfoEnabled { get; }
        bool WarnEnabled { get; }
        bool ErrorEnabled { get; }
        
        bool IsEnabled(InternalLogLevel level);

        void Trace(string msg);
        void Trace(string format, object arg);
        void Trace(string format, object argA, object argB);
        void Trace(string format, params object[] arguments);
        void Trace(string msg, Exception t);
        void Trace(Exception t);

        void Debug(string msg);
        void Debug(string format, object arg);
        void Debug(string format, object argA, object argB);
        void Debug(string format, params object[] arguments);
        void Debug(string msg, Exception t);
        void Debug(Exception t);

        void Info(string msg);
        void Info(string format, object arg);
        void Info(string format, object argA, object argB);
        void Info(string format, params object[] arguments);
        void Info(string msg, Exception t);
        void Info(Exception t);
        void Warn(string msg);
        void Warn(string format, object arg);
        void Warn(string format, params object[] arguments);
        void Warn(string format, object argA, object argB);
        void Warn(string msg, Exception t);
        void Warn(Exception t);

        void Error(string msg);
        void Error(string format, object arg);
        void Error(string format, object argA, object argB);
        void Error(string format, params object[] arguments);
        void Error(string msg, Exception t);
        void Error(Exception t);
        
        void Log(InternalLogLevel level, string msg);
        void Log(InternalLogLevel level, string format, object arg);
        void Log(InternalLogLevel level, string format, object argA, object argB);
        void Log(InternalLogLevel level, string format, params object[] arguments);
        void Log(InternalLogLevel level, string msg, Exception t);
        void Log(InternalLogLevel level, Exception t);
    }
}