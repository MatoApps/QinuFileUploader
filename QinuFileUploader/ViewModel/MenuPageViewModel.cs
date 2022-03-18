using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QinuFileUploader.Common;
using QinuFileUploader.Helper;
using QinuFileUploader.Model;
using QinuFileUploader.Model.LocalFile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Workshop.Infrastructure.Common;
using Workshop.Infrastructure.Helper;
using Workshop.Service.Manager;

namespace Workshop.ViewModel
{
    public class MenuPageViewModel : ObservableObject
    {
        private readonly IQiniuManager qiniuManager;

        public RelayCommand<string> SearchCommand { get; private set; }
        public RelayCommand AddImageCommand { get; }
        public RelayCommand<IFileInfo> RemoveImageCommand { get; private set; }

        public MenuPageViewModel(IQiniuManager qiniuManager)
        {
            this.qiniuManager = qiniuManager;
            this.SearchCommand = new RelayCommand<string>(SearchAction);
            this.AddImageCommand = new RelayCommand(AddImageAction);
            this.RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction);
            InitData();

        }


        private void SearchAction(string keyword)
        {
            InitData(keyword);
        }


        private async void InitData(string keyword = "")
        {
            var storageSK = ConfigureProvider.StorageAppSecret;
            await Bucket(storageSK);
            var fgalleryList = await qiniuManager.Search(qiniuManager.Bucket, keyword);


            var folders = fgalleryList.Where(c => c.IsFolder).GroupBy(c => c.FileName).Select(c => c.Key).ToList();

            foreach (var folder in folders)
            {
                var pathArray = folder.Split('/');
                if (pathArray.Any())
                {
                    foreach (var path in pathArray)
                    {
                        var e = new ExplorerItem()
                        {
                            Name = path,
                            Type = ExplorerItem.ExplorerItemType.Folder,
                            Children = new ObservableCollection<ExplorerItem>()

                        };
                    }
                }
            }

            this.FileInfos = new ObservableCollection<IFileInfo>(fgalleryList.Select(c => c));
        }

        public ExplorerItem a(ExplorerItem ex)
        {
            var children = new ObservableCollection<ExplorerItem>();

            children.Add(ex);

            var e = new ExplorerItem()
            {
                Name = path,
                Type = ExplorerItem.ExplorerItemType.Folder,
                Children = children
            };

            return a(e);
        }



        private void RemoveImageAction(IFileInfo obj)
        {
            var current = this.FileInfos.First(c => c == obj);
            if (current != null)
            {
                FileInfos.RemoveAt(FileInfos.IndexOf(current));
            }

        }

        private async void AddImageAction()
        {

            var open = new FileOpenPicker();
            open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".png");

            var file = await open.PickSingleFileAsync();

            var basicProp = await file.GetBasicPropertiesAsync();

            var localfile = new LocalFileInfo()
            {
                FileName = file.Name,
                FileType = file.FileType,
                FileSize = QiNiuHelper.GetFileSize((long)basicProp.Size),
                CreateDate = basicProp.ItemDate.ToString("yyyy/MM/dd HH:mm:ss")
            };

            this.FileInfos.Add(localfile);
        }


        private async Task UploadImage()
        {
            int index = 0;

            var currentDomainsResult = await qiniuManager.SetCurrentDomain(qiniuManager.Bucket);

            foreach (var item in this.FileInfos)
            {
                if (index > 5)
                {
                    return;
                }
                var fileExtension = "";
                //key格式: /670b14728ad9902aecba32e22fa4f6bd/670b14728ad9902aecba32e22fa4f6bd.jpg
                var isFromNet = item.Path.StartsWith("http");
                if (!isFromNet)
                {
                    fileExtension = Path.GetExtension(item.Path);

                }
                else
                {
                    fileExtension = string.Format(".{0}", item.Path.Split('.').LastOrDefault());
                }
                string fileName = Guid.NewGuid().ToString("N");
                var key = string.Format("{{0}{1}", fileName, fileExtension);
                var callbackBody = string.Format("key=$(key)&hash=$(etag)&bucket=$(bucket)&fsize=$(fsize)");
                var uploadResult = await qiniuManager.UploadSingle(item.Path, key, callbackBody);
                if (uploadResult)
                {

                }
                else
                {
                    await MessageBox.ShowAsync("上传图片失败");
                    break;
                }

                index++;
            }

        }

        private async Task Bucket(string storageSK)
        {
            var currentArea = qiniuManager.GetQiniuAreas().First(c => c.Name == "华南");
            var bucketList = await qiniuManager.ConnectServer(ConfigureProvider.StorageAppKey, storageSK, currentArea.ZoneValue);

            qiniuManager.Bucket = bucketList.First();
        }

        private ObservableCollection<IFileInfo> _candidateFilePathList;

        public ObservableCollection<IFileInfo> FileInfos
        {
            get { return _candidateFilePathList; }
            set
            {
                _candidateFilePathList = value;
                OnPropertyChanged(nameof(FileInfos));
            }
        }

        private string _keyWord;

        public string KeyWord
        {
            get { return _keyWord; }
            set
            {
                _keyWord = value;
                OnPropertyChanged(nameof(KeyWord));
            }
        }


        private int _uploadPercent;

        public int UploadPercent
        {
            get { return _uploadPercent; }
            set
            {
                _uploadPercent = value;
                OnPropertyChanged(nameof(UploadPercent));

            }
        }

    }
}
