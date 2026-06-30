using System;

namespace RobocadCs.Internal.Common
{
    public abstract class ConnectionBase
    {
        public virtual void Stop() { }
        public virtual CameraFrame GetCamera() => null;
        public virtual float[] GetLidar() => Array.Empty<float>();
    }
}
