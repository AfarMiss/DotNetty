// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Microbench.Http
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Jobs;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [BenchmarkCategory("Http")]
    public class WriteBytesVsShortOrMediumBenchmark
    {
        const int CrlfShort = (HttpConstants.CarriageReturn << 8) + HttpConstants.LineFeed;
        const int ZeroCrlfMedium = ('0' << 16) + (HttpConstants.CarriageReturn << 8) + HttpConstants.LineFeed;
        static readonly byte[] Crlf = { HttpConstants.CarriageReturn, HttpConstants.LineFeed };
        static readonly byte[] ZeroCrlf = { (byte)'0', HttpConstants.CarriageReturn, HttpConstants.LineFeed };

        IByteBuffer buf;

        [GlobalSetup]
        public void GlobalSetup()
        {
            // ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
            this.buf = ByteBuffer.Buffer(16);
        }

        [Benchmark]
        public IByteBuffer ShortInt()
        {
            this.buf.Write<short>(CrlfShort);
            this.buf.ResetWriterIndex();
            return this.buf;
        }

        // [Benchmark]
        // public IByteBuffer MediumInt() => this.buf.WriteMedium(ZeroCrlfMedium).ResetWriterIndex();

        [Benchmark]
        public IByteBuffer ByteArray2()
        {
            this.buf.WriteBytes(Crlf);
            this.buf.ResetWriterIndex();
            return this.buf;
        }

        [Benchmark]
        public IByteBuffer ByteArray3()
        {
            this.buf.WriteBytes(ZeroCrlf);
            this.buf.ResetWriterIndex();
            return this.buf;
        }

        [Benchmark]
        public IByteBuffer ChainedBytes2()
        {
            this.buf.Write<byte>(HttpConstants.CarriageReturn);
            this.buf.Write<byte>(HttpConstants.LineFeed);
            this.buf.ResetWriterIndex();
            return this.buf;
        }

        [Benchmark]
        public IByteBuffer ChainedBytes3()
        {
            this.buf.Write<byte>('0');
            this.buf.Write<byte>(HttpConstants.CarriageReturn);
            this.buf.Write<byte>(HttpConstants.LineFeed);
            this.buf.ResetWriterIndex();
            return this.buf;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.buf?.SafeRelease();
            this.buf = null;
        }
    }
}
