using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputingEngine.Data
{
    internal class ElementInfoData
    {
        public long StartedJsFormat { get; set; }
        public DateTime Started { get; set; }
        public string ID { get; set; }
        public string Result { get; set; }
        public bool Mark { get; set; }
        public ArrayList CurrentLoopElements { get; set; }
        public string Error { get; set; }
        public string ResultType { get; set; }
        public ElementInfoData(string ID, string Result, DateTime Started, bool Mark, string Error, string ResultType)
        {
            this.Error = Error;
            this.ID = ID;
            this.Started = Started;
            this.Result = Result;
            this.StartedJsFormat = Utils.ToJavascriptTimestamp(Started);
            this.Mark = Mark;
            this.CurrentLoopElements = new ArrayList();
            this.ResultType = ResultType;
        }
    }
}
