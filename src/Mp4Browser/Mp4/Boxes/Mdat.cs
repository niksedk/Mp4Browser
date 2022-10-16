using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    root?.Nodes.Add(new TreeNode(Name + " - (Size=" + Size + ")")
                    {
                        Tag = "Element: " + Name + " - " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    });
                }
                else if (Name == "vttc")
                {
                    var vttcNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };

                    root?.Nodes.Add(vttcNode);
                    var vttc = new Vttc(fs, Position, vttcNode);
                    if (vttc.Payload != null)
                    {
                        vttcNode.Text += $" - (Size={Size})";
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
                else 
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
