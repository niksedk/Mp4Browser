﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mp4Browser.Mp4.Boxes
{
    public class Vttc : Box
    {

        public List<string> Payload { get; set; }

        public Vttc(FileStream fs, ulong maximumLength)
        {
            Payload = new List<string>();
            long max = (long)maximumLength;
            int count = 0;
            while (fs.Position < max)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "payl")
                {
                    var length = (int)(max - fs.Position);
                    if (length > 0 && length < 5000)
                    {
                        var buffer = new byte[length];
                        fs.Read(buffer, 0, length);
                        var s = Encoding.UTF8.GetString(buffer);
                        s = string.Join(Environment.NewLine, s.Replace("\r\n", "\n").Split('\n'));
                        Payload.Add(s.Trim());
                        count++;
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
