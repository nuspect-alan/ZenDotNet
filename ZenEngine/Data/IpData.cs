using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputingEngine.Data
{
    internal class IpData
    {
        internal string Address { get; set; }
        internal string AddressFamily { get; set; }

        internal IpData(string Address, string AddressFamily)
        {
            this.Address = Address;
            this.AddressFamily = AddressFamily;
        }
    }
}
