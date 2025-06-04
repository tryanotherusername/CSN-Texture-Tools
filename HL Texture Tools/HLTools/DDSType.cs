using System;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BCnEncoder.Decoder;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Runtime.InteropServices;

public class DDSHeaderInfo
{
    public string FourCC { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int PitchOrLinearSize { get; set; }
    public int Depth { get; set; }
    public int MipMapCount { get; set; }
    public int Flags { get; set; }
    public int Caps { get; set; }
    public int Caps2 { get; set; }
    public int Caps3 { get; set; }
    public int Caps4 { get; set; }
    public int Reserved2 { get; set; }
    public DDSPixelFormat PixelFormat { get; set; }
    public bool IsDX10 { get; set; }
    public int Size { get; set; }
    public int[] Reserved1 { get; set; }
    // Optionally add DX10 fields here if needed
}

public class DDSPixelFormat
{
    public int Size { get; set; }
    public int Flags { get; set; }
    public string FourCC { get; set; }
    public int RGBBitCount { get; set; }
    public int RBitMask { get; set; }
    public int GBitMask { get; set; }
    public int BBitMask { get; set; }
    public int ABitMask { get; set; }
}
public static class DDSHeaderParser
{
    /// <summary>
    /// Parses a detailed DDS header from a DDS byte array.
    /// </summary>
    public static DDSHeaderInfo ParseHeader(byte[] ddsData)
    {
        if (ddsData == null || ddsData.Length < 128)
            throw new ArgumentException("Data too short for a valid DDS file.");

        // 1. Check magic
        if (Encoding.ASCII.GetString(ddsData, 0, 4) != "DDS ")
            throw new ArgumentException("Not a DDS file.");

        int pos = 4;
        int size = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int flags = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int height = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int width = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pitchOrLinearSize = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int depth = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int mipMapCount = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int[] reserved1 = new int[11];
        for (int i = 0; i < 11; i++) { reserved1[i] = BitConverter.ToInt32(ddsData, pos); pos += 4; }

        // DDS_PIXELFORMAT (32 bytes)
        int pfSize = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pfFlags = BitConverter.ToInt32(ddsData, pos); pos += 4;
        string pfFourCC = Encoding.ASCII.GetString(ddsData, pos, 4).TrimEnd('\0'); pos += 4;
        int pfRGBBitCount = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pfRBitMask = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pfGBitMask = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pfBBitMask = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int pfABitMask = BitConverter.ToInt32(ddsData, pos); pos += 4;

        // Caps
        int caps = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int caps2 = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int caps3 = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int caps4 = BitConverter.ToInt32(ddsData, pos); pos += 4;
        int reserved2 = BitConverter.ToInt32(ddsData, pos); pos += 4;

        bool isDX10 = (pfFourCC == "DX10");

        return new DDSHeaderInfo
        {
            Size = size,
            Flags = flags,
            Height = height,
            Width = width,
            PitchOrLinearSize = pitchOrLinearSize,
            Depth = depth,
            MipMapCount = mipMapCount,
            Reserved1 = reserved1,
            PixelFormat = new DDSPixelFormat
            {
                Size = pfSize,
                Flags = pfFlags,
                FourCC = pfFourCC,
                RGBBitCount = pfRGBBitCount,
                RBitMask = pfRBitMask,
                GBitMask = pfGBitMask,
                BBitMask = pfBBitMask,
                ABitMask = pfABitMask,
            },
            FourCC = pfFourCC,
            Caps = caps,
            Caps2 = caps2,
            Caps3 = caps3,
            Caps4 = caps4,
            Reserved2 = reserved2,
            IsDX10 = isDX10
        };
    }
}

public class DDSDecoderHelper
{
    /// <summary>
    /// Decodes a DDS file from a byte array into a System.Drawing.Bitmap.
    /// </summary>
    /// <param name="ddsImageBytes">DDS file contents as a byte array.</param>
    /// <returns>Decoded Bitmap.</returns>
    public static Bitmap DecodeDDSToBitmap(byte[] ddsImageBytes)
    {
        using (var ms = new MemoryStream(ddsImageBytes))
        {
            var decoder = new BcDecoder();
            var decoded = decoder.Decode(ms);

            int width = decoded.Width;
            int height = decoded.Height;
            byte[] pixelData = decoded.data; // returns RGBA byte array
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                byte r = pixelData[i];
                byte g = pixelData[i + 1];
                byte b = pixelData[i + 2];
                // byte a = pixelData[i + 3]; // alpha remains unchanged

                // Swap R and B
                pixelData[i] = b;
                pixelData[i + 1] = g;
                pixelData[i + 2] = r;
                // pixelData[i + 3] = a; // alpha remains unchanged
            }
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
    }
}