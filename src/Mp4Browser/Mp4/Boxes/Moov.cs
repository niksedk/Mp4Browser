using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Moov : Box
    {
        public Mvhd Mvhd;
        public List<Trak> Tracks;

        public Moov(FileStream fs, ulong maximumLength, TreeNode root)
        {
            Tracks = new List<Trak>();
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "trak")
                {
                    var trakNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Track Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root?.Nodes.Add(trakNode);
                    var trak = new Trak(fs, Position, trakNode);
                    if (!string.IsNullOrEmpty(trak.Mdia?.HandlerName))
                    {
                        trakNode.Text = trakNode.Text + " (" + trak.Mdia?.HandlerName + ")";
                    }
                    Tracks.Add(trak);
                }
                else if (Name == "mvhd")
                {
                    Mvhd = new Mvhd(fs);
                    root?.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Movie Header Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Timescale: " + Mvhd.TimeScale
                    });
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
