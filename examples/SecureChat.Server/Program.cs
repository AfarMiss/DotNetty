// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
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

    public class dddd
    {
        public int value;
    }
    public class ThreadTest_V0
    {
        public ConstantMap Map = new ConstantMap();
        public AttributeKey<int> fff = AttributeKey<int>.ValueOf("1");
        public object gg = new object();
        private static int refint;

        public void Add1()
        {
            int index = 0;
            while (index++ < 10)//100万次
            {
                var fff = this.Map.Update(this.fff, (reference1) =>
                {
                    Console.WriteLine($"{reference1}");

                    var fff = reference1 + 1;
                    return fff;
                });
                Console.WriteLine($"V0：count = {fff}");
            }
        }

        public void Add2()
        {
            int index = 0;
            while (index++ < 10)//100万次
            {
                var fff = this.Map.Update(this.fff, (reference1) =>
                {
                    Console.WriteLine($"{reference1}");

                    var fff = reference1 + 1;
                    return fff;
                });
                Console.WriteLine($"V1：count = {fff}");
            }
        }
        public void Add3()
        {
            int index = 0;
            while (index++ < 10)//100万次
            {
                var fff = this.Map.Update(this.fff, (reference1) =>
                {
                    Console.WriteLine($"{reference1}");
                    var fff = reference1 + 1;
                    return fff;
                });
                Console.WriteLine($"V3：count = {fff}");
            }
        }
    }
    
    class Program
    {
        static async Task RunServerAsync()
        {
            // ThreadTest_V0 testV0 = new ThreadTest_V0();
            // Thread th1 = new Thread(testV0.Add1);
            // Thread th2 = new Thread(testV0.Add2);
            // Thread th3 = new Thread(testV0.Add3);
            //
            // th1.Start();
            // th2.Start();
            // th3.Start();
            // th1.Join();
            // th2.Join();
            // th3.Join();
            // Console.ReadLine();
            //
            // // Console.WriteLine($"V0：count = {testV0.count}");
            // return;
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