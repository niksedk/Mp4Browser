using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Trak : Box
    {

        public Mdia Mdia;
        public Tkhd Tkhd;

        public Trak(Stream fs, ulong maximumLength, TreeNode root)
        {
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "mdia")
                {
                    var mdiaNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    Mdia = new Mdia(fs, Position, mdiaNode);
                    root.Nodes.Add(mdiaNode);
                }
                else if (Name == "tkhd")
                {
                    Tkhd = new Tkhd(fs);
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    });
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }

    }
}
