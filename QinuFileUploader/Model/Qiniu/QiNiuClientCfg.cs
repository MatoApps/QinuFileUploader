﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Model.Qiniu
{
   public class QiNiuClientCfg
    {
        public  string Ak { get; set; }
        public string Sk { get; set; }

        public int? DeleteAfterDays { get; set; }
    }
}
