using System.Buffers.Binary;
using System.Net.Sockets;

namespace RobocadCs.Internal.Common
{
    internal static class SocketUtil
    {
        public static byte[] Uint32Le(uint v) 
        {
            byte[] buf = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buf, v);
            return buf;
        }

        public static uint ReadUint32Le(byte[] b) => BinaryPrimitives.ReadUInt32LittleEndian(b);

        public static bool SendAll(Socket s, byte[] data)
        {
            int sent = 0;
            while (sent < data.Length)
            {
                try
                {
                    int n = s.Send(data, sent, data.Length - sent, SocketFlags.None);
                    if (n <= 0) return false;
                    sent += n;
                }
                catch { return false; }
            }
            return true;
        }

        public static bool ReceiveAll(Socket s, byte[] buffer, int count)
        {
            int got = 0;
            while (got < count)
            {
                try
                {
                    int n = s.Receive(buffer, got, count - got, SocketFlags.None);
                    if (n <= 0) return false;
                    got += n;
                }
                catch { return false; }
            }
            return true;
        }
    }
    
}
