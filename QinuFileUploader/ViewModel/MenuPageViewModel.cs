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
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand AddImageCommand { get; }
        public RelayCommand<IFileInfo> RemoveImageCommand { get; private set; }

        public MenuPageViewModel(IQiniuManager qiniuManager)
        {
            this.qiniuManager = qiniuManager;
            this.PropertyChanged += MenuPageViewModel_PropertyChanged;

            this.SearchCommand = new RelayCommand<string>(SearchAction);
            this.RefreshCommand = new RelayCommand(RefreshAction);
            this.AddImageCommand = new RelayCommand(AddImageAction);
            this.RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction);
            this.KeyWord = "";
            InitData();

        }

        private void RefreshAction()
        {
            InitData();
        }

        private void MenuPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentExplorerItem))
            {
                if (this.CurrentExplorerItem == null || string.IsNullOrEmpty(this.CurrentExplorerItem.Path))
                {
                    return;
                }
                RefreshCurrentFileInfosAsync();
            }

        }

        private async Task RefreshCurrentFileInfosAsync(string keyword = "")
        {


            var targetPath = this.CurrentExplorerItem.Path + '/' + keyword;
            var targetDirectoryPath = Path.GetDirectoryName(this.CurrentExplorerItem.Path + '/');

            var fileInfos = await qiniuManager.Search(qiniuManager.Bucket, targetPath);
            var currentFileInfos = new ObservableCollection<IFileInfo>(fileInfos.Where(c => !c.IsFolder).Where(c =>
            {
                var result = Path.GetDirectoryName(c.FileName) == targetDirectoryPath;
                return result;
            }));
            currentFileInfos.CollectionChanged += FileInfos_CollectionChangedAsync;
            this.CurrentFileInfos = currentFileInfos;

        }

        private void SearchAction(string keyword)
        {
            this.RefreshCurrentFileInfosAsync(keyword);
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

            this.CurrentExplorerItem = RootExplorerItems.FirstOrDefault();
        }

        private async void FileInfos_CollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
        {
            var currentDomainsResult = await qiniuManager.SetCurrentDomain(qiniuManager.Bucket);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var item = e.NewItems[0] as IFileInfo;
                    var callbackBody = string.Format("key=$(key)&hash=$(etag)&bucket=$(bucket)&fsize=$(fsize)");
                    var uploadResult = await qiniuManager.UploadSingle(item.Path, item.FileName, callbackBody);
                    if (uploadResult)
                    {
                        this.RefreshCurrentFileInfosAsync();

                    }
                    else
                    {
                        await UIHelper.ShowAsync("上传图片成功，但是回调失败了");
                        this.RefreshCurrentFileInfosAsync();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var deleteResult = await qiniuManager.Delete(new List<Model.Qiniu.QiNiuFileInfo>() { e.OldItems[0] as Model.Qiniu.QiNiuFileInfo });
                    if (deleteResult)
                    {
                        this.RefreshCurrentFileInfosAsync();

                    }
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
        }

        private void RemoveImageAction(IFileInfo obj)
        {
            var current = this.CurrentFileInfos.First(c => c.FileName == obj.FileName);
            if (current != null)
            {
                CurrentFileInfos.RemoveAt(CurrentFileInfos.IndexOf(current));
            }

        }

        private async void AddImageAction()
        {

            var open = new FileOpenPicker();
            open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".png");
            open.FileTypeFilter.Add(".jpg");

            UIHelper.InitFileOpenPicker(open);

            var files = await open.PickMultipleFilesAsync();
            if (files.Count == 0)
            {
                return;
            }
            var file = files[0];

            var basicProp = await file.GetBasicPropertiesAsync();
            var filename = this.CurrentExplorerItem.Path + "/" + file.Name;

            var localfile = new LocalFileInfo()
            {
                FileName = filename,
                FileType = file.FileType,
                FileSize = QiNiuHelper.GetFileSize((long)basicProp.Size),
                CreateDate = basicProp.ItemDate.ToString("yyyy/MM/dd HH:mm:ss"),
                Path = file.Path,
            };

            this.CurrentFileInfos.Insert(0, localfile);
        }


        private async Task Bucket(string storageSK)
        {
            var currentArea = qiniuManager.GetQiniuAreas().First(c => c.Name == "华南");
            var bucketList = await qiniuManager.ConnectServer(ConfigureProvider.StorageAppKey, storageSK, currentArea.ZoneValue);

            qiniuManager.Bucket = bucketList.First();
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
