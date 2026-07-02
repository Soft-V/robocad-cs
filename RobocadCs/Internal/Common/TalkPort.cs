using System.Buffers.Binary;
using System.Threading;

namespace RobocadCs.Internal.Common;

public class TalkPort : PortBase
{
    public TalkPort(Robot robot, int port) : base(robot, port)
    {
    }
    public void Start() => StartThread(Talking);

    private void Talking()
    {
        if(!Connect("TP")) return;

        byte[] ack = new byte[8];
        byte[] lenBuf = new byte[4];
            
        while (!StopThread)
        {
            byte[] toSend;
            lock (_lock) { toSend = _outBytes; }

            BinaryPrimitives.WriteUInt32LittleEndian(lenBuf, (uint)toSend.Length);
            if (!SocketUtil.SendAll(Sct, lenBuf)) break;
                
            if (toSend.Length > 0 && !SocketUtil.SendAll(Sct, toSend)) break;

            if (!SocketUtil.ReceiveAll(Sct, ack, 8)) break;
            Thread.Sleep(4);
        }

        Robot.WriteLog("TP: Disconnected: " + Port);
        SafeClose();
    }
}