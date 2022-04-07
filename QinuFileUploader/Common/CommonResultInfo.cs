using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QinuFileUploader.Common
{
    public interface ICommonResultInfo
    {

        int Errorno { get; set; }
        string Msg { get; set; }
        string Status { get; }

        string GetJsonString();
    }

    public class CommonResultInfo : ICommonResultInfo
    {
        public const int SUCCESS = 0;
        public const int ERROR = -1;
        public const int FATAL = -2;
        public const int UNAUTHORIZED = -3;
        public CommonResultInfo()
        {

        }
        public CommonResultInfo(string msg, int errorno = SUCCESS)
        {
            Errorno = errorno;
            Msg = msg;
        }

        public CommonResultInfo(int errorno = SUCCESS)
        {
            Errorno = errorno;
            Msg = "success";
        }
        [JsonProperty("errorno")]
        public int Errorno { get; set; }
        [JsonProperty("msg")]
        public string Msg { get; set; }
        [JsonProperty("status")]
        public string Status => Errorno == SUCCESS ? "ok" : "error";
        public string GetJsonString()
        {
            var result = JsonConvert.SerializeObject(this);
            return result;

        }

    }

    public class CommonResultInfo<T> : ICommonResultInfo
    {
        public const int SUCCESS = 0;
        public const int ERROR = -1;
        public const int FATAL = -2;
        public const int UNAUTHORIZED = -3;
        public CommonResultInfo()
        {

        }
        public CommonResultInfo(T resultObject, string msg, int errorno = SUCCESS)
        {
            if (errorno == SUCCESS)
            {
                if (resultObject == null)
                {
                    Errorno = ERROR;
                }
            }
            else
            {
                Errorno = errorno;
            }
            Msg = msg;
            ResultObject = resultObject;
        }

        public CommonResultInfo(T resultObject, int errorno = SUCCESS)
        {
            if (errorno == SUCCESS)
            {
                if (resultObject == null)
                {
                    Errorno = ERROR;
                    Msg = "error";

                }
                else
                {
                    Msg = "success";

                }
            }
            else
            {
                Errorno = errorno;
                Msg = "error";
            }
            ResultObject = resultObject;
        }
        [JsonProperty("errorno")]
        public int Errorno { get; set; }
        [JsonProperty("msg")]
        public string Msg { get; set; }
        [JsonProperty("status")]
        public string Status => Errorno == SUCCESS ? "ok" : "error";
        [JsonProperty("result")]
        public T ResultObject { get; set; }

        public string GetJsonString()
        {
            var result = JsonConvert.SerializeObject(this);
            return result;

        }
    }
}
