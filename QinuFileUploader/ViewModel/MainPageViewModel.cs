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
using QinuFileUploader;
using Microsoft.UI.Xaml.Controls;
using QinuFileUploader.Model.Qiniu;
using Windows.Storage;

namespace QinuFileUploader.ViewModel
{
    public class MainPageViewModel : ExplorerViewModel
    {
        private readonly IQiniuManager _qiniuManager;
        private readonly IMimeTypeManager _mimeTypeManager;

        private string _rootname;
        public RelayCommand<string> SearchCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand AddImageCommand { get; }
        public RelayCommand AddFolderCommand { get; }
        public RelayCommand ToggleDetailCommand { get; }
        public RelayCommand ToggleTreeCommand { get; }
        public RelayCommand<IFileInfo> RemoveImageCommand { get; private set; }
        public RelayCommand<IFileInfo> DownloadCommand { get; private set; }

        public MainPageViewModel(IQiniuManager qiniuManager, IMimeTypeManager mimeTypeManager, SettingsPageViewModel settingsPageViewModel) : base()
        {
            _qiniuManager = qiniuManager;
            _qiniuManager.PropertyChanged += _qiniuManager_PropertyChanged;
            _mimeTypeManager = mimeTypeManager;

            settingsPageViewModel.OnSubmit += SettingsPageViewModel_OnSubmit;
            settingsPageViewModel.OnReload += SettingsPageViewModel_OnReload;

            PropertyChanged += MenuPageViewModel_PropertyChanged;

            //init commands
            RefreshCommand = new RelayCommand(RefreshAction, () => IsServiceIdle());
            SearchCommand = new RelayCommand<string>(SearchAction, (s) => CanCurrentExplorerItemRelevantDo() && IsServiceIdle());
            AddImageCommand = new RelayCommand(AddImageAction, () => CanCurrentExplorerItemRelevantDo() && IsServiceIdle());
            AddFolderCommand = new RelayCommand(AddFolderAction, () => CanCurrentExplorerItemRelevantDo() && IsServiceIdle());
            RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction, (f) => CanCurrentExplorerItemRelevantDo() && SelectedFileInfo != null && IsServiceIdle());
            DownloadCommand = new RelayCommand<IFileInfo>(DownloadActionAsync, (f) => CanCurrentExplorerItemRelevantDo() && SelectedFileInfo != null && IsServiceIdle());
            ToggleDetailCommand = new RelayCommand(ToggleDetailAction);
            ToggleTreeCommand = new RelayCommand(ToggleTreeAction);


            //init data

            KeyWord = "";
            PathStack = new ObservableCollection<string>();
            IsShowDetail = false;
            IsShowTree = true;
            InitData();

        }

