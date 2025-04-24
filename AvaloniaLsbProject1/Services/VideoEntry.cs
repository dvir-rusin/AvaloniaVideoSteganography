using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaLsbProject1.Services
{
    public class VideoEntry
    {
        // the 32-byte password (Base64)
        public string Password { get; set; } = "";

        // the video file name
        public string VideoName { get; set; } = "";
    }
}
