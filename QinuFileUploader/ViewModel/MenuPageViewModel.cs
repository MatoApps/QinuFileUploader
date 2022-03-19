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
            this.PropertyChanged += MenuPageViewModel_PropertyChanged;

            this.SearchCommand = new RelayCommand<string>(SearchAction);
            this.AddImageCommand = new RelayCommand(AddImageAction);
            this.RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction);
            InitData();

        }

        private void MenuPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentExplorerItem))
            {
                if (string.IsNullOrEmpty(this.CurrentExplorerItem.Path))
                {
                    return;
                }
                var targetPath = Path.GetDirectoryName(this.CurrentExplorerItem.Path+'/');
                this.CurrentFileInfos = new ObservableCollection<IFileInfo>(this.FileInfos.Where(c =>
                {
                    var result = Path.GetDirectoryName(c.FileName) == targetPath;
                    return result;
                }));
            }
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

            ExplorerItem root = null;
            foreach (var folder in folders)
            {
                var trimdFolder = folder;
                if (folder.EndsWith('/'))
                {
                    trimdFolder = folder.Substring(0, folder.Length - 1);
                }
                var pathArray = trimdFolder.Split('/');

                void b(ref ExplorerItem current, int index)
                {
                    if (index == pathArray.Length - 1)
                    {
                        return;
                    }

                    var name = pathArray[index + 1];

                    var next = current.Children.FirstOrDefault(c => c.Name == name);
                    if (next == null)
                    {
                        var path = string.Join("/", pathArray);

                        var currentExplorerItem = new ExplorerItem()
                        {
                            Name = pathArray[pathArray.Length - 1],
                            Type = ExplorerItem.ExplorerItemType.Folder,
                            Path = path
                        };
                        var appendItem = a(currentExplorerItem, pathArray.Length - 2, index);
                        current.Children.Add(appendItem);
                        return;
                    }
                    b(ref next, index + 1);

                }

                ExplorerItem a(ExplorerItem ex, int index, int stopIndex = 0)
                {
                    if (index == stopIndex)
                    {
                        return ex;
                    }
                    var children = new ObservableCollection<ExplorerItem>();
                    children.Add(ex);
                    var path = string.Join("/", pathArray, 0, index + 1);

                    var e = new ExplorerItem()
                    {
                        Name = pathArray[index],
                        Path = path,
                        Type = ExplorerItem.ExplorerItemType.Folder,
                        Children = children
                    };
                    return a(e, index - 1);
                }
                if (pathArray.Any())
                {
                    if (root == null)
                    {
                        root = new ExplorerItem()
                        {
                            Name = pathArray[0],
                            Type = ExplorerItem.ExplorerItemType.Folder,
                            Path = pathArray[0]
                        };
                    }
                    b(ref root, 0);

                }
            }

            this.RootExplorerItems = new ObservableCollection<ExplorerItem>() { root };

            this.FileInfos = new ObservableCollection<IFileInfo>(fgalleryList.Where(c => !c.IsFolder));

            FileInfos.CollectionChanged += FileInfos_CollectionChangedAsync;
            this.CurrentExplorerItem = RootExplorerItems.FirstOrDefault();
        }

        private async Task FileInfos_CollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
        {

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
            qiniuManager.Delete()
            !await MenuManager.DeleteMenu(e.OldItems[0] as Menu)
            throw new NotImplementedException();
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

        private ObservableCollection<IFileInfo> _fileInfos;

        public ObservableCollection<IFileInfo> FileInfos
        {
            get { return _fileInfos; }
            set
            {
                _fileInfos = value;
                OnPropertyChanged(nameof(FileInfos));
            }
        }

        private ObservableCollection<IFileInfo> _currentFileInfos;

        public ObservableCollection<IFileInfo> CurrentFileInfos
        {
            get { return _currentFileInfos; }
            set
            {
                _currentFileInfos = value;
                OnPropertyChanged(nameof(CurrentFileInfos));
            }
        }

        private IFileInfo _selectedFileInfo;

        public IFileInfo SelectedFileInfo
        {
            get { return _selectedFileInfo; }
            set
            {
                _selectedFileInfo = value;
                OnPropertyChanged(nameof(SelectedFileInfo));
            }
        }

        private ObservableCollection<ExplorerItem> _rootExplorerItem;

        public ObservableCollection<ExplorerItem> RootExplorerItems
        {
            get { return _rootExplorerItem; }
            set
            {
                _rootExplorerItem = value;
                OnPropertyChanged(nameof(RootExplorerItems));
            }
        }

        private ExplorerItem _currentExplorerItem;

        public ExplorerItem CurrentExplorerItem
        {
            get { return _currentExplorerItem; }
            set
            {
                _currentExplorerItem = value;
                OnPropertyChanged(nameof(CurrentExplorerItem));

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
