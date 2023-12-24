using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DotNetty.Buffers;
using DotNetty.Common.Internal;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common;

namespace DotNetty.Transport.Channels
{
    internal sealed class DefaultChannelId : IChannelId
    {
        private const int MachineIdLen = 8;
        private const int ProcessIdLen = 4;
        // Maximal value for 64bit systems is 2^22.  See man 5 proc.
        private const int MaxProcessId = 4194304;
        private const int SequenceLen = 4;
        private const int TimestampLen = 8;
        private const int RandomLen = 4;
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DefaultChannelId>();
        private static readonly Regex MachineIdPattern = new Regex("^(?:[0-9a-fA-F][:-]?){6,8}$");
        private static readonly byte[] MachineId;
        private static readonly int ProcessId;
        private static int nextSequence;
        private static int seed = (int)(Stopwatch.GetTimestamp() & 0xFFFFFFFF); //used to safly cast long to int, because the timestamp returned is long and it doesn't fit into an int
        private static readonly ThreadLocal<Random> ThreadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed))); //used to simulate java ThreadLocalRandom
        private readonly byte[] data = new byte[MachineIdLen + ProcessIdLen + SequenceLen + TimestampLen + RandomLen];
        private int hashCode;

        private string longValue;
        private string shortValue;

        static DefaultChannelId()
        {
            var processId = DefaultProcessId();
            if (Logger.DebugEnabled)
            {
                Logger.Debug("-Dio.netty.processId: {} (auto-detected)", processId);
            }
            ProcessId = processId;
            
            var machineId = DefaultMachineId();
            if (Logger.DebugEnabled)
            {
                Logger.Debug("-Dio.netty.machineId: {} (auto-detected)", MacAddressUtil.FormatAddress(machineId));
            }
            MachineId = machineId;
        }

        public string AsShortText()
        {
            throw new NotImplementedException();
            // string asShortText = this.shortValue;
            // if (asShortText == null)
            // {
            //     this.shortValue = asShortText = ByteBufferUtil.HexDump(this.data, MachineIdLen + ProcessIdLen + SequenceLen + TimestampLen, RandomLen);
            // }
            //
            // return asShortText;
        }

        public string AsLongText()
        {
            throw new NotImplementedException();
            // string asLongText = this.longValue;
            // if (asLongText == null)
            // {
            //     this.longValue = asLongText = this.NewLongValue();
            // }
            // return asLongText;
        }

        public int CompareTo(IChannelId other) => 0;

        private static int DefaultProcessId()
        {
            var pId = Process.GetCurrentProcess().Id;
            if (pId <= 0)
            {
                pId = ThreadLocalRandom.Value.Next(MaxProcessId + 1);
            }
            return pId;
        }

        public static DefaultChannelId NewInstance()
        {
            var id = new DefaultChannelId();
            id.Init();
            return id;
        }

        private static byte[] DefaultMachineId()
        {
            byte[] bestMacAddr = MacAddressUtil.GetBestAvailableMac();
            if (bestMacAddr == null) {
                bestMacAddr = new byte[MacAddressUtil.MacAddressLength];
                ThreadLocalRandom.Value.NextBytes(bestMacAddr);
                Logger.Warn(
                    "Failed to find a usable hardware address from the network interfaces; using random bytes: {}",
                    MacAddressUtil.FormatAddress(bestMacAddr));
            }
            return bestMacAddr;
        }


        // private string NewLongValue()
        // {
        //     var buf = new StringBuilder(2 * this.data.Length + 5);
        //     int i = 0;
        //     i = this.AppendHexDumpField(buf, i, MachineIdLen);
        //     i = this.AppendHexDumpField(buf, i, ProcessIdLen);
        //     i = this.AppendHexDumpField(buf, i, SequenceLen);
        //     i = this.AppendHexDumpField(buf, i, TimestampLen);
        //     i = this.AppendHexDumpField(buf, i, RandomLen);
        //     Debug.Assert(i == this.data.Length);
        //     return buf.ToString().Substring(0, buf.Length - 1);
        // }
        //
        // private int AppendHexDumpField(StringBuilder buf, int i, int length)
        // {
        //     buf.Append(ByteBufferUtil.HexDump(this.data, i, length));
        //     buf.Append('-');
        //     i += length;
        //     return i;
        // }

        private void Init()
        {
            int i = 0;
            // machineId
            Array.Copy(MachineId, 0, this.data, i, MachineIdLen);
            i += MachineIdLen;

            // processId
            i = this.WriteInt(i, ProcessId);

            // sequence
            i = this.WriteInt(i, Interlocked.Increment(ref nextSequence));

            // timestamp (kind of)
            long ticks = Stopwatch.GetTimestamp();
            long nanos = (ticks / Stopwatch.Frequency) * 1000000000;
            long millis = (ticks / Stopwatch.Frequency) * 1000;
            i = this.WriteLong(i, ByteBufferUtil.SwapLong(nanos) ^ millis);

            // random
            int random = ThreadLocalRandom.Value.Next();
            this.hashCode = random;
            i = this.WriteInt(i, random);

            Debug.Assert(i == this.data.Length);
        }

        private int WriteInt(int i, int value)
        {
            uint val = (uint)value;
            this.data[i++] = (byte)(val >> 24);
            this.data[i++] = (byte)(val >> 16);
            this.data[i++] = (byte)(val >> 8);
            this.data[i++] = (byte)value;
            return i;
        }

        private int WriteLong(int i, long value)
        {
            ulong val = (ulong)value;
            this.data[i++] = (byte)(val >> 56);
            this.data[i++] = (byte)(val >> 48);
            this.data[i++] = (byte)(val >> 40);
            this.data[i++] = (byte)(val >> 32);
            this.data[i++] = (byte)(val >> 24);
            this.data[i++] = (byte)(val >> 16);
            this.data[i++] = (byte)(val >> 8);
            this.data[i++] = (byte)value;
            return i;
        }

        public override int GetHashCode() => this.hashCode;

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is DefaultChannelId channelId))
            {
                return false;
            }

            return Equals(this.data, channelId.data);
        }

        public override string ToString() => this.AsShortText();
    }
}
