namespace RobocadCs.Internal.Common
{
    public enum LidarTypes
    {
        N10Lidar = 0,
        YdLidarX2 = 1
    }

    public abstract class LidarBase
    {
        public abstract void Start();
        public abstract float[] GetData();
        public abstract void Stop();
    }
}
