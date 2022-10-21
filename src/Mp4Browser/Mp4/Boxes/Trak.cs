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
                {
                    return;
                }

                if (Name == "mdia")
                {
                    var mdiaNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Media Box" + Environment.NewLine +
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
                        Tag = "Element: " + Name + " - Track Header Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "TrackId: " + Tkhd.TrackId + Environment.NewLine +
                              "Duration: " + Tkhd.Duration + Environment.NewLine +
                              "Width: " + Tkhd.Width + Environment.NewLine +
                              "Height: " + Tkhd.Height + Environment.NewLine +
                              "RotationMatrix: "  + Tkhd.RotationMatrix[0] + " "
                                                  + Tkhd.RotationMatrix[1] + " "
                                                  + Tkhd.RotationMatrix[2] + " "
                                                  + Tkhd.RotationMatrix[3] + " "
                                                  + Tkhd.RotationMatrix[4] + " "
                                                  + Tkhd.RotationMatrix[5] + " "
                                                  + Tkhd.RotationMatrix[6] + " "
                                                  + Tkhd.RotationMatrix[7] + " "
                                                  + Tkhd.RotationMatrix[8] 
                    });
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
