using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mp4Browser.Mp4.Boxes
{
    public class Stbl : Box
    {
        public class SampleToChunkMap
        {
            public uint FirstChunk { get; set; }
            public uint SamplesPerChunk { get; set; }
            public uint SampleDescriptionIndex { get; set; }
        }

        public class TimeInfo
        {
            public uint SampleCount { get; set; }
            public uint SampleDelta { get; set; }
        }

        public List<double> StartTimeCodes = new List<double>();
        public List<double> EndTimeCodes = new List<double>();
        public List<uint> SampleSizes = new List<uint>();
        public ulong StszSampleCount = 0;
        private readonly Mdia _mdia;
        public ulong TimeScale { get; set; }
        public string HandlerType { get; set; }
        public List<TimeInfo> Ssts { get; set; }
        public List<SampleToChunkMap> Stsc { get; set; }
        public List<ChunkText> Texts { get; set; }
        public List<byte[]> TextBuffers { get; set; }

        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }

        public Stbl(Stream fs, ulong maximumLength, ulong timeScale, string handlerType, Mdia mdia, TreeNode root)
        {
            TimeScale = timeScale;
            HandlerType = handlerType;
            Ssts = new List<TimeInfo>();
            Stsc = new List<SampleToChunkMap>();
            Texts = new List<ChunkText>();
            TextBuffers = new List<byte[]>();
            _mdia = mdia;
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "stco") // 32-bit - chunk offset
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var totalEntries = GetUInt(4);
                    uint lastOffset = 0;
                    var sbTexts = new StringBuilder();
                    for (var i = 0; i < totalEntries; i++)
                    {
                        var offset = GetUInt(8 + i * 4);
                        if (lastOffset + 5 < offset)
                        {
                            var text = ReadText(fs, offset, handlerType, i);
                            Texts.Add(text);
                            sbTexts.AppendLine($" {i,4} - {text.Size,2} {offset,9} - {text.Text?.Replace(Environment.NewLine, "</br>")}");
                        }
                        else
                        {
                            Texts.Add(new ChunkText { Size = 2 });
                            sbTexts.AppendLine($" {i,4} -  2 - {offset,9} - ?");
                        }

                        lastOffset = offset;
                    }

                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Chunk Offset" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Total entries: " + totalEntries + Environment.NewLine +
                              "Texts: " + Environment.NewLine +
                              "[   # - Size - Offset - Text]" + Environment.NewLine +
                              sbTexts
                    });
                }
                else if (Name == "co64") // 64-bit
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var totalEntries = GetUInt(4);
                    var sbTexts = new StringBuilder();

                    ulong lastOffset = 0;
                    for (var i = 0; i < totalEntries; i++)
                    {
                        var offset = GetUInt64(8 + i * 8);
                        if (lastOffset + 8 < offset)
                        {
                            var s = ReadText(fs, offset, handlerType, i);
                            sbTexts.AppendLine($" {i,4} - {s.Size,2} - {offset,9} - {s.Text?.Replace(Environment.NewLine, "</br>")}");
                            Texts.Add(new ChunkText { Size = s.Size, Text = s.Text });
                        }
                        else
                        {
                            sbTexts.AppendLine($" {i,4} -  2 - {offset,9} - ?");
                            Texts.Add(new ChunkText { Size = 2 });
                        }

                        lastOffset = offset;
                    }
                    var co64Node = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Chunk Offset Box " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Total entries: " + totalEntries + Environment.NewLine +
                              "Texts: " + Environment.NewLine +
                              "[# - Size - Offset - Text]" + Environment.NewLine +
                              sbTexts
                    };
                    root.Nodes.Add(co64Node);
                }
                else if (Name == "stsz") // sample sizes
                {
                    var sbSampleSizes = new StringBuilder();
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var uniformSizeOfEachSample = GetUInt(4);
                    var numberOfSampleSizes = GetUInt(8);
                    StszSampleCount = numberOfSampleSizes;
                    for (var i = 0; i < numberOfSampleSizes; i++)
                    {
                        if (12 + i * 4 + 4 < Buffer.Length)
                        {
                            var sampleSize = GetUInt(12 + i * 4);
                            SampleSizes.Add(sampleSize);
                            sbSampleSizes.AppendLine($" {i,4} - {sampleSize,4}");
                        }
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample Size Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Uniform size of each sample: " + uniformSizeOfEachSample + Environment.NewLine +
                              "Number of samples: " + numberOfSampleSizes + Environment.NewLine +
                              "Sample sizes:" + Environment.NewLine +
                              "[   # - SampleSize]" + Environment.NewLine +
                              sbSampleSizes
                    });
                }
                else if (Name == "stts") // sample table time to sample map
                {
                    //https://developer.apple.com/library/mac/#documentation/QuickTime/QTFF/QTFFChap2/qtff2.html#//apple_ref/doc/uid/TP40000939-CH204-SW1

                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var numberOfSampleTimes = GetUInt(4);
                    double totalTime = 0;
                    var sbTimeToSample = new StringBuilder();
                    if (_mdia.IsClosedCaption)
                    {
                        for (var i = 0; i < numberOfSampleTimes; i++)
                        {
                            var sampleCount = GetUInt(8 + i * 8);
                            var sampleDelta = GetUInt(12 + i * 8);
                            Ssts.Add(new TimeInfo { SampleCount = sampleCount, SampleDelta = sampleDelta });
                            sbTimeToSample.AppendLine($" {i} - {sampleCount} - {sampleDelta}");
                            for (var j = 0; j < sampleCount; j++)
                            {
                                totalTime += sampleDelta / (double)timeScale;
                                if (StartTimeCodes.Count > 0)
                                {
                                    EndTimeCodes[EndTimeCodes.Count - 1] = totalTime - 0.001;
                                }

                                StartTimeCodes.Add(totalTime);
                                EndTimeCodes.Add(totalTime + 2.5);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < numberOfSampleTimes; i++)
                        {
                            var sampleCount = GetUInt(8 + i * 8);
                            var sampleDelta = GetUInt(12 + i * 8);
                            Ssts.Add(new TimeInfo { SampleCount = sampleCount, SampleDelta = sampleDelta });
                            sbTimeToSample.AppendLine($" {i,4} - {sampleCount,4} - {sampleDelta}");
                            totalTime += sampleDelta / (double)timeScale;
                            if (StartTimeCodes.Count <= EndTimeCodes.Count)
                            {
                                StartTimeCodes.Add(totalTime);
                            }
                            else
                            {
                                EndTimeCodes.Add(totalTime);
                            }
                        }
                    }
                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Time to Sample Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Number of samples: " + numberOfSampleTimes + Environment.NewLine +
                              "Number of samples unpacked: " + Ssts.Sum(p => p.SampleCount) + Environment.NewLine +
                              "Samples: " + Environment.NewLine +
                              "[   # - SampleCount - SampleDelta]" + Environment.NewLine +
                                 sbTimeToSample
                    });
                }
                else if (Name == "stsc") // sample table sample to chunk map
                {
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var numberOfSampleTimes = GetUInt(4);
                    var stscSb = new StringBuilder();
                    for (var i = 0; i < numberOfSampleTimes; i++)
                    {
                        if (16 + i * 12 + 4 < Buffer.Length)
                        {
                            var map = new SampleToChunkMap
                            {
                                FirstChunk = GetUInt(8 + i * 12),
                                SamplesPerChunk = GetUInt(12 + i * 12),
                                SampleDescriptionIndex = GetUInt(16 + i * 12),
                            };
                            Stsc.Add(map);
                            stscSb.AppendLine($"{map.FirstChunk,4} - {map.SamplesPerChunk,4} - {map.SampleDescriptionIndex,4}");
                        }
                    }

                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample‐to‐Chunk Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Number of samples: " + numberOfSampleTimes + Environment.NewLine +
                              "[FirstChunk - SamplesPerChunk - SampleDescriptionIndex]" + Environment.NewLine +
                              stscSb,
                    });
                }
                else if (Name == "stsd")
                {
                    var stsdNode = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Sample Description Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine
                    };
                    var stsd = new Stsd(fs, Position, stsdNode);
                    stsdNode.Tag += "Number of entries: " + stsd.NumberOfEntries;
                    root.Nodes.Add(stsdNode);
                }
                else
                {
                    var unknown = new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - unkown " + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition
                    };
                    root.Nodes.Add(unknown);
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }

        private ChunkText ReadText(Stream fs, ulong offset, string handlerType, int index)
        {
            if (handlerType == "vide" || handlerType == "soun")
            {
                return new ChunkText { Size = 2 };
            }

            fs.Seek((long)offset, SeekOrigin.Begin);
            var data = new byte[4];
            fs.Read(data, 0, 2);
            var textSize = (uint)GetWord(data, 0);

            if (handlerType == "subp") // VobSub created with Mp4Box
            {
                if (textSize > 100)
                {
                    fs.Seek((long)offset, SeekOrigin.Begin);
                    data = new byte[textSize + 2];
                    fs.Read(data, 0, data.Length);
                    //SubPictures.Add(new SubPicture(data)); // TODO: Where is palette?
                    return new ChunkText { Size = 2 };
                }
            }
            else
            {
                if (handlerType == "text" && index + 1 < SampleSizes.Count && SampleSizes[index + 1] <= 2)
                {
                    return new ChunkText { Size = 2 };
                }

                if (textSize == 0)
                {
                    fs.Read(data, 2, 2);
                    textSize = GetUInt(data, 0); // don't get it exactly - seems like mp4box sometimes uses 2 bytes length field (first text record only)... handbrake uses 4 bytes
                }

                if (textSize > 0 && textSize < 500)
                {
                    data = new byte[textSize];
                    fs.Read(data, 0, data.Length);
                    var text = GetString(data, 0, (int)textSize).TrimEnd();

                    if (_mdia.IsClosedCaption)
                    {
                        var sb = new StringBuilder();
                        for (int j = 8; j < data.Length - 3; j++)
                        {
                            string h = data[j].ToString("X2").ToLowerInvariant();
                            if (h.Length < 2)
                            {
                                h = "0" + h;
                            }

                            sb.Append(h);
                            if (j % 2 == 1)
                            {
                                sb.Append(' ');
                            }
                        }

                        var hex = sb.ToString();
                        var errorCount = 0;
                        text = string.Empty; //  ScenaristClosedCaptions.GetSccText(hex, ref errorCount);
                        if (text.StartsWith("n") && text.Length > 1)
                        {
                            text = "<i>" + text.Substring(1) + "</i>";
                        }

                        if (text.StartsWith("-n", StringComparison.Ordinal))
                        {
                            text = text.Remove(0, 2);
                        }

                        if (text.StartsWith("-N", StringComparison.Ordinal))
                        {
                            text = text.Remove(0, 2);
                        }

                        if (text.StartsWith("-") && !text.Contains(Environment.NewLine + "-"))
                        {
                            text = text.Remove(0, 1);
                        }
                    }

                    text = text.Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine);
                    return new ChunkText { Size = textSize, Text = text };
                }
            }

            return new ChunkText { Size = 2 };
        }
    }
}
