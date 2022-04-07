using QinuFileUploader.Helper;
using QinuFileUploader.Model;
using QinuFileUploader.Model.Qiniu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinuFileUploader.Common
{
    public static class ConfigureProvider
    {
        public const string DefaultStorageAppKey = "[请填写AppKey]";
        public const string DefaultStorageAppSecret = "[请填写AppSecret]";
        public const string DefaultCallbackBody = "key=$(key)&hash=$(etag)&bucket=$(bucket)&fsize=$(fsize)";
        public const string DefaultCallbackUrl = "";


        public static SettingInfo SettingInfo;

        static ConfigureProvider()
        {
            var settingInfo = LocalDataHelper.ReadObjectLocal<SettingInfo>();
            if (settingInfo == null)
            {
                settingInfo = new SettingInfo()
                {
                    StorageAppKey = DefaultStorageAppKey,
                    StorageAppSecret = DefaultStorageAppSecret,
                    StorageRegion = QiniuRegion.GetRegionList().First(c => c.Title == "华南"),
                    CallbackBody = DefaultCallbackBody,
                    CallbackUrl = DefaultCallbackUrl
                };
                LocalDataHelper.SaveObjectLocal(settingInfo);

            }
            SettingInfo = settingInfo;
        }
    }
}
