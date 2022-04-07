using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace QinuFileUploader.Helper
{
    public class LocalDataHelper
    {
        public static IList<T> ReadCollectionLocal<T>() where T : class
        {
            var basePath = CommonHelper.ExePath;
            var dirPath = Path.Combine(basePath, "LocalData");
            if (DirFileHelper.IsExistDirectory(dirPath))
            {

                var fileName = string.Format("local_{0}s.json", typeof(T).Name);
                var filePath = Path.Combine(dirPath, fileName);
                if (DirFileHelper.IsExistFile(filePath))
                {
                    var serializedstr = DirFileHelper.ReadFile(filePath);
                    var deserializedobj = JsonConvert.DeserializeObject<IList<T>>(serializedstr);
                    return deserializedobj;
                }
            }
            return new List<T>();
        }


        public static void SaveCollectionLocal<T>(IList<T> source) where T : class
        {
            var basePath = CommonHelper.ExePath;
            var dirPath = Path.Combine(basePath, "LocalData");
            DirFileHelper.CreateDir(dirPath);
            var serializedstr = JsonConvert.SerializeObject(source);
            var fileName = string.Format("local_{0}s.json", typeof(T).Name);
            var filePath = Path.Combine(dirPath, fileName);
            DirFileHelper.CreateFile(filePath, serializedstr);
        }

        public static T ReadObjectLocal<T>() where T : class
        {
            var basePath = CommonHelper.ExePath;
            var dirPath = Path.Combine(basePath, "LocalData");
            if (DirFileHelper.IsExistDirectory(dirPath))
            {
                var fileName = string.Format("local_{0}.json", typeof(T).Name);
                var filePath = Path.Combine(dirPath, fileName);
                if (DirFileHelper.IsExistFile(filePath))
                {
                    var serializedstr = DirFileHelper.ReadFile(filePath);
                    var deserializedobj = JsonConvert.DeserializeObject<T>(serializedstr);
                    return deserializedobj;
                }
            }
            return default;
        }

        public static void SaveObjectLocal<T>(T source) where T : class
        {
            var basePath = CommonHelper.ExePath;
            var dirPath = Path.Combine(basePath, "LocalData");
            DirFileHelper.CreateDir(dirPath);
            var serializedstr = JsonConvert.SerializeObject(source);
            var fileName = string.Format("local_{0}.json", typeof(T).Name);
            var filePath = Path.Combine(dirPath, fileName);
            DirFileHelper.CreateFile(filePath, serializedstr);
        }


        public static void InitLocalPath()
        {
            var basePath = CommonHelper.ExePath;
            var dirPath = Path.Combine(basePath, "LocalData");
            if (DirFileHelper.IsExistDirectory(dirPath))
            {
                DirFileHelper.ClearDirectory(dirPath);
            }
            else
            {
                DirFileHelper.CreateDir(dirPath);
            }


        }
    }
}