        private void _qiniuManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_qiniuManager.IsBusy))
            {
                if (App.Window == null)
                {
                    return;
                }
                App.Window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
               {

                   RefreshCommand.NotifyCanExecuteChanged();
                   SearchCommand.NotifyCanExecuteChanged();
                   AddImageCommand.NotifyCanExecuteChanged();
                   AddFolderCommand.NotifyCanExecuteChanged();
                   RemoveImageCommand.NotifyCanExecuteChanged();
                   DownloadCommand.NotifyCanExecuteChanged();

               });

            }
        }

        private async void DownloadActionAsync(IFileInfo obj)
        {
            if (obj == null || obj.Type == FileInfoType.Folder)
            {
                return;
            }
            var currentDomainsResult = await _qiniuManager.SetCurrentDomain(_qiniuManager.Bucket);

            var extenstion = Path.GetExtension(obj.FileName);
            var fileName = Path.GetFileNameWithoutExtension(obj.FileName);

            var picker = new FileSavePicker();
            picker.DefaultFileExtension = extenstion;
            picker.FileTypeChoices.Add(obj.FileType, new List<string>() { extenstion });
            picker.SuggestedFileName = fileName;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            UIHelper.InitFileOpenPicker(picker);

            var files = await picker.PickSaveFileAsync();
            if (files != null)
            {
                _qiniuManager.DownLoad(new List<QiniuFile>() { obj as QiniuFile }, files.Path);

            }

        }

        private void SettingsPageViewModel_OnReload(object sender, EventArgs e)
        {
            this.ReloadAction();
        }

        private async void AddFolderAction()
        {
            var folderName = "";
            ContentDialog contentDialog = null;
            var createFolderPagePage = new CreateFolderPage();
            createFolderPagePage.OnSubmit += (sender, e) =>
            {

                var currentName = (sender as CreateFolderPage).CurrentName;
                if (!string.IsNullOrEmpty(currentName))
                {
                    folderName = currentName;
                    //没有类似close()的方法可供调用
                    //contentDialog.Close();
                }
            };
            contentDialog = new ContentDialog
            {
                Title = "请键入文件夹名称",
                Content = createFolderPagePage,
                PrimaryButtonText = "确定"
            };


            contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            await contentDialog.ShowAsync();
            if (string.IsNullOrEmpty(folderName))
            {
                await UIHelper.ShowAsync("不能创建名称为空的文件夹哦");
            }

            var file = "README.txt";
            var filename = CurrentExplorerItem.Path + ExplorerItem.SpliterChar + folderName + ExplorerItem.SpliterChar + file;
            var fileType = _mimeTypeManager.GetMimeType(file);

            var localfile = new LocalFile()
            {
                FileName = filename,
                FileSize = QiniuHelper.GetFileSize(0),
                Path = file,
                FileType = fileType,
                Type = FileInfoType.File,
            };
            localfile.SetFolderType();
            CurrentFileInfos.Insert(0, localfile);
        }

        private bool CanCurrentExplorerItemRelevantDo()
        {
            return CurrentExplorerItem != null;
        }

        private bool IsServiceIdle()
        {
            return !_qiniuManager.IsBusy;

        }

        private void SettingsPageViewModel_OnSubmit(object sender, EventArgs e)
        {
            ReloadAction();
        }

        private void ToggleTreeAction()
        {
            IsShowTree = !IsShowTree;
        }

        private void ToggleDetailAction()
        {
            IsShowDetail = !IsShowDetail;
        }

        private void ReloadAction()
        {
            ClearData();
            InitData();
        }

        private void ClearData()
        {
            ClearStack();
            RootExplorerItems?.Clear();
            CurrentFileInfos = null;
            CurrentExplorerItem = null;
            SelectedFileInfo = null;
        }

        private async void RefreshAction()
        {
            var targetPath = EnsureOriginUrl(CurrentExplorerItem.Path);
            targetPath += ExplorerItem.SpliterChar;
            var subRoot = await GenerateExplorerRoot(CurrentExplorerItem.Name, targetPath);
            if (subRoot != null && subRoot.Name == CurrentExplorerItem.Name)
            {

                var subRootChildren = subRoot.Children;
                CurrentExplorerItem.Children.Clear();
                foreach (var subRootChild in subRootChildren)
                {
                    subRootChild.Path = CurrentExplorerItem.Path.Substring(0, CurrentExplorerItem.Path.LastIndexOf(ExplorerItem.SpliterChar) + 1) + subRootChild.Path;

                    CurrentExplorerItem.Children.Add(subRootChild);
                }
            }

            await RefreshCurrentFileInfosAsync();


        }

        private async void MenuPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentExplorerItem))
            {
                PathStack.Clear();

                if (CurrentExplorerItem == null || string.IsNullOrEmpty(CurrentExplorerItem.Path))
                {
                    return;
                }

                foreach (var item in CurrentExplorerItem.PathStack)
                {
                    PathStack.Add(item);
                }

                await RefreshCurrentFileInfosAsync();

                RemoveImageCommand.NotifyCanExecuteChanged();
                AddImageCommand.NotifyCanExecuteChanged();
                AddFolderCommand.NotifyCanExecuteChanged();
                SearchCommand.NotifyCanExecuteChanged();

                SelectedFileInfo = null;
            }

        }

        private async Task RefreshCurrentFileInfosAsync(string keyword = "")
        {

            if (CurrentExplorerItem == null)
            {
                return;
            }
            var targetPath = CurrentExplorerItem.Path + ExplorerItem.SpliterChar + keyword;
            targetPath = EnsureOriginUrl(targetPath);

            var targetDirectoryPath = Path.GetDirectoryName(targetPath + ExplorerItem.SpliterChar);
            if (targetDirectoryPath == null)
            {
                targetDirectoryPath = string.Empty;
            }
            var currentFileInfos = new ObservableCollectionEx<IFileInfo>();
            foreach (var childItem in CurrentExplorerItem.Children)
            {
                var newModule = new QiniuFile();
                newModule.FileName = childItem.Name;
                newModule.SetFolderType();
                currentFileInfos.Add(newModule);

            }

            var fileInfos = await _qiniuManager.Search(_qiniuManager.Bucket, targetPath);
            var currentFileInfoList = fileInfos.Where(c =>
            {
                return Path.GetDirectoryName(c.FileName) == targetDirectoryPath && c.Type == FileInfoType.File;
            }).OrderBy(c => c.Type);

            foreach (var item in currentFileInfoList)
            {
                currentFileInfos.Add(item);
            }

            currentFileInfos.CollectionChanged += FileInfos_CollectionChangedAsync;
            CurrentFileInfos = currentFileInfos;

        }

        private string EnsureOriginUrl(string targetPath)
        {
            if (targetPath.StartsWith(_rootname))
            {
                targetPath = targetPath.Substring(_rootname.Length, targetPath.Length - _rootname.Length);
            }

            if (targetPath.StartsWith(ExplorerItem.SpliterChar))
            {
                targetPath = targetPath.Substring(1, targetPath.Length - 1);
            }

            return targetPath;
        }

        private async void SearchAction(string keyword)
        {
            await RefreshCurrentFileInfosAsync(keyword);
        }


        private async void InitData()
        {
            var storageSK = ConfigureProvider.SettingInfo.StorageAppSecret;
            if (!await Bucket(storageSK))
            {
                return;
            };
            _rootname = _qiniuManager.Bucket;
            var root = await GenerateExplorerRoot(_qiniuManager.Bucket);

            RootExplorerItems = new ObservableCollectionEx<IExplorerItem>() { root };

            NavigationHistoryStack.Add(root);
            NavigationStack.Add(root);

            CurrentExplorerItem = RootExplorerItems.FirstOrDefault();
        }

        private async Task<IExplorerItem> GenerateExplorerRoot(string rootName, string rootPath = "")
        {
            var fgalleryList = await _qiniuManager.Search(_qiniuManager.Bucket, rootPath);
            var folders = fgalleryList.Where(c => c.Type == FileInfoType.Folder).GroupBy(c => c.FileName).Select(c =>
            {
                string result = string.IsNullOrEmpty(rootPath)
                    ? c.Key
                    : c.Key.StartsWith(rootPath) ? c.Key.Substring(rootPath.Length, c.Key.Length - rootPath.Length) : c.Key;
                return result;
            }).ToList();
            if (string.IsNullOrEmpty(rootPath))
            {
                folders.Add("");

            }
            var vfolders = fgalleryList
                .Where(c => c.Type == FileInfoType.File)
                .Select(c =>
                {
                    var fileName = Path.GetFileName(c.FileName);
                    var folderName = c.FileName.Substring(0, c.FileName.Length - fileName.Length);
                    string result = string.IsNullOrEmpty(rootPath)
                    ? folderName
                    : folderName.StartsWith(rootPath) ? folderName.Substring(rootPath.Length, folderName.Length - rootPath.Length) : folderName;
                    return result;
                })
                .Distinct()
                .ToList();

            foreach (var vfolder in vfolders)
            {
                if (!folders.Contains(vfolder))
                {
                    folders.Add(vfolder);
                }
            }

            IExplorerItem root = null;
            foreach (var folder in folders)
            {
                var trimdFolder = rootName + ExplorerItem.SpliterChar + folder;
                if (trimdFolder.EndsWith(ExplorerItem.SpliterChar))
                {
                    trimdFolder = trimdFolder.Substring(0, trimdFolder.Length - 1);
                }
                var pathArray = trimdFolder.Split(ExplorerItem.SpliterChar);

                void b(ref IExplorerItem current, int index)
                {
                    if (index == pathArray.Length - 1)
                    {
                        return;
                    }

                    var name = pathArray[index + 1];

                    var next = current.Children.FirstOrDefault(c => c.Name == name);
                    if (next == null)
                    {
                        var path = string.Join(ExplorerItem.SpliterChar, pathArray);

                        var currentExplorerItem = new ExplorerItem()
                        {
                            Name = pathArray[pathArray.Length - 1],
                            Type = ExplorerItemType.Folder,
                            Path = path
                        };
                        var appendItem = a(currentExplorerItem, pathArray.Length - 2, index);
                        current.Children.Add(appendItem);
                        return;
                    }
                    b(ref next, index + 1);

                }

                IExplorerItem a(IExplorerItem ex, int index, int stopIndex = 0)
                {
                    if (index == stopIndex)
                    {
                        return ex;
                    }
                    var children = new ObservableCollectionEx<IExplorerItem>();
                    children.Add(ex);
                    var path = string.Join(ExplorerItem.SpliterChar, pathArray, 0, index + 1);

                    var e = new ExplorerItem()
                    {
                        Name = pathArray[index],
                        Path = path,
                        Type = ExplorerItemType.Folder,
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
                            Type = ExplorerItemType.Folder,
                            Path = pathArray[0]
                        };
                    }
                    b(ref root, 0);

                }
            }

            return root;
        }

        private async void FileInfos_CollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
        {
            var currentDomainsResult = await _qiniuManager.SetCurrentDomain(_qiniuManager.Bucket);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var currentItem = e.NewItems[0] as IFileInfo;

                    if (e.NewItems[0] is not LocalFile)
                    {
                        await UIHelper.ShowAsync("上传过程中出现错误");
                        return;
                    }
                    var item = e.NewItems[0] as IFileInfo;
                    var callbackBody = ConfigureProvider.SettingInfo.CallbackBody;
                    var callbackUrl = ConfigureProvider.SettingInfo.CallbackUrl;

                    var filename = EnsureOriginUrl(item.FileName);
                    bool uploadResult;

                    uploadResult = await _qiniuManager.UploadSingle(item.Path, filename, callbackUrl, callbackBody);

                    if (uploadResult)
                    {
                        RefreshAction();
                    }
                    else
                    {
                        await UIHelper.ShowAsync("上传过程中出现错误");
                        await RefreshCurrentFileInfosAsync();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var deleteResult = await _qiniuManager.Delete(new List<Model.Qiniu.QiniuFile>() { e.OldItems[0] as Model.Qiniu.QiniuFile });
                    if (deleteResult)
                    {
                        SelectedFileInfo = null;
                        RefreshAction();
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
            var current = CurrentFileInfos.First(c => c.FileName == obj.FileName);
            if (current != null)
            {
                CurrentFileInfos.RemoveAt(CurrentFileInfos.IndexOf(current));
            }

        }

        private async void AddImageAction()
        {
            var open = new FileOpenPicker();
            open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            var extensionAvailable = ConfigureProvider.SettingInfo.ExtensionAvailable;
            foreach (var item in extensionAvailable.Split('|'))
            {
                try
                {
                    open.FileTypeFilter.Add(item);

                }
                catch (Exception e)
                {
                    await UIHelper.ShowAsync("所填写的扩展名不正确：" + item);
                    return;

                }
            }

            UIHelper.InitFileOpenPicker(open);

            var files = await open.PickMultipleFilesAsync();
            if (files.Count == 0)
            {
                return;
            }
            var file = files[0];

            var basicProp = await file.GetBasicPropertiesAsync();
            var filename = CurrentExplorerItem.Path + ExplorerItem.SpliterChar + file.Name;
            var fileType = _mimeTypeManager.GetMimeType(file.FileType);
            var localfile = new LocalFile()
            {
                FileName = filename,
                FileType = fileType,
                FileSize = QiniuHelper.GetFileSize((long)basicProp.Size),
                CreateDate = basicProp.ItemDate.ToString("yyyy/MM/dd HH:mm:ss"),
                Path = file.Path,

            };
            if (string.IsNullOrEmpty(localfile.FileType))
            {
                localfile.SetFolderType();
            }
            CurrentFileInfos.Insert(0, localfile);
        }


        private async Task<bool> Bucket(string storageSK)
        {
            var currentArea = ConfigureProvider.SettingInfo.StorageRegion;
            var bucketList = await _qiniuManager.ConnectServer(ConfigureProvider.SettingInfo.StorageAppKey, storageSK, currentArea.Value);
            if (bucketList.Count == 0)
            {
                await UIHelper.ShowAsync("没有找到Bucket列表，请填写正确的AppKey和AppSecret");
                return false;
            }
            else if (bucketList.Count == 1)
            {
                _qiniuManager.Bucket = bucketList.First();

            }
            else
            {
                ContentDialog contentDialog = null;
                var bucketListPage = new BucketListPage(bucketList);
                bucketListPage.OnSubmit += (sender, e) =>
                {

                    var currentBucket = (sender as BucketListPage).CurrentBucket;
                    if (!string.IsNullOrEmpty(currentBucket))
                    {
                        _qiniuManager.Bucket = currentBucket;
                        contentDialog.IsPrimaryButtonEnabled = true;

                        //没有类似close()的方法可供调用
                        //contentDialog.Close();
                    }
                };
                contentDialog = new ContentDialog
                {
                    Title = "选择一个Bucket",
                    Content = bucketListPage,
                    IsPrimaryButtonEnabled = false,
                    PrimaryButtonText = "继续"
                };
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
                await contentDialog.ShowAsync();
            }
            return true;
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



        private ObservableCollection<string> _pathStack;

        public ObservableCollection<string> PathStack
        {
            get { return _pathStack; }
            set
            {
                _pathStack = value;
                OnPropertyChanged(nameof(PathStack));

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
