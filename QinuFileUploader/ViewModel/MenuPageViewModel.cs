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
using QinuFileUploader.Service;
using Workshop.Infrastructure.Common;
using Workshop.Infrastructure.Helper;
using Workshop.Service.Manager;

namespace Workshop.ViewModel
{
    public class MenuPageViewModel : ObservableObject
    {
        private readonly IQiniuManager _qiniuManager;
        private readonly IMimeTypeManager _mimeTypeManager;

        private const string Rootname = "我的网盘";
        private const char SpliterChar = '/';
        public RelayCommand<string> SearchCommand { get; private set; }
        public RelayCommand NavigationHistoryBackCommand { get; private set; }
        public RelayCommand NavigationHistoryForwardCommand { get; private set; }
        public RelayCommand NavigationBackCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand AddImageCommand { get; }
        public RelayCommand ToggleDetailCommand { get; }
        public RelayCommand ToggleTreeCommand { get; }
        public RelayCommand<IFileInfo> RemoveImageCommand { get; private set; }

        public MenuPageViewModel(IQiniuManager qiniuManager, IMimeTypeManager mimeTypeManager)
        {
            this._qiniuManager = qiniuManager;
            _mimeTypeManager = mimeTypeManager;
            this.PropertyChanged += MenuPageViewModel_PropertyChanged;

            //init commands
            this.RefreshCommand = new RelayCommand(RefreshAction);
            this.NavigationHistoryBackCommand = new RelayCommand(NavigationHistoryBack);
            this.NavigationHistoryForwardCommand = new RelayCommand(NavigationHistoryForward);
            this.NavigationBackCommand = new RelayCommand(NavigationBack);

            this.SearchCommand = new RelayCommand<string>(SearchAction);
            this.AddImageCommand = new RelayCommand(AddImageAction);
            this.RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction);

            this.ToggleDetailCommand = new RelayCommand(ToggleDetailAction);
            this.ToggleTreeCommand = new RelayCommand(ToggleTreeAction);


            //init data
            this.NavigationStack = new ObservableCollectionEx<ExplorerItem>();
            this.NavigationHistoryStack = new ObservableCollectionEx<ExplorerItem>();
            this.KeyWord = "";

            this.PathList = new ObservableCollectionEx<string>();
            InitData();

        }

        private void ToggleTreeAction()
        {
            this.IsShowTree = !this.IsShowTree;
        }

        private void ToggleDetailAction()
        {
            this.IsShowDetail = !this.IsShowDetail;
        }

        private void RefreshAction()
        {
            InitData();
        }

