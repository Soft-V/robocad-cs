namespace RobocadCs.Internal.Common
{
    public sealed class CameraFrame
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Bgr { get; } 

        public CameraFrame(int width, int height, byte[] bgr)
        {
            Width = width;
            Height = height;
            Bgr = bgr;
        }
    }
}
