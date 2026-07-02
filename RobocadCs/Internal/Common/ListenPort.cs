using System.Text;
using System.Threading;

namespace RobocadCs.Internal.Common;

public class ListenPort : PortBase
{
    public ListenPort(Robot robot, int port) : base(robot, port) {} 

    public void Start() => StartThread(Listening);
        
    private void Listening()
    {
        if(!Connect("LP")) return;

        byte[] msg = Encoding.Unicode.GetBytes("Wait for data");
        byte[] msgLen = SocketUtil.Uint32Le((uint)msg.Length);
        byte[] lenBuf = new byte[4];

        while (!StopThread)
        {
            if (!SocketUtil.SendAll(Sct, msgLen)) break;
            if (!SocketUtil.SendAll(Sct, msg)) break;

            if (!SocketUtil.ReceiveAll(Sct, lenBuf, 4)) break;
            int length = (int)SocketUtil.ReadUint32Le(lenBuf);

            byte[] buffer = new byte[length];
            if (length > 0 && !SocketUtil.ReceiveAll(Sct, buffer, length)) break;

            lock (_lock) { _outBytes = buffer; }
            Thread.Sleep(4);
        }

        Robot.WriteLog("LP: Disconnected: " + Port);
        SafeClose();
    }
}