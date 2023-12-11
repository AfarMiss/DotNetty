// using System;
// using System.Buffers;
//
// namespace DotNetty.Buffers
// {
//     internal class NewCompositeByteBuffer : ReadOnlySequenceSegment<byte>
//     {
//         public NewCompositeByteBuffer(ReadOnlyMemory<byte> memory) => this.Memory = memory;
//
//         public ReadOnlySequence<byte> Append(ReadOnlyMemory<byte> memory)
//         {
//             var segment = new NewCompositeByteBuffer(memory);
//             segment.RunningIndex = this.RunningIndex + this.Memory.Length;
//             this.Next = segment;
//             // return segment;
//             var gg = new ReadOnlySequence<byte>(this, 0, segment, segment.Memory.Length);
//             var sequenceReader = new SequenceReader<byte>(gg);
//             sequenceReader.Advance();
//         }
//
//         // public ReadOnlySequence<byte> ToReadOnlySequence(ReadOnlySequenceSegment<byte> endSegment)
//         // {
//         //     return new ReadOnlySequence<byte>(this, 0, endSegment, endSegment.Memory.Length);
//         // }
//     }
// }