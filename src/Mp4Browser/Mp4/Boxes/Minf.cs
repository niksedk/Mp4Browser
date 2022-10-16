using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Minf : Box
    {

        public Stbl Stbl;

        public Minf(Stream fs, ulong maximumLength, ulong timeScale, string handlerType, Mdia mdia, TreeNode root)
        {
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "stbl")
                {
                    var stblNode = new TreeNode(Name);
                    Stbl = new Stbl(fs, Position, timeScale, handlerType, mdia, stblNode)
                    {
                        Text = "Element: " + Name + " - Sample Table" + Environment.NewLine +
                               "Size: " + Size + Environment.NewLine +
                               "Position: " + StartPosition
                    };
                    stblNode.Tag = Stbl;
                    root.Nodes.Add(stblNode);
                    
                }
                else if (Name == "sthd")
                {
                    var stblNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Subtitle Media Header" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root.Nodes.Add(stblNode);
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }

    }
}
