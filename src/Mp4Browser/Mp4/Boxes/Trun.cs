using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/ff469478.aspx
    /// </summary>
    public class Trun : Box
    {
        public List<TimeSegment> Samples { get; set; }
        public uint? DataOffset { get; set; }

        public Trun(Stream fs, ulong maximumLength, TreeNode rootNode)
        {
            Samples = new List<TimeSegment>();
            if (maximumLength <= 4)
            {
                return;
            }

            Buffer = new byte[maximumLength - 4];
            var readCount = fs.Read(Buffer, 0, Buffer.Length);
            //if (readCount < (int)maximumLength)
            //    return;

            var versionAndFlags = GetUInt(0);
            var version = versionAndFlags >> 24;
            var flags = versionAndFlags & 0xFFFFFF;

            var sampleCount = GetUInt(4);
            var pos = 8;

            if ((flags & 0x000001) > 0)
            {
                pos += 4;
                DataOffset = GetUInt(8);
            }

            // skip "first_sample_flags" if present
            if ((flags & 0x000004) > 0)
            {
                pos += 4;
            }

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var sample = new TimeSegment();
                if (pos > Buffer.Length - 4)
                    return;

                // read "sample duration" if present
                if ((flags & 0x000100) > 0)
                {
                    sample.Duration = GetUInt(pos);
                    pos += 4;
                }
                if (pos > Buffer.Length - 4)
                    return;

                // read "sample_size" if present
                if ((flags & 0x000200) > 0)
                {
                    sample.Size = GetUInt(pos);
                    pos += 4;
                }

                if (pos > Buffer.Length - 4)
                {
                    return;
                }

                // skip "sample_flags" if present
                if ((flags & 0x000400) > 0)
                {
                    pos += 4;
                }
                if (pos > Buffer.Length - 4)
                    return;

                // read "sample_time_offset" if present
                if ((flags & 0x000800) > 0)
                {
                    sample.TimeOffset = version == 0 ? (long)GetUInt(pos) : GetInt(pos); // version==1 equals signed
                    pos += 4;
                }
                Samples.Add(sample);
            }

            var sampleNodes = new TreeNode("Samples (Count=" + Samples.Count + ")")
            {
                Tag = "Samples (Count=" + Samples.Count + ")",
            };
            rootNode.Nodes.Add(sampleNodes);

            for (var i = 0; i < Samples.Count; i++)
            {
                var sample = Samples[i];
                sampleNodes.Nodes.Add("Time: " + sample.TimeOffset + ", size: " + sample.Size + ", dur: " + sample.Duration);
            }
        }
    }
}
