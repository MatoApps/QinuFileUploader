using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QinuFileUploader.Model.Qiniu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QinuFileUploader.Model
{
    public class SettingInfo : ObservableObject
    {
        private string _storageAppKey;
        [JsonProperty("StorageAppKey")]

        public string StorageAppKey
        {
            get { return _storageAppKey; }
            set
            {
                _storageAppKey = value;
                OnPropertyChanged(nameof(StorageAppKey));
            }
        }

        private string _storageAppSecret;
        [JsonProperty("StorageAppSecret")]

        public string StorageAppSecret
        {
            get { return _storageAppSecret; }
            set
            {
                _storageAppSecret = value;
                OnPropertyChanged(nameof(StorageAppSecret));

            }
        }


        private QiniuRegion _storageRegion;

        [JsonProperty("StorageRegion")]
        public QiniuRegion StorageRegion
        {
            get { return _storageRegion; }
            set
            {
                _storageRegion = value;
                OnPropertyChanged(nameof(StorageRegion));

            }
        }

        private string _callbackUrl;

        [JsonProperty("CallbackUrl")]
        public string CallbackUrl
        {
            get { return _callbackUrl; }
            set
            {
                _callbackUrl = value;

                OnPropertyChanged(nameof(CallbackUrl));
            }
        }

        private string _callbackBody;

        [JsonProperty("CallbackBody")]
        public string CallbackBody
        {
            get { return _callbackBody; }
            set
            {
                _callbackBody = value;

                OnPropertyChanged(nameof(CallbackBody));
            }
        }


    }
}
