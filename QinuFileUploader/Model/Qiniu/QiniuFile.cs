using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using QinuFileUploader.Model;
using QinuFileUploader.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QinuFileUploader.Model.Qiniu
{
    public class QiniuFile : IFileInfo
    {
        public const string QiniuFolderType = "application/qiniu-object-manager";

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string StorageType { get; set; }
        public void SetFolderType()
        {
            FileType = QiniuFolderType;
            Type = FileInfoType.Folder;
        }

        public string FileSize { get; set; }

        public string CreateDate { get; set; }

        public string EndUser { get; set; }

        public string Path
        {
            get
            {
                var fullUrl = string.Format("http://res.matoapp.net/{0}", FileName);
                return fullUrl;

            }
            set
            {
                throw new Exception();
            }
        }

        public int Type { get; set; }

        public ImageSource ImageSource => GetImageSource(Path);
        public  ImageSource GetImageSource(string value)
        {
            var mimeTypeManager = Ioc.Default.GetRequiredService<IMimeTypeManager>();
            BitmapSource bitmapSource = null;
            if (mimeTypeManager.GetMimeType(System.IO.Path.GetExtension(value.ToString())).StartsWith("image/"))
            {
                try
                {
                    Stream fs;
                    HttpClient httpClient = new HttpClient();
                    var httpStream = httpClient.GetStreamAsync(value.ToString()).Result;
                    const int bufferLength = 1024;
                    byte[] buffer = new byte[bufferLength];
                    int actual;
                    var memoryStream = new MemoryStream();
                    while ((actual = httpStream.Read(buffer, 0, bufferLength)) > 0)
                    {
                        memoryStream.Write(buffer, 0, actual);
                    }
                    memoryStream.Position = 0;
                    fs = memoryStream;



                    using (fs)
                    {
                        // Set the image source to the selected bitmap 
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSourceAsync(fs.AsRandomAccessStream());
                        bitmapSource = bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.UriSource = new Uri("ms-appx:///Assets/image.png");
                    bitmapSource = bitmapImage;

                }
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
