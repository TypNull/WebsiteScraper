using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WebsiteCreator.MVVM.Model
{
    internal class IOManager
    {
        public static BitmapSource LoadImageFromFile(string path)
        {
            string _orientationQuery = "System.Photo.Orientation";
            Rotation rotation = Rotation.Rotate0;

            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
            BitmapFrame bitmapFrame = BitmapFrame.Create(fileStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

            if ((bitmapFrame.Metadata is BitmapMetadata bitmapMetadata) && (bitmapMetadata.ContainsQuery(_orientationQuery)))
            {
                object o = bitmapMetadata.GetQuery(_orientationQuery);
                if (o != null)
                    switch ((ushort)o)
                    {
                        case 6:
                            rotation = Rotation.Rotate90;
                            break;
                        case 3:
                            rotation = Rotation.Rotate180;
                            break;
                        case 8:
                            rotation = Rotation.Rotate270;
                            break;
                    }
            }

            BitmapImage image = new();
            image.BeginInit();
            image.DecodePixelWidth = 300;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = fileStream;
            image.Rotation = rotation;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static byte[]? BitmapSourceToByte(BitmapSource source)
        {
            PngBitmapEncoder encoder = new();
            if (source == null)
                return null;
            BitmapFrame frame = BitmapFrame.Create(source);
            encoder.Frames.Add(frame);
            MemoryStream stream = new();

            encoder.Save(stream);
            return stream.ToArray();
        }

        public static BitmapSource ToBitmapSource(byte[] bytes) => BitmapFrame.Create(new MemoryStream(bytes));

        public static async Task<BitmapSource?> LoadImage(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
                return null;
            BitmapImage? bitmap = null;
            try
            {
                HttpResponseMessage response = await DownloadAssistant.Base.HttpGet.HttpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    using MemoryStream stream = new();
                    await response.Content.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                return bitmap;

            }
            catch (Exception)
            {
                return null;
            }

        }

        public static async Task<string?> LoadHtmlFromUrlAsync(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
                return null;
            Debug.WriteLine("Download Website: " + url);
            try
            {
                DownloadAssistant.Request.SiteRequest request = new(url);
                await request.Task;
                return request.HTML;
            }
            catch (Exception)
            {
                Debug.WriteLine("Error");
            }
            return null;
        }
    }
}
