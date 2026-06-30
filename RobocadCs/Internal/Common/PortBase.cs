using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RobocadCs.Internal.Common;

public abstract class PortBase
{
    protected readonly Robot Robot;
    protected readonly int Port;
    protected volatile bool StopThread;
    private Thread _thread;
    protected Socket Sct;
    protected readonly object _lock = new object();
    protected byte[] _outBytes = Array.Empty<byte>();

    public PortBase(Robot robot, int port)
    {
        Robot = robot;
        Port = port;
    }
        
    protected void StartThread(ThreadStart work)
    {
        StopThread = false;
        _thread = new Thread(work) { IsBackground = true };
        _thread.Start();
    }
        
    public void Stop()
    {
        StopThread = true;
        try { Sct?.Shutdown(SocketShutdown.Both); } catch { }
        try { _thread?.Join(1000); } catch { }
        finally { SafeClose(); }
    }
        
    protected void SafeClose()
    {
        try { Sct?.Shutdown(SocketShutdown.Both); } catch { }
        try { Sct?.Close(); } catch { }
    }
        
    protected bool Connect(string logPrefix)
    {
        Sct = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            Sct.Connect(new IPEndPoint(IPAddress.Loopback, Port));
            Robot.WriteLog(logPrefix + ": Connected: " + Port);
            return true;
        }
        catch (SocketException)
        {
            Robot.WriteLog(logPrefix + ": Failed to connect on port " + Port);
            SafeClose();
            return false;
        }
    }
        
    public byte[] GetBytesSafe()
    {
        lock (_lock) { return _outBytes; }
    }

    public void SetBytesSafe(byte[] bytes)
    {
        lock (_lock) { _outBytes = bytes; }
    }
}