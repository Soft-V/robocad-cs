using System.Runtime.InteropServices;
using OpenCvSharp;

namespace RobocadCs.Internal.Common
{
    internal static class OpenCvImaging
    {
        public static CameraFrame ToFrame(Mat mat)
        {
            if (mat == null || mat.Empty()) return null;
            Mat src = mat.IsContinuous() ? mat : mat.Clone();
            try
            {
                int len = (int)(src.Total() * src.ElemSize());
                byte[] buf = new byte[len];
                Marshal.Copy(src.Data, buf, 0, len);
                return new CameraFrame(src.Width, src.Height, buf);
            }
            finally
            {
                if (!ReferenceEquals(src, mat)) src.Dispose();
            }
        }

        public static byte[] EncodeJpeg(CameraFrame frame)
        {
            if (frame == null) return null;
            using (var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3, frame.Bgr))
            {
                Cv2.ImEncode(".jpg", mat, out byte[] buf);
                return buf;
            }
        }
    }
}
