
using System;

namespace Workshop.Infrastructure.Common
{
    public class CommonFunctionEventArgs : EventArgs
    {
        public CommonFunctionEventArgs(object info, string code)
        {
            Info = info;
            Code = code;
        }
        public object Info { get; set; }
        public string Code { get; set; }

        public const string FAILD = "FAILD";
        public const string SUSSCESS = "SUCCESS";
        public const string CANCEL = "CANCEL";
        public const string OK = "OK";
    }
}
