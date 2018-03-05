using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Stsd : Box
    {
        public Stsd(Stream fs, ulong maximumLength, TreeNode root)
        {
            Position = (ulong)fs.Position;

            Buffer = new byte[8];
            fs.Read(Buffer, 0, Buffer.Length);
            int version = Buffer[0];
            uint numberOfSampleTimes = GetUInt(4);

            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "tx3g")
                {
                    var tx3GNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root.Nodes.Add(tx3GNode);
                }
                else if (Name == "text")
                {
                    var textNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root.Nodes.Add(textNode);
                }
                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
