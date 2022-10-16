using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Traf : Box
    {

        public Trun Trun { get; set; }
        public Tfdt Tfdt { get; set; }

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
                    var buf = new byte[Size -8];
                    fs.Read(buf, 0, buf.Length);
                    fs.Position = oldP;
                    data = Environment.NewLine + "Data: " + ByteArrayToString(buf);
                }

                if (Name == "trun")
                {
                    var trunNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Track Fragment Run" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                              + data
                    };
                    root.Nodes.Add(trunNode);
                    Trun = new Trun(fs, Position, trunNode);
                }
                else if (Name == "tfdt")
                {
                    Tfdt = new Tfdt(fs, Size);
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                              + data
                    });
                }
                else
                {
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                          + data
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
