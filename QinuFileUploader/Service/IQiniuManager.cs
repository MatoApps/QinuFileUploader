using Qiniu.Storage;
using QinuFileUploader.Model.Qiniu;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QinuFileUploader.Service
{
    public interface IQiniuManager
    {
        string Bucket { get; set; }
        List<string> BucketList { get; set; }
        DomainsResult CurrentDomain { get; set; }
        List<QiniuFile> FileInfos { get; set; }
        bool IsBusy { get; set; }

        Task<List<string>> ConnectServer(string StorageAppKey, string DefaultStorageAppSecret, Zone zone);
        Task<bool> Delete(List<QiniuFile> list);
        void DownLoad(List<QiniuFile> list, string fileSaveDir);
        Task<bool> EditDeleteAfterDays(List<QiniuFile> list, int deleteAfterDays);
        string GetPreviewAddress(List<QiniuFile> list);
        IEnumerable<QiniuRegion> QiniuRegions();
        Task<bool> RefreshNetAddress(List<QiniuFile> list);
        void Rename(List<QiniuFile> list, string txtRename);
        Task<List<QiniuFile>> Search(string bucket, string keyword);
        Task<DomainsResult> SetCurrentDomain(string bucket);
        Task<bool> Upload(string[] fileUploadFiles, string callbackUrl, string callbackBody, bool overlay = true);
        Task<bool> UploadSingle(string fileUploadFile, string key, string callbackUrl, string callbackBody, bool overlay = true);
    }
}