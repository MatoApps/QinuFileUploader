using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinuFileUploader.Model.LocalFile
{
    public class LocalFile : IFileInfo
    {
        public const string FolderType = "application/local-object-manager";

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }

        public string CreateDate { get; set; }

        public string Path { get; set; }

        public void SetFolderType()
        {
            this.FileType = FolderType;
            Type = FileInfoType.Folder;

        }

        public int Type { get; set; }

    }
}
