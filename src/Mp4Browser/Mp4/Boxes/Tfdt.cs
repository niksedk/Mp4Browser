using System;
using System.IO;

namespace Mp4Browser.Mp4.Boxes
{
    public class Tfdt : Box
    {
        public ulong BaseMediaDecodeTime { get; set; }

        public Tfdt(Stream fs, ulong size)
        {
            var bufferSize = size - 8;
            if (bufferSize <= 0)
                return;

            Buffer = new byte[bufferSize];
            int bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
                return;

            var version = Buffer[0];
            //var flags = GetUInt(0) & 0xffffff;

            if (version == 1)
            {
                BaseMediaDecodeTime = GetUInt(8);
            }
            else
            {
                BaseMediaDecodeTime = GetUInt64(8);
            }

            if (BaseMediaDecodeTime > 0)
            {
                Console.WriteLine("Baseline found: " + BaseMediaDecodeTime);
            }
        }
    }
}
