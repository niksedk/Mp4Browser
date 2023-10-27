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

        public List<uint> SampleSizes = new List<uint>();
        public ulong StszSampleCount = 0;
        public ulong TimeScale { get; set; }
        public string HandlerType { get; set; }
        public List<uint> Ssts { get; set; }
        public List<SampleToChunkMap> Stsc { get; set; }
        public List<ChunkText> Texts { get; set; }
        public string TextsName { get; set; }
        public ulong TextsSize { get; set; }
        public ulong TextsPosition { get; set; }
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
            Ssts = new List<uint>();
            Stsc = new List<SampleToChunkMap>();
            Texts = new List<ChunkText>();
            TextBuffers = new List<byte[]>();
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                    return;

                if (Name == "stco") // 32-bit - chunk offset
                {
                    TextsName = Name;
                    TextsSize = Size;
                    TextsPosition = Position;
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var totalEntries = GetUInt(4);

                    for (var i = 0; i < totalEntries; i++)
                    {
                        var offset = GetUInt(8 + i * 4);
                        Texts.Add(new ChunkText { Offset = offset });
                    }
                }
                else if (Name == "co64") // 64-bit
                {
                    TextsName = Name;
                    TextsSize = Size;
                    TextsPosition = Position;
                    Buffer = new byte[Size - 4];
                    fs.Read(Buffer, 0, Buffer.Length);
                    int version = Buffer[0];
                    var totalEntries = GetUInt(4);

                    for (var i = 0; i < totalEntries; i++)
                    {
                        var offset = GetUInt64(8 + i * 8);
                        Texts.Add(new ChunkText { Offset = offset });
                    }
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
                    var count = 1;
                    for (var i = 0; i < numberOfSampleSizes; i++)
                    {
                        if (12 + i * 4 + 4 < Buffer.Length)
                        {
                            var sampleSize = GetUInt(12 + i * 4);

                            var subsamplePriority = Buffer[16 + i * 4];
                            var discardable = Buffer[17 + i * 4];

                            SampleSizes.Add(sampleSize);

                            if (discardable > 0)
                            {
                                sbSampleSizes.AppendLine($" {count,4} - {sampleSize,4} - discardable={discardable}");
                            }
                            else
                            {
                                sbSampleSizes.AppendLine($" {count,4} - {sampleSize,4}");
                            }

                            count++;
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

                    var sbTimeToSample = new StringBuilder();

                    int count = 1;
                    for (var i = 0; i < numberOfSampleTimes; i++)
                    {
                        var sampleCount = GetUInt(8 + i * 8);
                        var sampleDelta = GetUInt(12 + i * 8);
                        for (var j = 0; j < sampleCount; j++)
                        {
                            Ssts.Add(sampleDelta);
                            sbTimeToSample.AppendLine($" {count,4} - {sampleDelta}");
                            count++;
                        }
                    }

                    root.Nodes.Add(new TreeNode(Name)
                    {
                        Tag = "Element: " + Name + " - Time to Sample Box" + Environment.NewLine +
                              "Size: " + Size + Environment.NewLine +
                              "Position: " + StartPosition + Environment.NewLine +
                              "Number of samples (expanded): " + Ssts.Count + Environment.NewLine +
                              "Samples: " + Environment.NewLine +
                              "[   # - SampleDelta]" + Environment.NewLine +
                              sbTimeToSample + Environment.NewLine + Environment.NewLine + Environment.NewLine
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
                            var firstChunk = GetUInt(8 + i * 12);
                            var samplesPerChunk = GetUInt(12 + i * 12);
                            var sampleDescriptionIndex = GetUInt(16 + i * 12);
                            Stsc.Add(new SampleToChunkMap { FirstChunk = firstChunk, SamplesPerChunk = samplesPerChunk, SampleDescriptionIndex = sampleDescriptionIndex });
                            stscSb.AppendLine($"{firstChunk,4} - {samplesPerChunk,4} - {sampleDescriptionIndex,4}");
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

            var isSubtitle = false;
            if (handlerType != "vide" && handlerType != "soun")
            {
                GetParagraphs(fs, handlerType == "subp");
                isSubtitle = true;
            }

            if (Texts.Count > 0)
            {
                var sbTexts = new StringBuilder();
                for (var index = 0; index < Texts.Count; index++)
                {
                    var chunkText = Texts[index];
                    if (isSubtitle)
                    {
                        sbTexts.AppendLine($"{index + 1:###} - {chunkText.Offset:#######} - {chunkText.Text?.Replace(Environment.NewLine, "|")}");
                    }
                    else
                    {
                        sbTexts.AppendLine($"{index + 1:###} - {chunkText.Offset:#######}");
                    }
                }

                root.Nodes.Add(new TreeNode(Name)
                {
                    Tag = "Element: " + TextsName + " - Chunk Offset" + Environment.NewLine +
                          "Size: " + TextsSize + Environment.NewLine +
                          "Position: " + TextsPosition + Environment.NewLine +
                          "Number of chunks: " + Texts.Count + Environment.NewLine +
                          "Texts: " + Environment.NewLine +
                          "[   # - Offset " + (isSubtitle ? "- Text]" : string.Empty) + Environment.NewLine +
                          sbTexts
                });
            }
        }

        private void GetParagraphs(Stream fs, bool subtitlePicture)
        {
            uint samplesPerChunk = 1;
            var max = Texts.Count;
            var index = 0;
            for (var chunkIndex = 0; chunkIndex < max; chunkIndex++)
            {
                var newSamplesPerChunk = Stsc.FirstOrDefault(item => item.FirstChunk == chunkIndex + 1);
                if (newSamplesPerChunk != null)
                {
                    samplesPerChunk = newSamplesPerChunk.SamplesPerChunk;
                }

                var chunk = Texts[chunkIndex];
                for (var i = 0; i < samplesPerChunk; i++)
                {
                    if (index >= SampleSizes.Count || index >= Ssts.Count)
                    {
                        return;
                    }

                    var sampleSize = SampleSizes[index];

                    if (sampleSize > 2)
                    {
                        fs.Seek((long)chunk.Offset, SeekOrigin.Begin);
                        var buffer = new byte[2];
                        fs.Read(buffer, 0, buffer.Length);
                        var textSize = (uint)GetWord(buffer, 0);
                        if (textSize == 0 && samplesPerChunk > 1)
                        {
                            fs.Read(buffer, 0, buffer.Length);
                            textSize = (uint)GetWord(buffer, 0);
                        }

                        if (textSize > 0)
                        {
                            if (!subtitlePicture)
                            {
                                buffer = new byte[textSize];
                                fs.Read(buffer, 0, buffer.Length);
                                chunk.Text = GetString(buffer, 0, (int)textSize).TrimEnd();
                            }
                        }
                    }

                    index++;
                }
            }
        }
    }
}