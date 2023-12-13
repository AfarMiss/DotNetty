// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Handlers.Logging
{
    using DotNetty.Common.Internal.Logging;

    public enum LogLevel
    {
        TRACE = Common.Internal.Logging.InternalLogLevel.TRACE,
        DEBUG = Common.Internal.Logging.InternalLogLevel.DEBUG,
        INFO = Common.Internal.Logging.InternalLogLevel.INFO,
        WARN = Common.Internal.Logging.InternalLogLevel.WARN,
        ERROR = Common.Internal.Logging.InternalLogLevel.ERROR,
    }
}