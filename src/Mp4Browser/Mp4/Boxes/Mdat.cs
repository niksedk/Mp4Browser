using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Mdat : Box
    {
        public List<string> Payloads { get; set; }

        public Mdat(FileStream fs, ulong maximumLength, TreeNode root)
        {
            Payloads = new List<string>();
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "vtte")
                {
                    root?.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    });
                }                
                else if (Name == "vttc")
                {
                    root?.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    });
                    var vttc = new Vttc(fs, Position);
                    if (vttc.Payload != null)
                    {
                        Payloads.AddRange(vttc.Payload);
                    }
                }
                else if (Name == "payl")
                {
                    root?.Nodes.Add(new TreeNode(Name)
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