        private async void MenuPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentExplorerItem))
            {
                if (this.CurrentExplorerItem == null || string.IsNullOrEmpty(this.CurrentExplorerItem.Path))
                {
                    return;
                }

                this.PathList.Clear();
                foreach (var path in CurrentExplorerItem.Path.Split(SpliterChar).ToList())
                {
                    this.PathList.Add(path);
                }
                await RefreshCurrentFileInfosAsync();
            }

        }

        private async Task RefreshCurrentFileInfosAsync(string keyword = "")
        {


            var targetPath = this.CurrentExplorerItem.Path + SpliterChar + keyword;
            targetPath = EnsureOriginUrl(targetPath);

            var targetDirectoryPath = Path.GetDirectoryName(targetPath + SpliterChar);
            if (targetDirectoryPath == null)
            {
                targetDirectoryPath = string.Empty;
            }

            var fileInfos = await _qiniuManager.Search(_qiniuManager.Bucket, targetPath);
            var currentFileInfos = new ObservableCollectionEx<IFileInfo>(fileInfos.Where(c => !c.IsFolder).Where(c =>
            {
                var result = Path.GetDirectoryName(c.FileName) == targetDirectoryPath;
                return result;
            }));
            currentFileInfos.CollectionChanged += FileInfos_CollectionChangedAsync;
            this.CurrentFileInfos = currentFileInfos;

        }

        private string EnsureOriginUrl(string targetPath)
        {
            if (targetPath.StartsWith(Rootname))
            {
                targetPath = targetPath.Substring(Rootname.Length, targetPath.Length - Rootname.Length);
            }

            if (targetPath.StartsWith(SpliterChar))
            {
                targetPath = targetPath.Substring(1, targetPath.Length - 1);
            }

            return targetPath;
        }

        private async void SearchAction(string keyword)
        {
            await this.RefreshCurrentFileInfosAsync(keyword);
        }


        private async void InitData(string keyword = "")
        {
            var storageSK = ConfigureProvider.StorageAppSecret;
            await Bucket(storageSK);
            var fgalleryList = await _qiniuManager.Search(_qiniuManager.Bucket, keyword);

            var folders = fgalleryList.Where(c => c.IsFolder).GroupBy(c => c.FileName).Select(c => c.Key).ToList();

            ExplorerItem root = null;
            foreach (var folder in folders)
            {
                var trimdFolder = Rootname + SpliterChar + folder;
                if (trimdFolder.EndsWith(SpliterChar))
                {
                    trimdFolder = trimdFolder.Substring(0, trimdFolder.Length - 1);
                }
                var pathArray = trimdFolder.Split(SpliterChar);

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
                        var path = string.Join(SpliterChar, pathArray);

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
                    var children = new ObservableCollectionEx<ExplorerItem>();
                    children.Add(ex);
                    var path = string.Join(SpliterChar, pathArray, 0, index + 1);

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

            this.RootExplorerItems = new ObservableCollectionEx<ExplorerItem>() { root };

            this.NavigationHistoryStack.Add(root);
            this.NavigationStack.Add(root);

            this.CurrentExplorerItem = RootExplorerItems.FirstOrDefault();
        }

        private async void FileInfos_CollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
        {
            var currentDomainsResult = await _qiniuManager.SetCurrentDomain(_qiniuManager.Bucket);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var item = e.NewItems[0] as IFileInfo;
                    var callbackBody = string.Format("key=$(key)&hash=$(etag)&bucket=$(bucket)&fsize=$(fsize)");

                    var filename = EnsureOriginUrl(item.FileName);

                    var uploadResult = await _qiniuManager.UploadSingle(item.Path, filename, callbackBody);
                    if (uploadResult)
                    {
                        await this.RefreshCurrentFileInfosAsync();

                    }
                    else
                    {
                        await UIHelper.ShowAsync("上传图片成功，但是回调失败了");
                        await this.RefreshCurrentFileInfosAsync();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var deleteResult = await _qiniuManager.Delete(new List<Model.Qiniu.QiNiuFileInfo>() { e.OldItems[0] as Model.Qiniu.QiNiuFileInfo });
                    if (deleteResult)
                    {
                        await this.RefreshCurrentFileInfosAsync();

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
            var filename = this.CurrentExplorerItem.Path + SpliterChar + file.Name;
            var fileType = _mimeTypeManager.GetMimeType(file.FileType);
            var localfile = new LocalFileInfo()
            {
                FileName = filename,
                FileType = fileType,
                FileSize = QiNiuHelper.GetFileSize((long)basicProp.Size),
                CreateDate = basicProp.ItemDate.ToString("yyyy/MM/dd HH:mm:ss"),
                Path = file.Path,
            };

            this.CurrentFileInfos.Insert(0, localfile);
        }


        private async Task Bucket(string storageSK)
        {
            var currentArea = _qiniuManager.GetQiniuAreas().First(c => c.Name == "华南");
            var bucketList = await _qiniuManager.ConnectServer(ConfigureProvider.StorageAppKey, storageSK, currentArea.ZoneValue);

            _qiniuManager.Bucket = bucketList.First();
        }




        private void NavigationBack()
        {
            if (this.NavigationStack.Count == 1)
            {
                return;
            }
            this.NavigationStack.Pop();
            var lastItem = this.NavigationStack.LastOrDefault();
            if (lastItem == null)
            {
                return;
            }
            if (this.ToFolder(lastItem))
            {

                this.NavigationHistoryStack.ForEach((element) =>
                {
                    element.IsCurrent = false;
                });
                lastItem.IsCurrent = true;

                this.PushNavigationHistoryStack(lastItem);
            }
        }
        private void PushNavigationHistoryStack(ExplorerItem item)
        {
            var newItem = new ExplorerItem
            {
                Name = item.Name,
                Path = item.Path,
                IsCurrent = item.IsCurrent,
                Type = item.Type,
                Children = item.Children,
                IsExpanded = false
            };

            if (this.NavigationHistoryStack.Count > 10)
            {
                this.NavigationHistoryStack.Pop();
            }
            this.NavigationHistoryStack.Unshift(newItem);
        }

        private void DealWithNavigationStack(ExplorerItem folder)
        {

            this.NavigationStack.Clear();
            var paths = folder.Path.Split(SpliterChar);


            void a(IEnumerable<ExplorerItem> ex, int index)
            {
                if (index > paths.Length - 1)
                {
                    return;
                }

                var currentName = paths[index];
                var currentExplorerItem = ex.FirstOrDefault(c => c.Name == currentName);
                if (currentExplorerItem == null)
                {
                    return;

                }
                this.NavigationStack.Push(currentExplorerItem);
                a(currentExplorerItem.Children, index + 1);
            }
            a(this.RootExplorerItems, 0);
        }

        public void NavigationTo(ExplorerItem folder)
        {
            this.DealWithNavigationStack(folder);
            if (this.ToFolder(folder))
            {
                this.NavigationHistoryStack.ForEach((element) =>
                {
                    element.IsCurrent = false;
                });
                folder.IsCurrent = true;
                this.PushNavigationHistoryStack(folder);
            }
        }

        private void NavigationHistoryBack()
        {
            var currentIndex = (this.NavigationHistoryStack).IndexOf(
              (c) => c.IsCurrent
            );
            if (currentIndex < this.NavigationHistoryStack.Count - 1)
            {
                var forwardIndex = currentIndex + 1;

                var folder = this.NavigationHistoryStack[forwardIndex];
                this.DealWithNavigationStack(folder);

                if (this.ToFolder(folder))
                {
                    this.NavigationHistoryStack.ForEach((element) =>
                    {
                        element.IsCurrent = false;
                    });
                    this.NavigationHistoryStack[forwardIndex].IsCurrent = true;
                }
            }
        }

        private void NavigationHistoryForward()
        {

            var currentIndex = this.NavigationHistoryStack.IndexOf(
              (c) => c.IsCurrent
            );
            if (currentIndex > 0)
            {
                var forwardIndex = currentIndex - 1;

                var folder = this.NavigationHistoryStack[forwardIndex];
                this.DealWithNavigationStack(folder);

                if (this.ToFolder(folder))
                {
                    this.NavigationHistoryStack.ForEach((element) =>
                    {
                        element.IsCurrent = false;
                    });
                    this.NavigationHistoryStack[forwardIndex].IsCurrent = true;
                }
            }
        }

        private bool ToFolder(ExplorerItem item)
        {
            if (item == null || item.Path == this.CurrentExplorerItem.Path)
            {
                return false;
            }
            //var paths = item.Path.Split(SpliterChar);

            //ExplorerItem a(IEnumerable<ExplorerItem> ex, int index)
            //{
            //    if (index > paths.Length - 1)
            //    {
            //        return null;
            //    }

            //    var currentName = paths[index];
            //    var currentExplorerItem = ex.FirstOrDefault(c => c.Name == currentName);
            //    if (currentExplorerItem == null)
            //    {
            //        return null;
            //    }
            //    return a(currentExplorerItem.Children, index + 1);
            //}
            //var currentExplorerItem = a(this.RootExplorerItems, 0);

            var currentExplorerItem = this.NavigationStack.FirstOrDefault(c => c.Path == item.Path);

            if (currentExplorerItem == null)
            {
                return false;
            }

            this.CurrentExplorerItem = currentExplorerItem;
            return true;
        }


        private bool GetIsCurrentHistoryNavigationItem(ExplorerItem item)
        {
            var result = item.IsCurrent;
            return result;
        }



        private ObservableCollectionEx<ExplorerItem> _navigationStack;

        public ObservableCollectionEx<ExplorerItem> NavigationStack
        {
            get { return _navigationStack; }
            set
            {
                _navigationStack = value;
                OnPropertyChanged(nameof(NavigationStack));
            }
        }

        private ObservableCollectionEx<ExplorerItem> _navigationHistoryStack;

        public ObservableCollectionEx<ExplorerItem> NavigationHistoryStack
        {
            get { return _navigationHistoryStack; }
            set
            {
                _navigationHistoryStack = value;
                OnPropertyChanged(nameof(NavigationHistoryStack));
            }
        }


        private ObservableCollectionEx<IFileInfo> _currentFileInfos;

        public ObservableCollectionEx<IFileInfo> CurrentFileInfos
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

        private ObservableCollectionEx<ExplorerItem> _rootExplorerItem;

        public ObservableCollectionEx<ExplorerItem> RootExplorerItems
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

        private ObservableCollectionEx<string> _pathList;

        public ObservableCollectionEx<string> PathList
        {
            get { return _pathList; }
            set
            {
                _pathList = value;
                OnPropertyChanged(nameof(PathList));


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

        private bool _isShowTree;

        public bool IsShowTree
        {
            get { return _isShowTree; }
            set
            {
                _isShowTree = value;
                OnPropertyChanged(nameof(IsShowTree));
            }
        }
        private bool _isShowDetail;

        public bool IsShowDetail
        {
            get { return _isShowDetail; }
            set
            {
                _isShowDetail = value;
                OnPropertyChanged(nameof(IsShowDetail));

            }
        }


    }
}
