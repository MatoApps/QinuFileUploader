﻿using CommunityToolkit.Mvvm.ComponentModel;
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

namespace QinuFileUploader.ViewModel
{
    public class MenuPageViewModel : ObservableObject
    {
        private readonly IQiniuManager _qiniuManager;
        private readonly IMimeTypeManager _mimeTypeManager;

        private string _rootname;
        public RelayCommand<string> SearchCommand { get; private set; }
        public RelayCommand NavigationHistoryBackCommand { get; private set; }
        public RelayCommand NavigationHistoryForwardCommand { get; private set; }
        public RelayCommand NavigationBackCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand AddImageCommand { get; }
        public RelayCommand AddFolderCommand { get; }
        public RelayCommand ToggleDetailCommand { get; }
        public RelayCommand ToggleTreeCommand { get; }
        public RelayCommand<IFileInfo> RemoveImageCommand { get; private set; }

        public MenuPageViewModel(IQiniuManager qiniuManager, IMimeTypeManager mimeTypeManager, SettingsPageViewModel settingsPageViewModel)
        {
            _qiniuManager = qiniuManager;
            _mimeTypeManager = mimeTypeManager;

            settingsPageViewModel.OnSubmit += SettingsPageViewModel_OnSubmit;

            PropertyChanged += MenuPageViewModel_PropertyChanged;

            //init commands
            RefreshCommand = new RelayCommand(RefreshAction);
            NavigationHistoryBackCommand = new RelayCommand(NavigationHistoryBack);
            NavigationHistoryForwardCommand = new RelayCommand(NavigationHistoryForward);
            NavigationBackCommand = new RelayCommand(NavigationBack);

            SearchCommand = new RelayCommand<string>(SearchAction, (s) => CanCurrentExplorerItemRelevantDo());
            AddImageCommand = new RelayCommand(AddImageAction, () => CanCurrentExplorerItemRelevantDo());
            AddFolderCommand = new RelayCommand(AddFolderAction, () => CanCurrentExplorerItemRelevantDo());
            RemoveImageCommand = new RelayCommand<IFileInfo>(RemoveImageAction, (f) => CanCurrentExplorerItemRelevantDo() && SelectedFileInfo != null);

            ToggleDetailCommand = new RelayCommand(ToggleDetailAction);
            ToggleTreeCommand = new RelayCommand(ToggleTreeAction);


            //init data
            NavigationStack = new ObservableCollectionEx<ExplorerItem>();
            NavigationHistoryStack = new ObservableCollectionEx<ExplorerItem>();
            KeyWord = "";

            PathStack = new ObservableCollection<string>();
            IsShowDetail = false;
            IsShowTree = true;
            InitData();

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
                PrimaryButtonText = "继续"
            };
            contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            await contentDialog.ShowAsync();
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

        private void SettingsPageViewModel_OnSubmit(object sender, EventArgs e)
        {
            NavigationStack.Clear();
            NavigationHistoryStack.Clear();
            RootExplorerItems?.Clear();
            CurrentFileInfos = null;
            CurrentExplorerItem = null;
            SelectedFileInfo = null;
            InitData();
        }

        private void ToggleTreeAction()
        {
            IsShowTree = !IsShowTree;
        }

        private void ToggleDetailAction()
        {
            IsShowDetail = !IsShowDetail;
        }

