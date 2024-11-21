using System.IO;
using System.Windows.Media.Imaging;
using System;

public class BitmapSourceConverter
{
    public static string ConvertBitmapSourceToString(BitmapSource bitmapSource)
    {
        // Step 1: Create a BitmapEncoder to save the bitmap
        using (MemoryStream memoryStream = new MemoryStream())
        {
            // Create a PNG encoder
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            // Step 2: Save the bitmap to memory stream
            encoder.Save(memoryStream);

            // Step 3: Convert the memory stream to a Base64 string
            byte[] imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }
}