using System;
using System.Diagnostics;
using System.Threading;
using RobocadCs.Internal;

namespace RobocadCs.Internal.Common;

public abstract class VmxSpi
{
    protected Thread _thread;
    protected volatile bool _stop;
    protected ConnectionReal _conn;
    protected Robot _robot;
    protected RobotInternal _ri;
    protected RobotConfiguration _conf;
    protected int _toggler;

    public void Start(ConnectionReal conn, Robot robot, RobotInternal ri, RobotConfiguration conf)
    {
        _conn = conn;
        _robot = robot;
        _ri = ri;
        _conf = conf;
        _toggler = 0;
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
            if (_conn.SpiIni(_conf.VmxPort, _conf.VmxCh, _conf.VmxSpeed, _conf.VmxMode) != 0)
            {
                _robot.WriteLog("Failed to open SPI");
                return;
            }

            long startTime = TimeUnits.Now();
            var countSw = Stopwatch.StartNew();
            int commCounter = 0;
            while (!_stop)
            {
                long tx = TimeUnits.Now();
                byte[] txList = BuildTx();
                _robot.RobotInfo.TxSpiTimeDev = TimeUnits.Now() - tx;

                byte[] rxList = _conn.SpiRw(txList);

                long rx = TimeUnits.Now();
                ParseRx(rxList);
                _robot.RobotInfo.RxSpiTimeDev = TimeUnits.Now() - rx;

                commCounter++;
                if (countSw.Elapsed.TotalSeconds >= 1)
                {
                    countSw.Restart();
                    _robot.RobotInfo.SpiCountDev = commCounter;
                    commCounter = 0;
                }

                Thread.Sleep(2);
                _robot.RobotInfo.SpiTimeDev = TimeUnits.Now() - startTime;
                startTime = TimeUnits.Now();
            }
        }
        catch (Exception e)
        {
            _conn.SpiStop();
            _robot.WriteLog("Exception in VMXSPI: " + e.Message);
        }
    }

    protected abstract void ParseRx(byte[] data);

    protected abstract byte[] BuildTx();
}
