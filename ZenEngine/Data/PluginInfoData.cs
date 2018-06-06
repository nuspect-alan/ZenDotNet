/*************************************************************************
 * Copyright (c) 2015, 2018 Zenodys BV
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Contributors:
 *    Tomaž Vinko
 *   
 **************************************************************************/

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
