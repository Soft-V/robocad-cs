using RobocadCs.Internal.Common;

namespace RobocadCs.Internal;

public abstract class RobotInternal
{
    public float SpeedMotor0 { get; set; }
    public float SpeedMotor1 { get; set; }
    public float SpeedMotor2 { get; set; }
    public float SpeedMotor3 { get; set; }
    public int EncMotor0 { get; set; }
    public int EncMotor1 { get; set; }
    public int EncMotor2 { get; set; }
    public int EncMotor3 { get; set; }
    public bool LimitL0 { get; set; }
    public bool LimitH0 { get; set; }
    public bool LimitL1 { get; set; }
    public bool LimitH1 { get; set; }
    public bool LimitL2 { get; set; }
    public bool LimitH2 { get; set; }
    public bool LimitL3 { get; set; }
    public bool LimitH3 { get; set; }
    public float AdditionalServo1 { get; set; }
    public float AdditionalServo2 { get; set; }

    public bool IsStep1Busy { get; set; }
    public bool IsStep2Busy { get; set; }

    public int StepMotor1Steps { get; set; }
    public int StepMotor2Steps { get; set; }

    public int StepMotor1StepsPerS { get; set; }
    public int StepMotor2StepsPerS { get; set; }

    public bool StepMotor1Direction { get; set; }
    public bool StepMotor2Direction { get; set; }

    public bool UsePid { get; set; }

    public float PPid { get; set; } = 0.14f;
    public float IPid { get; set; } = 0.1f;
    public float DPid { get; set; } = 0.0f;

    public float Yaw { get; set; }
    public float YawUnlim { get; set; }
    public float Pitch { get; set; }
    public float PitchUnlim { get; set; }
    public float Roll { get; set; }
    public float RollUnlim { get; set; }
    public float Ultrasound1 { get; set; }
    public float Ultrasound2 { get; set; }
    public float Ultrasound3 { get; set; }
    public float Ultrasound4 { get; set; }

    public int Analog1 { get; set; }
    public int Analog2 { get; set; }
    public int Analog3 { get; set; }
    public int Analog4 { get; set; }
    public int Analog5 { get; set; }
    public int Analog6 { get; set; }
    public int Analog7 { get; set; }
    public int Analog8 { get; set; }

    public readonly bool[] Inputs = new bool[4];
    public readonly bool[] Outputs = new bool[4];
    public readonly float[] ServoAngles = new float[8];

    protected readonly Robot _robot;
    private readonly ConnectionBase _connection;
    private readonly RobocadConnection _robocad;
    private readonly TitanCom _titan;
    private readonly VmxSpi _vmx;
    private readonly Updater _updater;

    public RobotInternal(Robot robot, RobotConfiguration conf)
    {
        _robot = robot;
        if (robot.OnRealRobot)
        {
            _updater = CreateUpdater(robot);
            var real = new ConnectionReal(robot, _updater, conf);
            _connection = real;
            _titan = CreateTitanCom();
            _titan.Start(real, robot, this, conf);
            _vmx = CreateVmxSpi();
            _vmx.Start(real, robot, this, conf);
        }
        else
        {
            var sim = new ConnectionSim(robot);
            _connection = sim;
            _robocad = CreateRobocadConnection();
            _robocad.Start(sim, robot, this);
        }
    }
    
    public void Stop()
    {
        _titan?.Stop();
        _vmx?.Stop();
        _robocad?.Stop();
        _connection?.Stop();
    }
    
    protected Updater CreateUpdater(Robot robot) => new Updater(robot);
    
    public CameraFrame GetCamera() => _connection.GetCamera();
    public float[] GetLidar() => _connection.GetLidar();
    protected abstract TitanCom CreateTitanCom();
    protected abstract VmxSpi CreateVmxSpi();
    protected abstract RobocadConnection CreateRobocadConnection();
}