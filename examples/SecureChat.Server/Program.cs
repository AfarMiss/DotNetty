// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using DotNetty.Common.Utilities;

namespace SecureChat.Server
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using DotNetty.Codecs;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Examples.Common;

    
    class Program
    {
        static async Task RunServerAsync()
        {
            var valueOf = (object)AttributeKey<int>.ValueOf("1");
            // var valueOf1 = (object)AttributeKey<string>.ValueOf("1");
            var valueOf2 = (object)ChannelOption<string>.ValueOf("1");

            var dictionary = new Dictionary<object, bool>();
            dictionary.TryAdd(valueOf, false);
            ExampleHelper.SetConsoleLogger();

            var bossGroup = new MultiThreadEventLoopGroup(1);
            var workerGroup = new MultiThreadEventLoopGroup();

            var STRING_ENCODER = new StringEncoder();
            var STRING_DECODER = new StringDecoder();
            var SERVER_HANDLER = new SecureChatServerHandler();

            // X509Certificate2 tlsCertificate = null;
            // if (ServerSettings.IsSsl)
            // {
            //     tlsCertificate = new X509Certificate2(Path.Combine(ExampleHelper.ProcessDirectory, "dotnetty.com.pfx"), "password");
            // }
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .SetGroup(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .SetHandler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        // if (tlsCertificate != null)
                        // {
                        //     pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        // }

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(STRING_ENCODER, STRING_DECODER, SERVER_HANDLER);
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(ServerSettings.Port);

                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }

        static void Main() => RunServerAsync().Wait();
    }
}