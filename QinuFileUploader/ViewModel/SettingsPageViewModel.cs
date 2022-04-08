using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QinuFileUploader.Common;
using QinuFileUploader.Helper;
using QinuFileUploader.Model.Qiniu;
using QinuFileUploader.Model;

namespace QinuFileUploader.ViewModel
{
    public class SettingsPageViewModel : ObservableObject
    {
        public event EventHandler<EventArgs> OnSubmit;
        public event EventHandler<EventArgs> OnReload;
        public SettingsPageViewModel()
        {
            SubmitCommand = new RelayCommand(SubmitAction, CanSubmit);
            ReloadCommand = new RelayCommand(ReloadAction);
            PropertyChanged += SettingPageViewModel_PropertyChanged;
            SettingInfo = new SettingInfo();
            SettingInfo.StorageAppSecret = ConfigureProvider.SettingInfo.StorageAppSecret;
            SettingInfo.StorageAppKey = ConfigureProvider.SettingInfo.StorageAppKey;
            SettingInfo.CallbackUrl = ConfigureProvider.SettingInfo.CallbackUrl;
            SettingInfo.CallbackBody = ConfigureProvider.SettingInfo.CallbackBody;
            SettingInfo.PropertyChanged += SettingInfo_PropertyChanged;

            BucketRegionSource = QiniuRegion.GetRegionList().ToList();
            SettingInfo.StorageRegion = BucketRegionSource.First(c => c.Title == ConfigureProvider.SettingInfo.StorageRegion.Title);

        }

        private void ReloadAction()
        {
            OnReload?.Invoke(this, new EventArgs());
        }

        private bool _hasChanged;

        public bool HasChanged
        {
            get { return _hasChanged; }
            set
            {
                _hasChanged = value;
                OnPropertyChanged(nameof(HasChanged));
            }
        }


        private void SettingInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            HasChanged = true;
        }


        private void SettingPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingInfo) && SettingInfo != null)
            {

            }
            else if (e.PropertyName == nameof(HasChanged))
            {
                SubmitCommand.NotifyCanExecuteChanged();

            }
        }



        private void SubmitAction()
        {
            ConfigureProvider.SettingInfo = SettingInfo;
            LocalDataHelper.SaveObjectLocal(SettingInfo);
            HasChanged = false;

            OnSubmit?.Invoke(this, new EventArgs());

        }

        private bool CanSubmit()
        {
            return SettingInfo != null && HasChanged;
        }

        private SettingInfo _settingInfo;

        public SettingInfo SettingInfo
        {
            get { return _settingInfo; }
            set
            {
                _settingInfo = value;
                OnPropertyChanged(nameof(SettingInfo));
            }
        }

        private List<QiniuRegion> _bucketRegionSource;

        public List<QiniuRegion> BucketRegionSource
        {
            get { return _bucketRegionSource; }
            set
            {
                _bucketRegionSource = value;
                OnPropertyChanged(nameof(BucketRegionSource));
            }
        }


        public RelayCommand SubmitCommand { get; set; }
        public RelayCommand ReloadCommand { get; set; }

    }
}
