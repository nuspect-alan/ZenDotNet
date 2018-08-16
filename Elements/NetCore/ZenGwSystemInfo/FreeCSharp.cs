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
#if !NETCOREAPP2_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZenGwSystemInfo
{
    internal class FreeCSharp
    {
        public long MemTotal { get; private set; }
        public long MemFree { get; private set; }
        public long Buffers { get; private set; }
        public long Cached { get; private set; }

        public void GetValues()
        {
            string[] memInfoLines = File.ReadAllLines(@"/proc/meminfo");

            MemInfoMatch[] memInfoMatches =
            {
                new MemInfoMatch(@"^Buffers:\s+(\d+)", value => Buffers = Convert.ToInt64(value)),
                new MemInfoMatch(@"^Cached:\s+(\d+)", value => Cached = Convert.ToInt64(value)),
                new MemInfoMatch(@"^MemFree:\s+(\d+)", value => MemFree = Convert.ToInt64(value)),
                new MemInfoMatch(@"^MemTotal:\s+(\d+)", value => MemTotal = Convert.ToInt64(value))
            };

            foreach (string memInfoLine in memInfoLines)
            {
                foreach (MemInfoMatch memInfoMatch in memInfoMatches)
                {
                    Match match = memInfoMatch.regex.Match(memInfoLine);
                    if (match.Groups[1].Success)
                    {
                        string value = match.Groups[1].Value;
                        memInfoMatch.updateValue(value);
                    }
                }
            }
        }

        internal class MemInfoMatch
        {
            public Regex regex;
            public Action<string> updateValue;

            public MemInfoMatch(string pattern, Action<string> update)
            {
                this.regex = new Regex(pattern, RegexOptions.Compiled);
                this.updateValue = update;
            }
        }
    }
}
 #endif