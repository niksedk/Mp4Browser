using System.IO;

namespace Mp4Browser.Mp4.Boxes
{
    public class Tkhd : Box
    {
        public readonly uint TrackId;
        public readonly ulong Duration;
        public readonly uint Width;
        public readonly uint Height;

        // Rotation Matrix
        //             a   b   u
        // x y 1  *    c   d   v  = x y 1
        //             tx  ty  w
        //All values in the matrix are 32-bit fixed-point numbers divided as 16.16, except for the {u, v, w} column,
        //which contains 32-bit fixed-point numbers divided as 2.30.Figure 4 - 1(page 200) depicts how QuickTime
        //uses matrices to transform displayed objects.
        //

        public float[] RotationMatrix; // UVW rotate mapping

        //version           // 8 unsigned bit
        //flags             // 24 unsigned bit
        //creationTime      // 32 or 64 unsigned bit
        //modificationTime  // 32 or 64 unsigned bit
        //trackID           // 32 unsigned bit
        //__reserved__      // 32 bit
        //duration          // 32 or 64 unsigned bit
        //__reserved__[2]   // 2 32 bit
        //layer             // 16 unsingned bit
        //alternateGroup    // 16 unsigned bit
        //volume            // 16 float bit
        //__reserved__      // 16 bit
        //matrix[9]         // 3 X 3 32 float bit
        //width             // 32 unsigned bit
        //height            // 32 unsigned bit

        public Tkhd(Stream fs)
        {
            Buffer = new byte[84];
            var bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
            {
                return;
            }

            var version = Buffer[0];
            var addToIndex64Bit = 0;
            if (version == 1)
            {
                addToIndex64Bit = 8;
            }

            TrackId = GetUInt(12 + addToIndex64Bit);
            if (version == 1)
            {
                Duration = GetUInt64(20 + addToIndex64Bit);
                addToIndex64Bit += 4;
            }
            else
            {
                Duration = GetUInt(20 + addToIndex64Bit);
            }

            RotationMatrix = new float[9];
            //TODO: fix reading with correct # before/after decimal
            RotationMatrix[0] = GetWord(40 + addToIndex64Bit); // 16.16
            RotationMatrix[1] = GetWord(44 + addToIndex64Bit); // 16.16
            RotationMatrix[2] = GetWord(48 + addToIndex64Bit); // 2.30
            RotationMatrix[3] = GetWord(52 + addToIndex64Bit); // 16.16
            RotationMatrix[4] = GetWord(56 + addToIndex64Bit); // 16.16
            RotationMatrix[5] = GetWord(60 + addToIndex64Bit); // 2.30
            RotationMatrix[6] = GetWord(64 + addToIndex64Bit); // 16.16
            RotationMatrix[7] = GetWord(68 + addToIndex64Bit); // 16.16
            RotationMatrix[8] = GetWord(72 + addToIndex64Bit); // 2.30

            Width = (uint)GetWord(76 + addToIndex64Bit); // skip decimals
            Height = (uint)GetWord(80 + addToIndex64Bit); // skip decimals
        }
    }
}
