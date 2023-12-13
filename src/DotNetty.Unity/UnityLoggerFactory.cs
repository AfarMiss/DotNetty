using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Common;
using DotNetty.Common.Internal.Logging;
using UnityEngine;

namespace DotNetty.Unity
{
    public class UnityLoggerFactory
    {
        public static readonly UnityLoggerFactory Default = new UnityLoggerFactory(InternalLogLevel.ALL);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnInitialize()
        {
            InternalLoggerFactory.Factory = Default.GetLogger;
        }

        private Dictionary<string, IInternalLogger> repositories = new Dictionary<string, IInternalLogger>();
        public InternalLogLevel Level { get; set; }

        public UnityLoggerFactory(InternalLogLevel level)
        {
            this.Level = level;
        }

        public IInternalLogger GetLogger(string name)
        {
            if (!this.repositories.TryGetValue(name, out var logger))
            {
                logger = new UnityLogger(name, this.Level);
                this.repositories[name] = logger;
            }

            return logger;
        }

        private class UnityLogger : AbstractInternalLogger
        {
            private InternalLogLevel level = InternalLogLevel.DEBUG;
            public UnityLogger(string name) : base(name)
            {
            }

            public UnityLogger(string name, InternalLogLevel level) : base(name)
            {
                this.level = level;
            }

            public override bool TraceEnabled => InternalLogLevel.TRACE >= level;

            public override bool DebugEnabled => InternalLogLevel.DEBUG >= level;

            public override bool InfoEnabled => InternalLogLevel.INFO >= level;

            public override bool WarnEnabled => InternalLogLevel.WARN >= level;

            public override bool ErrorEnabled => InternalLogLevel.ERROR >= level;

            public override void Trace(string msg) => LogFormat(InternalLogLevel.TRACE, msg);

            public override void Trace(string format, object arg) => LogFormat(InternalLogLevel.TRACE, format, arg);

            public override void Trace(string format, object argA, object argB) => LogFormat(InternalLogLevel.TRACE, format, argA, argB);

            public override void Trace(string format, params object[] arguments) => LogFormat(InternalLogLevel.TRACE, format, arguments);

            public override void Trace(string msg, Exception t) => LogFormat(InternalLogLevel.TRACE, msg, t);

            public override void Debug(string msg) => LogFormat(InternalLogLevel.DEBUG, msg);

            public override void Debug(string format, object arg) => LogFormat(InternalLogLevel.DEBUG, format, arg);

            public override void Debug(string format, object argA, object argB) => LogFormat(InternalLogLevel.DEBUG, format, argA, argB);

            public override void Debug(string format, params object[] arguments) => LogFormat(InternalLogLevel.DEBUG, format, arguments);

            public override void Debug(string msg, Exception t) => LogFormat(InternalLogLevel.DEBUG, msg, t);

            public override void Info(string msg) => LogFormat(InternalLogLevel.INFO, msg);

            public override void Info(string format, object arg) => LogFormat(InternalLogLevel.INFO, format, arg);

            public override void Info(string format, object argA, object argB) => LogFormat(InternalLogLevel.INFO, format, argA, argB);

            public override void Info(string format, params object[] arguments) => LogFormat(InternalLogLevel.INFO, format, arguments);

            public override void Info(string msg, Exception t) => LogFormat(InternalLogLevel.INFO, msg, t);

            public override void Warn(string msg) => LogFormat(InternalLogLevel.WARN, msg);

            public override void Warn(string format, object arg) => LogFormat(InternalLogLevel.WARN, format, arg);

            public override void Warn(string format, object argA, object argB) => LogFormat(InternalLogLevel.WARN, format, argA, argB);

            public override void Warn(string format, params object[] arguments) => LogFormat(InternalLogLevel.WARN, format, arguments);

            public override void Warn(string msg, Exception t) => LogFormat(InternalLogLevel.WARN, msg, t);

            public override void Error(string msg) => LogFormat(InternalLogLevel.ERROR, msg);

            public override void Error(string format, object arg) => LogFormat(InternalLogLevel.ERROR, format, arg);

            public override void Error(string format, object argA, object argB) => LogFormat(InternalLogLevel.ERROR, format, argA, argB);

            public override void Error(string format, params object[] arguments) => LogFormat(InternalLogLevel.ERROR, format, arguments);

            public override void Error(string msg, Exception t) => LogFormat(InternalLogLevel.ERROR, msg, t);

            private void LogFormat(InternalLogLevel level, string message, Exception e)
            {
                switch (level)
                {
                    case InternalLogLevel.OFF:
                        break;
                    case InternalLogLevel.TRACE:
                    case InternalLogLevel.DEBUG:
                    case InternalLogLevel.INFO:
                        {
                            UnityEngine.Debug.Log(MessageFormatter.Format(level, this.Name, message, e));
                            break;
                        }
                    case InternalLogLevel.WARN:
                        {
                            UnityEngine.Debug.LogWarning(MessageFormatter.Format(level, this.Name, message, e));
                            break;
                        }
                    case InternalLogLevel.ERROR:
                        {
                            UnityEngine.Debug.LogError(MessageFormatter.Format(level, this.Name, message, e));
                            break;
                        }
                }
            }

            private void LogFormat(InternalLogLevel level, string message, params object[] arguments)
            {
                switch (level)
                {
                    case InternalLogLevel.OFF:
                        break;
                    case InternalLogLevel.TRACE:
                    case InternalLogLevel.DEBUG:
                    case InternalLogLevel.INFO:
                        {
                            UnityEngine.Debug.Log(MessageFormatter.Format(level, this.Name, message, arguments));
                            break;
                        }
                    case InternalLogLevel.WARN:
                        {
                            UnityEngine.Debug.LogWarning(MessageFormatter.Format(level, this.Name, message, arguments));
                            break;
                        }
                    case InternalLogLevel.ERROR:
                        {
                            UnityEngine.Debug.LogError(MessageFormatter.Format(level, this.Name, message, arguments));
                            break;
                        }
                }
            }
        }
    }
}