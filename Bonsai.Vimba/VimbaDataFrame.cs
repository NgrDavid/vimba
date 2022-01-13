using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Vimba
{
    public class VimbaDataFrame
    {
        public VimbaDataFrame(IplImage image, ulong frameID, ulong timestamp)
        {
            Image = image;
            FrameID = frameID;
            Timestamp = timestamp;
        }

        public IplImage Image { get; private set; }

        public ulong FrameID { get; private set; }

        public ulong Timestamp { get; private set; }
    }
}
