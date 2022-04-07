using QinuFileUploader.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QinuFileUploader.Model.Qiniu
{
    public class QiniuFile : IFileInfo
    {
        public const string QiniuFolderType = "application/qiniu-object-manager";

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string StorageType { get; set; }
        public void SetFolderType()
        {
            FileType = QiniuFolderType;
            Type = FileInfoType.Folder;
        }

        public string FileSize { get; set; }

        public string CreateDate { get; set; }

        public string EndUser { get; set; }

        public string Path
        {
            get
            {
                var fullUrl = string.Format("http://res.matoapp.net/{0}", FileName);
                return fullUrl;

            }
            set
            {
                throw new Exception();
            }
        }

        public int Type { get; set; }

    }
}
