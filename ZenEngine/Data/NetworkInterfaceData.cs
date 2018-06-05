using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputingEngine.Data
{
    internal class NetworkInterfaceData
    {
        internal string InterfaceName { get; set; }
        internal List<IpData> Ips { get; set; }
        internal NetworkInterfaceData(string InterfaceName)
        {
            this.InterfaceName = InterfaceName;
            this.Ips = new List<IpData>();
        }
    }
}
