using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Vttc : Box
    {

        public List<string> Payload { get; set; }
        public int PayloadSize { get; set; }

        public Vttc(FileStream fs, ulong maximumLength, TreeNode root)
        {
            Payload = new List<string>();
            long max = (long)maximumLength;
            int count = 0;
            while (fs.Position < max)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                //if (Name == "payl")
                {
                    var length = (int)Size - 8;
                    if (length > 0 && length < 5000)
                    {
                        var buffer = new byte[length];
                        fs.Read(buffer, 0, length);
                        var s = Encoding.UTF8.GetString(buffer);
                        s = string.Join(Environment.NewLine, s.Replace("\r\n", "\n").Split('\n'));
                        Payload.Add(s.Trim());
                        count++;

                        PayloadSize += (int)Size;

                        var payloadNode = new TreeNode(Name)
                        {
                            Tag = "Element: " + Name + " - " + Environment.NewLine +
                                                    "Size: " + Size + Environment.NewLine +
                                                    "Position: " + StartPosition + Environment.NewLine +
                                                    "Payload: " + s.Trim()
                        };

                        root?.Nodes.Add(payloadNode);
                    }
                    else
                    {
                        Payload.Add(string.Empty);
                    }
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }

            if (count == 0)
            {
                Payload.Add(null);
            }
        }
    }
}
