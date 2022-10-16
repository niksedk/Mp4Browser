using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using static Mp4Browser.Mp4.Boxes.Sidx;

namespace Mp4Browser.Mp4.Boxes
{
    public class Sidx : Box
    {
        public readonly uint Version;
        public readonly uint ReferenceId;
        public readonly uint TimeScale;
        public readonly ulong EarliestPresentationTime;
        public readonly ulong FirstOffset;
        public readonly uint ReferenceCount;
        public List<ReferenceItem> ReferenceItems { get; set; }

        public class ReferenceItem
        {
            public int ReferenceType { get; set; }
            public uint ReferencedSize { get; set; }
            public uint SubSegmentDuration { get; set; }
            public int StartsWithSap { get; set; }
            public int SapType { get; set; }
            public uint SapDeltaTime { get; set; }
        }

        public Sidx(FileStream fs)
        {
            ReferenceItems = new List<ReferenceItem>();
            Buffer = new byte[12];
            var bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
            {
                return;
            }

            Version = Buffer[0];
            ReferenceId = GetUInt(4);
            TimeScale = GetUInt(8);

            if (Version == 0)
            {
                Buffer = new byte[8];
                bytesRead = fs.Read(Buffer, 0, Buffer.Length);
                if (bytesRead < Buffer.Length)
                {
                    return;
                }

                EarliestPresentationTime = GetUInt(0);
                FirstOffset = GetUInt(4);
            }
            else
            {
                Buffer = new byte[16];
                bytesRead = fs.Read(Buffer, 0, Buffer.Length);
                if (bytesRead < Buffer.Length)
                {
                    return;
                }

                EarliestPresentationTime = GetUInt64(0);
                FirstOffset = GetUInt64(8);
            }

            Buffer = new byte[4];
            bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
            {
                return;
            }

            ReferenceCount = (uint) GetWord(2);
            if (ReferenceCount == 0)
            {
                return;
            }

            const int referenceItemSize = 12;
            Buffer = new byte[ReferenceCount * referenceItemSize];
            bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
            {
                return;
            }

            for (var i = 0; i < ReferenceCount; i++)
            {
                var idx = i * referenceItemSize;
                ReferenceItems.Add(new ReferenceItem
                {
                    ReferenceType = (Buffer[idx] & 0b10000000) >> 7,
                    ReferencedSize = (uint)(((Buffer[idx] & 0b10000000) << 24) + (Buffer[idx+1] << 16) + (Buffer[idx + 2] << 8) + Buffer[idx + 3]),

                    SubSegmentDuration = GetUInt(idx + 4),

                    StartsWithSap = (Buffer[idx + 8] & 0b10000000) >> 7,
                    SapType = (Buffer[idx + 8] & 0b01110000) >> 4,
                    SapDeltaTime = (uint)(((Buffer[idx + 8] & 0b00001111) << 24) + (Buffer[idx + 9] << 16) + (Buffer[idx + 10] << 8) + Buffer[idx + 11]),
                });
            }
        }
    }
}
