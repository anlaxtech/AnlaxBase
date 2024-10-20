using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AnlaxBase
{
    internal class ImageUtils
    {
        public static BitmapImage NewBitmapImage(Image img, int pixels = 32, int dpi = 96)
        {
            // Создаем новое изображение с заданным разрешением DPI
            Bitmap newBitmap = new Bitmap(img.Width, img.Height);
            newBitmap.SetResolution(dpi, dpi);

            // Копируем содержимое изображения img в новое изображение с заданным разрешением
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);
            }

            // Масштабируем изображение до указанных размеров пикселей
            Bitmap scaledBitmap = new Bitmap(newBitmap, new System.Drawing.Size(pixels, pixels));
            return ConvertBitmapToBitmapImage(scaledBitmap);
        }
        public static BitmapImage NewBitmapImage(Uri imageUri, int pixels = 32, int dpi = 96)
        {
            // Загружаем изображение из Uri
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = imageUri;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            // Преобразуем его в формат Bitmap
            Bitmap bitmap;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                bitmap = new Bitmap(memoryStream);
            }

            // Создаем новое изображение с заданным разрешением DPI
            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            newBitmap.SetResolution(dpi, dpi);

            // Копируем содержимое изображения в новое изображение с заданным разрешением
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            // Масштабируем изображение до указанных размеров пикселей
            Bitmap scaledBitmap = new Bitmap(newBitmap, new System.Drawing.Size(pixels, pixels));
            return ConvertBitmapToBitmapImage(scaledBitmap);
        }
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
