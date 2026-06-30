using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace RobocadCs.Internal.Common
{
    internal sealed class ByteWriter
    {
        private readonly byte[] _buf;
        private int _pos;

        public ByteWriter(int size)
        {
            _buf = new byte[size];
        }

        public void WriteInt32(int v)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buf.AsSpan(_pos), v);
            _pos += 4;
        }

        public void WriteUInt16(ushort v)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_buf.AsSpan(_pos), v);
            _pos += 2;
        }

        public void WriteFloat(float v)
        {
            BinaryPrimitives.WriteSingleLittleEndian(_buf.AsSpan(_pos), v);
            _pos += 4;
        }
        
        public void WriteByte(byte v) => _buf[_pos++] = v;

        public byte[] ToArray() => _buf;
    }

    internal sealed class ByteReader
    {
        private readonly byte[] _b;
        private int _pos;

        public ByteReader(byte[] b)
        {
            _b = b;
        }

        public int ReadInt32()
        {
            int v = BinaryPrimitives.ReadInt32LittleEndian(_b.AsSpan(_pos));
            _pos += 4;
            return v;
        }

        public float ReadFloat()
        {
            float v = BinaryPrimitives.ReadSingleLittleEndian(_b.AsSpan(_pos));
            _pos += 4;
            return v;
        }

        public ushort ReadUInt16()
        {
            ushort v = BinaryPrimitives.ReadUInt16LittleEndian(_b.AsSpan(_pos));
            _pos += 2;
            return v;
        }

        public byte ReadByte() => _b[_pos++];
    }

    internal static class TimeUnits
    {
        private static readonly Stopwatch Sw = Stopwatch.StartNew();
        public static long Now() => (long)(Sw.Elapsed.TotalMilliseconds * 10.0);
    }
}