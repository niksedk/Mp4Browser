using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Traf : Box
    {

        public Trun Trun { get; set; }
        public Tfdt Tfdt { get; set; }
        public Tfhd Tfhd { get; set; }

        public Traf(Stream fs, ulong maximumLength, TreeNode root)
        {
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                var data = string.Empty;
                if (Size < 100 && Size > 8)
                {
                    var oldP = fs.Position;
                    var buf = new byte[Size - 8];
                    fs.Read(buf, 0, buf.Length);
                    fs.Position = oldP;
                    data = Environment.NewLine + "Data: " + ByteArrayToString(buf);
                }

                if (Name == "trun")
                {
                    var trunNode = new TreeNode(Name);
                    root.Nodes.Add(trunNode);
                    Trun = new Trun(fs, Position, trunNode);
                    trunNode.Tag = "Element: " + Name + " - Track Fragment Run" + Environment.NewLine +
                                   "Size: " + Size + Environment.NewLine +
                                   "DataOffset: " + Trun.DataOffset + Environment.NewLine +
                                   "Position: " + StartPosition
                                   + data;

                }
                else if (Name == "tfdt")
                {
                    Tfdt = new Tfdt(fs, Size);
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Track Fragment Decode Time" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "BaseMediaDecodeTime: " + Tfdt.BaseMediaDecodeTime + Environment.NewLine +
                              data
                    });
                }
                else if (Name == "tfhd")
                {
                    Tfhd = new Tfhd(fs, Size);
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Fragment Header" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "TrackId: " + Tfhd.TrackId + Environment.NewLine +
                              "BaseDataOffset: " + Tfhd.BaseDataOffset + Environment.NewLine +
                              "SampleDescriptionIndex: " + Tfhd.SampleDescriptionIndex + Environment.NewLine +
                              "DefaultSampleDuration: " + Tfhd.DefaultSampleDuration + Environment.NewLine +
                              "DefaultSampleSize: " + Tfhd.DefaultSampleSize + Environment.NewLine +
                              "DefaultSampleFlags: " + Tfhd.DefaultSampleFlags + Environment.NewLine +
                              data
                    });
                }
                else
                {
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              data
                    });
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
            if (Trun?.Samples != null && Tfdt != null)
            {
                foreach (var timeSegment in Trun.Samples)
                {
                    timeSegment.BaseMediaDecodeTime = Tfdt.BaseMediaDecodeTime;
                }
            }
        }

    }
}
