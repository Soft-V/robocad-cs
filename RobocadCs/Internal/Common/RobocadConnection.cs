using System.Threading;
using RobocadCs.Internal;

namespace RobocadCs.Internal.Common;

public abstract class RobocadConnection
{
    protected Thread _thread;
    protected volatile bool _stop;
    protected ConnectionSim _conn;
    protected RobotInternal _ri;

    public void Start(ConnectionSim conn, Robot robot, RobotInternal ri)
    {
        _conn = conn; _ri = ri;
        robot.Power = 12.0f;
        _stop = false;
        _thread = new Thread(Loop) { IsBackground = true };
        _thread.Start();
    }

    public void Stop()
    {
        _stop = true;
        try { _thread?.Join(1000); } catch { }
    }

    protected abstract void Loop();

    protected abstract byte[] BuildTx();
}
