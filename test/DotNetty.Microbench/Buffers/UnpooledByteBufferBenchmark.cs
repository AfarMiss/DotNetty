// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// namespace DotNetty.Microbench.Buffers
// {
//     using BenchmarkDotNet.Attributes;
//     using BenchmarkDotNet.Jobs;
//     using DotNetty.Buffers;
//     using DotNetty.Common;
// #if NET472
//     using BenchmarkDotNet.Diagnostics.Windows.Configs;
// #endif
//
// #if !NET472
//     [SimpleJob(RuntimeMoniker.NetCoreApp31)]
// #else
//     [SimpleJob(RuntimeMoniker.Net472)]
//     [InliningDiagnoser(true, true)]
// #endif
//     [BenchmarkCategory("ByteBuffer")]
//     public class UnpooledByteBufferBenchmark
//     {
//         static UnpooledByteBufferBenchmark()
//         {
//             // ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
//         }
//
//         IByteBuffer unsafeBuffer;
//         IByteBuffer buffer;
//
//         [GlobalSetup]
//         public void GlobalSetup()
//         {
//             this.unsafeBuffer = new UnpooledUnsafeDirectByteBuffer(UnpooledByteBufferAllocator.Default, 8, int.MaxValue);
//             this.buffer = new UnpooledHeapByteBuffer(UnpooledByteBufferAllocator.Default, 8, int.MaxValue);
//             this.buffer.Write<long>(1L);
//         }
//
//         [GlobalCleanup]
//         public void GlobalCleanup()
//         {
//             this.unsafeBuffer.Release();
//             this.buffer.Release();
//         }
//
//         [Benchmark]
//         public byte GetByteUnsafe() => this.unsafeBuffer.Get<byte>(0);
//
//         [Benchmark]
//         public byte GetByte() => this.buffer.Get<byte>(0);
//
//         [Benchmark]
//         public short GetShortUnsafe() => this.unsafeBuffer.Get<short>(0);
//
//         [Benchmark]
//         public short GetShort() => this.buffer.Get<short>(0);
//
//         // [Benchmark]
//         // public int GetMediumUnsafe() => this.unsafeBuffer.GetMedium(0);
//         //
//         // [Benchmark]
//         // public int GetMedium() => this.buffer.GetMedium(0);
//
//         [Benchmark]
//         public int GetIntUnsafe() => this.unsafeBuffer.Get<int>(0);
//
//         [Benchmark]
//         public int GetInt() => this.buffer.Get<int>(0);
//
//         [Benchmark]
//         public long GetLongUnsafe() => this.unsafeBuffer.Get<long>(0);
//
//         [Benchmark]
//         public long GetLong() => this.buffer.Get<long>(0);
//     }
// }