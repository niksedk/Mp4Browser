using System;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Stsd : Box
    {
        public uint NumberOfEntries { get; set; }

        public Stsd(Stream fs, ulong maximumLength, TreeNode root)
        {
            Position = (ulong)fs.Position;

            Buffer = new byte[8];
            fs.Read(Buffer, 0, Buffer.Length);
            int version = Buffer[0];
            NumberOfEntries = GetUInt(4);

            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                {
                    return;
                }

                if (Name == "tx3g")
                {
                    Buffer = new byte[Size-8];
                    fs.Read(Buffer, 0, Buffer.Length);
                    var dataReferenceIndex = GetWord(6);
                    if (Buffer[0] == 0)
                    {
                    }

                    var tx3GNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Data reference index: " + dataReferenceIndex
                    };
                    root.Nodes.Add(tx3GNode);
                }
                else //if (Name == "text")
                {
                    var textNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine 
                    };
                    root.Nodes.Add(textNode);
                }


                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
