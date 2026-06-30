using System;
using System.Diagnostics;
using System.Threading;
using RobocadCs.Internal;

namespace RobocadCs.Internal.Common;

public abstract class TitanCom
{
    protected Thread _thread;
    protected volatile bool _stop;
    protected ConnectionReal _conn;
    protected Robot _robot;
    protected RobotInternal _ri;
    protected RobotConfiguration _conf;

    public void Start(ConnectionReal conn, Robot robot, RobotInternal ri, RobotConfiguration conf)
    {
        _conn = conn;
        _robot = robot;
        _ri = ri;
        _conf = conf;
        _stop = false;
        _thread = new Thread(Loop) { IsBackground = true };
        _thread.Start();
    }

    public void Stop()
    {
        _stop = true;
        try
        {
            _thread?.Join(1000);
        }
        catch
        {
        }
    }

    private void Loop()
    {
        try
        {
            if (_conn.ComIni(_conf.TitanPort, _conf.TitanBaud) != 0)
            {
                _robot.WriteLog("Failed to open COM");
                return;
            }

            long startTime = TimeUnits.Now();
            var countSw = Stopwatch.StartNew();
            int commCounter = 0;
            while (!_stop)
            {
                long tx = TimeUnits.Now();
                byte[] txData = BuildTx();
                _robot.RobotInfo.TxComTimeDev = TimeUnits.Now() - tx;

                byte[] rxData = _conn.ComRw(txData);

                long rx = TimeUnits.Now();
                ParseRx(rxData);
                _robot.RobotInfo.RxComTimeDev = TimeUnits.Now() - rx;

                commCounter++;
                if (countSw.Elapsed.TotalSeconds >= 1)
                {
                    countSw.Restart();
                    _robot.RobotInfo.ComCountDev = commCounter;
                    commCounter = 0;
                }

                Thread.Sleep(2);
                _robot.RobotInfo.ComTimeDev = TimeUnits.Now() - startTime;
                startTime = TimeUnits.Now();
            }
        }
        catch (Exception e)
        {
            _conn.ComStop();
            _robot.WriteLog("Exception in TitanCOM: " + e.Message);
        }
    }

    protected abstract void ParseRx(byte[] data);

    protected abstract byte[] BuildTx();
}
