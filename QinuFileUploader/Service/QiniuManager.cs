
using Newtonsoft.Json;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Util;
using QinuFileUploader.Common;
using QinuFileUploader.Helper;
using QinuFileUploader.Model;
using QinuFileUploader.Model.Qiniu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QinuFileUploader.Service
{
    public class QiniuManager : IQiniuManager
    {
        private Mac mac;
        private Config config;
        private BucketManager bucketManager;
        private string marker;

        private DomainsResult _currentDomain;

        public DomainsResult CurrentDomain
        {
            get { return _currentDomain; }
            set { _currentDomain = value; }
        }



        public bool IsBusy { get; set; }
        private List<string> _bucketList;

        public List<string> BucketList
        {
            get { return _bucketList; }
            set { _bucketList = value; }
        }

        private string _bucket;

        public string Bucket
        {
            get { return _bucket; }
            set { _bucket = value; }
        }

        private List<QiniuFile> _fileInfos;

        public List<QiniuFile> FileInfos
        {
            get { return _fileInfos; }
            set { _fileInfos = value; }
        }



        public QiniuManager()
        {
            BucketList = new List<string>();
            FileInfos = new List<QiniuFile>();
        }

        public IEnumerable<QiniuRegion> QiniuRegions()
        {
            return QiniuRegion.GetRegionList();
        }

        public async Task<List<string>> ConnectServer(string StorageAppKey, string DefaultStorageAppSecret, Zone zone)
        {

            if (string.IsNullOrWhiteSpace(StorageAppKey) || string.IsNullOrWhiteSpace(DefaultStorageAppSecret))
            {
                return null;
            }

            if (BucketList.Count > 0)
            {
                BucketList.Clear();
            }

            Config.DefaultRsHost = "rs.qiniu.com";

            mac = new Mac(StorageAppKey, DefaultStorageAppSecret);
            config = new Config { Zone = Zone.ZONE_CN_East };
            if (zone != null)
            {
                config.Zone = zone;
            }
            bucketManager = new BucketManager(mac, config);



            BucketList.Clear();
            IsBusy = true;

            return await Task.Run(() =>
            {
                BucketsResult bucketsResult = bucketManager.Buckets(true);
                if (bucketsResult.Code == 200)
                {
                    List<string> buckets = bucketsResult.Result;
                    BucketList = buckets;
                    IsBusy = false;
                }
                else
                {

                    IsBusy = false;
                }
                Thread.Sleep(10);
                return BucketList;
            });
        }

        public async Task<List<QiniuFile>> Search(string bucket, string keyword)
        {
            if (IsBusy == true)
            {
                return null;
            }

            List<QiniuFile> qiNiuFileInfoList = new List<QiniuFile>(); ;

            var startWith = keyword.Trim();
            return await Task.Run(() =>
            {
                ListResult listResult = bucketManager.ListFiles(bucket, startWith, marker, 5000, "");

                if (listResult != null && listResult.Result != null && listResult.Result.Marker != null)
                {
                    marker = listResult.Result.Marker;
                }
                else
                {

                    marker = string.Empty;
                }
                if (listResult?.Result?.Items != null)
                {

                    foreach (ListItem item in listResult.Result.Items)
                    {
                        // item.EndUser
                        QiniuFile f = new QiniuFile
                        {

                            FileName = item.Key,
                            FileType = item.MimeType,
                            StorageType = QiniuHelper.GetStorageType(item.FileType),
                            FileSize = QiniuHelper.GetFileSize(item.Fsize),
                            EndUser = item.EndUser,
                            CreateDate = QiniuHelper.GetDataTime(item.PutTime),
                            Type = item.MimeType == QiniuFile.QiniuFolderType ? FileInfoType.Folder : FileInfoType.File
                        };
                        qiNiuFileInfoList.Add(f);

                    }

                    if (qiNiuFileInfoList.Count > 0)
                    {
                        qiNiuFileInfoList = qiNiuFileInfoList.OrderByDescending(t => t.CreateDate).ToList();
                        FileInfos = qiNiuFileInfoList;
                    }
                    else
                    {
                        FileInfos = new List<QiniuFile>();
                    }
                }
                else
                {
                    LogHelper.LogError("未能加载数据:" + listResult.Text);
                }
                IsBusy = false;

                return FileInfos;
            });
        }

        public async Task<DomainsResult> SetCurrentDomain(string bucket)
        {
            marker = "";
            return await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(bucket))
                {
                    CurrentDomain = bucketManager.Domains(bucket);
                    return CurrentDomain;
                }
                else
                {
                    return null;
                }

            });
        }


        public void DownLoad(List<QiniuFile> list, string fileSaveDir)
        {
            if (IsBusy == true)
            {
                return;
            }
            if (list.Count > 0)
            {
                IsBusy = true;
                ThreadPool.QueueUserWorkItem(async state =>
                {
                    if (CurrentDomain.Result.Count > 0)
                    {

                        string domain = CurrentDomain.Result[0];

                        domain = config.UseHttps ? "https://" + domain : "http://" + domain;

                        var rresult = new StringBuilder();

                        foreach (QiniuFile info in list)
                        {
                            string pubfile = GetPublishUrl(info.FileName);
                            if (string.IsNullOrWhiteSpace(pubfile))
                            {
                                return;
                            }

                            string saveFile = Path.Combine(fileSaveDir, info.FileName.Replace('/', '-'));
                            if (File.Exists(saveFile))
                            {
                                saveFile = Path.Combine(fileSaveDir,
                                    Path.GetFileNameWithoutExtension(info.FileName.Replace('/', '-')) + Guid.NewGuid() +
                                    Path.GetExtension(info.FileName));
                            }
                            HttpResult result = DownloadManager.Download(pubfile, saveFile);
                            if (result.Code != 200)
                            {
                                result = DownloadManager.Download(
                                    DownloadManager.CreatePrivateUrl(mac, domain, info.FileName, 3600), saveFile);
                                if (result.Code != 200)
                                {
                                    rresult.AppendLine(info.FileName + ":下载失败！");
                                    return;
                                }
                            }

                        }
                        await UIHelper.ShowAsync(string.IsNullOrWhiteSpace(rresult.ToString()) ? "下载结束！" : rresult.ToString());
                    }
                    else
                    {
                        LogHelper.LogError("无法获得空间的域名！");
                    }
                });
                Thread.Sleep(10);
            }

            IsBusy = false;
        }


        public async Task<bool> Delete(List<QiniuFile> list)
        {
            return await Task.Run(() =>
            {
                if (list.Count > 0)
                {
                    List<string> ops = new List<string>();
                    foreach (var key in list)
                    {
                        string op = bucketManager.DeleteOp(Bucket, key.FileName);
                        ops.Add(op);
                    }

                    BatchResult ret = bucketManager.Batch(ops);

                    StringBuilder sb = new StringBuilder();

                    if (ret.Code / 100 != 2)
                    {
                        return false;
                    }
                    Thread.Sleep(10);

                    return true;
                }
                else
                {
                    return false;
                }
            });
        }


        public async Task<bool> EditDeleteAfterDays(List<QiniuFile> list, int deleteAfterDays = 0)
        {
            return await Task.Run(() =>
              {
                  if (list.Count > 0)
                  {
                      string[] urls = new string[list.Count];

                      var result = false;
                      foreach (QiniuFile qiNiuFileInfo in list)
                      {
                          HttpResult expireRet = bucketManager.DeleteAfterDays(Bucket, qiNiuFileInfo.FileName, deleteAfterDays);
                          result &= expireRet.Code == (int)HttpCode.OK;

                      }
                      return result;

                  }
                  else
                  {
                      return false;
                  }
              });
        }

        public async Task<bool> RefreshNetAddress(List<QiniuFile> list)
        {
            if (list.Count > 0)
            {
                string[] urls = new string[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    QiniuFile qiNiuFileInfo = list[i];

                    urls[i] = GetPublishUrl(qiNiuFileInfo.FileName);
                    if (string.IsNullOrWhiteSpace(urls[i]))
                    {
                        return false;
                    }
                }
                return await Task.Run(() =>
                {
                    bool result = QiniuHelper.RefreshUrls(mac, urls);

                    return result;

                });



            }
            else
            {
                return false;
            }
        }

        private string GetPublishUrl(string fileName)
        {
            if (CurrentDomain.Result.Count > 0)
            {

                string domain = CurrentDomain.Result[0];

                if (domain.StartsWith(".") && !string.IsNullOrWhiteSpace(Bucket))
                {
                    domain = Bucket + domain;
                }
                string domainUrl = config.UseHttps ? "https://" + domain : "http://" + domain;

                return DownloadManager.CreatePublishUrl(domainUrl, fileName);
            }
            else
            {
                LogHelper.LogError("无法获得空间的域名");
                return string.Empty;
            }


        }

        private string GetPrivateUrl(string fileName)
        {
            if (CurrentDomain?.Result?.Count > 0)
            {

                string domain = CurrentDomain.Result[0];
                if (domain.StartsWith(".") && !string.IsNullOrWhiteSpace(Bucket))
                {
                    domain = Bucket + domain;
                }

                domain = config.UseHttps ? "https://" + domain : "http://" + domain;
                return DownloadManager.CreatePrivateUrl(mac, domain, fileName, 3600);

            }
            else
            {
                LogHelper.LogError("无法获得空间的域名");
                return string.Empty;
            }


        }


        public async Task<bool> Upload(string[] fileUploadFiles, string callbackUrl, string callbackBody, bool overlay = true)
        {
            return await Task.Run(() =>
            {
                if (IsBusy == true)
                {
                    return false;
                }
                IsBusy = true;


                if (fileUploadFiles.Length <= 0) return false;


                bool result;

                foreach (string file in fileUploadFiles)
                {
                    var fileInfo = new System.IO.FileInfo(file);

                    if (fileInfo.Length > 1024 * 1024 * 5)
                    {
                        LogHelper.LogError("单个文件大小不得大于5M");
                        return false;
                    }
                }
                if (fileUploadFiles.Length > 10)
                {
                    LogHelper.LogError("每次上传文件不得大于10个");
                    return false;
                }

                result = true;

                //普通上传
                if (overlay == true)
                {

                    foreach (string file in fileUploadFiles)
                    {
                        var key = Path.GetFileName(file);
                        var currentResult = UploadFile(file, key, callbackUrl, callbackBody, true);
                        var currentDataResult = false;
                        if (currentResult.Errorno == CommonResultInfo.SUCCESS)
                        {
                            var dataResult = GetData((currentResult as CommonResultInfo<string>).ResultObject);
                            if (dataResult)
                            {
                                currentDataResult = true;
                            }
                            else
                            {
                                bucketManager.Delete(Bucket, key);
                                currentDataResult = false;
                            }

                        }
                        else
                        {
                            currentDataResult = false;
                        }
                        result &= currentDataResult;


                    }
                }
                else
                {
                    //不覆盖上传，文件若存在就跳过


                    foreach (string file in fileUploadFiles)
                    {
                        var key = Path.GetFileName(file);
                        var currentResult = UploadFile(file, key, callbackUrl, callbackBody);
                        var currentDataResult = false;
                        if (currentResult.Errorno == CommonResultInfo.SUCCESS)
                        {
                            var dataResult = GetData((currentResult as CommonResultInfo<string>).ResultObject);
                            if (dataResult)
                            {
                                currentDataResult = true;
                            }
                            else
                            {
                                bucketManager.Delete(Bucket, key);
                                currentDataResult = false;
                            }

                        }
                        else
                        {
                            currentDataResult = false;
                        }
                        result &= currentDataResult;

                    }
                }
                IsBusy = false;
                return result;

            });
        }

        public async Task<bool> UploadSingle(string fileUploadFile, string key, string callbackUrl, string callbackBody, bool overlay = true)
        {
            return await Task.Run(async () =>
            {
                if (IsBusy == true)
                {
                    return false;
                }
                IsBusy = true;

                if (string.IsNullOrEmpty(fileUploadFile)) return false;

                if (string.IsNullOrEmpty(key))
                {
                    key = Path.GetFileName(fileUploadFile);
                }

                var result = await UploadFileResumable(fileUploadFile, key, callbackUrl, callbackBody, overlay);
                IsBusy = false;

                if (result.Errorno == CommonResultInfo.SUCCESS)
                {
                    var dataResult = GetData((result as CommonResultInfo<string>).ResultObject);
                    if (dataResult)
                    {
                        return true;
                    }
                    else
                    {
                        bucketManager.Delete(Bucket, key);
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            });
        }


        private async Task<ICommonResultInfo> UploadFileResumable(string file, string key, string callbackUrl, string callbackBody, bool overLay = false)
        {
            ICommonResultInfo commonResultInfo;
            var putPolicy = new PutPolicy();

            if (putPolicy != null)
            {
                Stream fs;
                var isFromNet = file.StartsWith("http");
                if (isFromNet)
                {
                    HttpClient httpClient = new HttpClient();
                    var httpStream = await httpClient.GetStreamAsync(file);
                    const int bufferLength = 1024;
                    byte[] buffer = new byte[bufferLength];
                    int actual;
                    var memoryStream = new MemoryStream();
                    while ((actual = httpStream.Read(buffer, 0, bufferLength)) > 0)
                    {
                        memoryStream.Write(buffer, 0, actual);
                    }
                    memoryStream.Position = 0;
                    fs = memoryStream;

                }
                else
                {
                    if (File.Exists(file))
                    {
                        fs = File.OpenRead(file);

                    }
                    else
                    {
                        fs = Assembly.GetExecutingAssembly().GetManifestResourceStream("QinuFileUploader.Assets.README.txt");
                    }
                }
                if (fs == null)
                {
                    commonResultInfo = new CommonResultInfo(string.Format("key:上传失败，找不到文件"), CommonResultInfo.ERROR);
                    return commonResultInfo;
                }
                using (fs)
                {
                    if (overLay)
                    {
                        putPolicy.Scope = Bucket + ":" + key;
                    }
                    else
                    {
                        putPolicy.Scope = Bucket;
                    }
                    putPolicy.SetExpires(3600);

                    putPolicy.DeleteAfterDays = 0;



                    putPolicy.CallbackUrl = callbackUrl;
                    putPolicy.CallbackBody = callbackUrl;
                    putPolicy.CallbackBodyType = "application/x-www-form-urlencoded";



                    string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());

                    ResumableUploader target = new ResumableUploader(config);

                    PutExtra extra = isFromNet ? new PutExtra() : new PutExtra { ResumeRecordFile = ResumeHelper.GetDefaultRecordKey(file, key) };
                    //设置断点续传进度记录文件

                    HttpResult result = target.UploadStream(fs, key, token, extra);

                    if (result.Code == 200)
                    {
                        commonResultInfo = new CommonResultInfo<string>(result.Text, string.Format("key:{0}上传成功！", key));
                        return commonResultInfo;
                    }
                    else
                    {
                        commonResultInfo = new CommonResultInfo(string.Format("key:{0}上传失败，错误码{1}，请重试！", key, result.Code), CommonResultInfo.ERROR);
                        return commonResultInfo;
                    }
                }
            }
            commonResultInfo = new CommonResultInfo(string.Format("key:{0}上传失败，成员变量putPolicy为空！", key), CommonResultInfo.ERROR);


            return commonResultInfo;
        }

        private ICommonResultInfo UploadFile(string file, string key, string callbackUrl, string callbackBody, bool overlay = false)
        {
            ICommonResultInfo commonResultInfo;
            var putPolicy = new PutPolicy();

            if (putPolicy != null)
            {
                putPolicy.FsizeLimit = 1024 * 1024 * 100;
                if (overlay)
                {
                    putPolicy.Scope = Bucket + ":" + key;
                }
                else
                {
                    putPolicy.Scope = Bucket;
                }
                putPolicy.SetExpires(3600);

                putPolicy.DeleteAfterDays = 0;



                putPolicy.CallbackUrl = callbackUrl;
                putPolicy.CallbackBody = callbackBody;
                putPolicy.CallbackBodyType = "application/x-www-form-urlencoded";

                string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());
                UploadManager um = new UploadManager(config);
                HttpResult result = um.UploadFile(file, key, token, null);

                if (result.Code == 200)
                {
                    commonResultInfo = new CommonResultInfo<string>(result.Text, string.Format("key:{0}上传成功！", key));
                    return commonResultInfo;
                }
                else
                {
                    commonResultInfo = new CommonResultInfo(string.Format("key:{0}上传失败，错误码{1}，请重试！", key, result.Code), CommonResultInfo.ERROR);
                    return commonResultInfo;
                }
            }
            commonResultInfo = new CommonResultInfo(string.Format("key:{0}上传失败，成员变量putPolicy为空！", key), CommonResultInfo.ERROR);


            return commonResultInfo;
        }


        internal bool GetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }
            var result = JsonConvert.DeserializeObject<CommonResultInfo>(data);
            if (result.Errorno == CommonResultInfo.SUCCESS)
            {
                return true;
            }
            else if (result.Errorno == CommonResultInfo.UNAUTHORIZED)
            {
                LogHelper.LogWarn(result.Msg);
                return false;

            }
            else
            {
                LogHelper.LogWarn(result.Msg);

                return false;
            }

        }


        public string GetPreviewAddress(List<QiniuFile> list)
        {
            var result = "";
            if (list.Count > 0)
            {
                string address = string.Empty;
                if (list[0].FileType.StartsWith("image"))
                {
                    result = GetPrivateUrl(list[0].FileName + "?imageView2/2/w/600/h/400/interlace/1/q/100");
                }
            }
            return result;
        }


        public void Rename(List<QiniuFile> list, string txtRename)
        {
            if (list.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(Bucket) && !string.IsNullOrWhiteSpace(list[0].FileName) &&
                !string.IsNullOrWhiteSpace(txtRename.Trim()))
                {
                    QiniuHelper.Move(bucketManager, Bucket, list[0].FileName, Bucket, txtRename.Trim());
                }
            }
        }
    }
}
