using Qiniu.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QinuFileUploader.Model.Qiniu
{
    public class QiniuRegion
    {
        public string Title { get; set; }
        public Zone Value { get; set; }


        public static IEnumerable<QiniuRegion> GetRegionList()
        {
            var result = new List<QiniuRegion>
            {
                new QiniuRegion() {Title = "华南", Value = Zone.ZONE_CN_South},
                new QiniuRegion() {Title = "华东", Value = Zone.ZONE_CN_East},
                new QiniuRegion() {Title = "华北", Value = Zone.ZONE_CN_North},
                new QiniuRegion() {Title = "北美", Value = Zone.ZONE_US_North},
                new QiniuRegion() {Title = "东南亚", Value = Zone.ZONE_AS_Singapore}
            };
            return result;
        }
    }
}
