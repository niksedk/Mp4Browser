using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Moof : Box
    {
        public Traf Traf { get; set; }

        public Moof(Stream fs, ulong maximumLength, TreeNode root, ulong startPosition)
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


                if (Name == "traf")
                {
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Track Fragment" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition +
                              data
                    });
                    Traf = new Traf(fs, Position, root.Nodes[root.Nodes.Count-1]);
                }
                else
                {
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name +  Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + 
                              data
                    });                 
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }

            StartPosition = startPosition;
        }
    }
}