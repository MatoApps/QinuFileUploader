using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Microsoft.UI.Xaml.Media;
using QinuFileUploader.Service;
using Microsoft.UI.Xaml.Media.Imaging;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace QinuFileUploader.Model.LocalFile
{
    public class LocalFile : IFileInfo
    {
        public const string FolderType = "application/local-object-manager";

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }

        public string CreateDate { get; set; }

        public string Path { get; set; }

        public void SetFolderType()
        {
            this.FileType = FolderType;
            Type = FileInfoType.Folder;

        }

        public int Type { get; set; }

        public ImageSource ImageSource => GetImageSource(Path).Result;
        public async Task<ImageSource> GetImageSource(string value)
        {
            var mimeTypeManager = Ioc.Default.GetRequiredService<IMimeTypeManager>();
            BitmapSource bitmapSource = null;
            if (mimeTypeManager.GetMimeType(System.IO.Path.GetExtension(value.ToString())).StartsWith("image/"))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(value);
                bitmapSource = bitmapImage;
            }
            else
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri("ms-appx:///Assets/file.png");
                bitmapSource = bitmapImage;
            }

            return bitmapSource;
        }

    }
}
