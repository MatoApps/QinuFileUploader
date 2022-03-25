using QinuFileUploader.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Model.Qiniu
{
    public class QiniuFile : IFileInfo
    {
        private const string qiniuFolderType = "application/qiniu-object-manager";

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string StorageType { get; set; }

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

        public bool IsFolder
        {
            get
            {

                return FileType == qiniuFolderType;

            }
        }

    }
}
