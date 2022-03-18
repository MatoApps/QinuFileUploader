using Qiniu.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using Workshop.Model.Qiniu;

namespace Workshop.Service.Manager
{
    public interface IQiniuManager
    {
        string Bucket { get; set; }
        List<string> BucketList { get; set; }
        DomainsResult CurrentDomain { get; set; }
        List<QiNiuFileInfo> FileInfos { get; set; }
        bool IsBusy { get; set; }

        Task<List<string>> ConnectServer(string TxtAK, string TxtSK, Zone zone);
        Task<bool> Delete(List<QiNiuFileInfo> list);
        void DownLoad(List<QiNiuFileInfo> list, string fileSaveDir);
        Task<bool> EditDeleteAfterDays(List<QiNiuFileInfo> list, int deleteAfterDays);
        string GetPreviewAddress(List<QiNiuFileInfo> list);
        IEnumerable<QiniuArea> GetQiniuAreas();
        Task<bool> RefreshNetAddress(List<QiNiuFileInfo> list);
        void Rename(List<QiNiuFileInfo> list, string txtRename);
        Task<List<QiNiuFileInfo>> Search(string bucket, string keyword);
        Task<DomainsResult> SetCurrentDomain(string bucket);
        Task<bool> Upload(string[] fileUploadFiles, string callbackBody, bool overlay = true);
        Task<bool> UploadSingle(string fileUploadFile, string key, string callbackBody, bool overlay = true);
    }
}