        private void RefreshAction()
        {
            RootExplorerItems?.Clear();
            CurrentExplorerItem = null;
            SelectedFileInfo = null;
            InitData();
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

            var fileInfos = await _qiniuManager.Search(_qiniuManager.Bucket, targetPath);
            var currentFileInfos = new ObservableCollectionEx<IFileInfo>(fileInfos.Where(c =>
            {
                var result = false;
                if (c.Type == FileInfoType.Folder)
                {
                    if (c.FileName.EndsWith('/') && c.FileName.Length > 1)
                    {
                        var fileName = c.FileName.Substring(0, c.FileName.Length - 1);
                        result = Path.GetDirectoryName(fileName) == targetDirectoryPath;
                    }
                }
                else
                {
                    result = Path.GetDirectoryName(c.FileName) == targetDirectoryPath;
                }
                return result;
            }).OrderBy(c => c.Type));
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


        private async void InitData(string keyword = "")
        {
            var storageSK = ConfigureProvider.SettingInfo.StorageAppSecret;
            if (!await Bucket(storageSK))
            {
                return;
            };
            _rootname = _qiniuManager.Bucket;
            var fgalleryList = await _qiniuManager.Search(_qiniuManager.Bucket, keyword);

            var folders = fgalleryList.Where(c => c.Type == FileInfoType.Folder).GroupBy(c => c.FileName).Select(c => c.Key).ToList();
            folders.Add("");

            var vfolders = fgalleryList
                .Where(c => c.Type == FileInfoType.File)
                .Select(c =>
                {
                    var fileName = Path.GetFileName(c.FileName);
                    var folderName = c.FileName.Substring(0, c.FileName.Length - fileName.Length);
                    return folderName;
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

            ExplorerItem root = null;
            foreach (var folder in folders)
            {
                var trimdFolder = _rootname + ExplorerItem.SpliterChar + folder;
                if (trimdFolder.EndsWith(ExplorerItem.SpliterChar))
                {
                    trimdFolder = trimdFolder.Substring(0, trimdFolder.Length - 1);
                }
                var pathArray = trimdFolder.Split(ExplorerItem.SpliterChar);

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
                        var path = string.Join(ExplorerItem.SpliterChar, pathArray);

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
                    var path = string.Join(ExplorerItem.SpliterChar, pathArray, 0, index + 1);

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

            RootExplorerItems = new ObservableCollectionEx<ExplorerItem>() { root };

            NavigationHistoryStack.Add(root);
            NavigationStack.Add(root);

            CurrentExplorerItem = RootExplorerItems.FirstOrDefault();
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
                        await RefreshCurrentFileInfosAsync();

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
                        await RefreshCurrentFileInfosAsync();

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




        private void NavigationBack()
        {
            if (NavigationStack.Count == 1)
            {
                return;
            }
            NavigationStack.Pop();
            var lastItem = NavigationStack.LastOrDefault();
            if (lastItem == null)
            {
                return;
            }
            if (ToFolder(lastItem))
            {

                NavigationHistoryStack.ForEach((element) =>
                {
                    element.IsCurrent = false;
                });
                lastItem.IsCurrent = true;

                PushNavigationHistoryStack(lastItem);
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

            if (NavigationHistoryStack.Count > 10)
            {
                NavigationHistoryStack.Pop();
            }
            NavigationHistoryStack.Unshift(newItem);
        }

        private void DealWithNavigationStack(ExplorerItem folder)
        {

            NavigationStack.Clear();
            var paths = folder.Path.Split(ExplorerItem.SpliterChar);


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
                NavigationStack.Push(currentExplorerItem);
                a(currentExplorerItem.Children, index + 1);
            }
            a(RootExplorerItems, 0);
        }

        public void NavigationTo(ExplorerItem folder)
        {
            DealWithNavigationStack(folder);
            if (ToFolder(folder))
            {
                NavigationHistoryStack.ForEach((element) =>
                {
                    element.IsCurrent = false;
                });
                folder.IsCurrent = true;
                PushNavigationHistoryStack(folder);
            }
        }

        private void NavigationHistoryBack()
        {
            var currentIndex = NavigationHistoryStack.IndexOf(
              (c) => c.IsCurrent
            );
            if (currentIndex < NavigationHistoryStack.Count - 1)
            {
                var forwardIndex = currentIndex + 1;

                var folder = NavigationHistoryStack[forwardIndex];
                DealWithNavigationStack(folder);

                if (ToFolder(folder))
                {
                    NavigationHistoryStack.ForEach((element) =>
                    {
                        element.IsCurrent = false;
                    });
                    NavigationHistoryStack[forwardIndex].IsCurrent = true;
                }
            }
        }

        private void NavigationHistoryForward()
        {

            var currentIndex = NavigationHistoryStack.IndexOf(
              (c) => c.IsCurrent
            );
            if (currentIndex > 0)
            {
                var forwardIndex = currentIndex - 1;

                var folder = NavigationHistoryStack[forwardIndex];
                DealWithNavigationStack(folder);

                if (ToFolder(folder))
                {
                    NavigationHistoryStack.ForEach((element) =>
                    {
                        element.IsCurrent = false;
                    });
                    NavigationHistoryStack[forwardIndex].IsCurrent = true;
                }
            }
        }

        private bool ToFolder(ExplorerItem item)
        {
            if (item == null || item.Path == CurrentExplorerItem.Path)
            {
                return false;
            }
            //var paths = item.Path.Split(ExplorerItem.SpliterChar);

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

            var currentExplorerItem = NavigationStack.FirstOrDefault(c => c.Path == item.Path);

            if (currentExplorerItem == null)
            {
                return false;
            }

            CurrentExplorerItem = currentExplorerItem;
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
