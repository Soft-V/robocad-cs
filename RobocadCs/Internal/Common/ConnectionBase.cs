using System;
using OpenCvSharp;

namespace RobocadCs.Internal.Common
{
    public abstract class ConnectionBase
    {
        public virtual void Stop() { }
        public virtual Mat GetCamera() => null;
        public virtual float[] GetLidar() => Array.Empty<float>();
    }
}
