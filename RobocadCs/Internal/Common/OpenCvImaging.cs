using OpenCvSharp;

namespace RobocadCs.Internal.Common
{
    internal static class OpenCvImaging
    {
        public static byte[] EncodeJpeg(Mat frame)
        {
            if (frame == null || frame.Empty()) return null;
            Cv2.ImEncode(".jpg", frame, out byte[] buf);
            return buf;
        }
    }
}
