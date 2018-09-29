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

//dotnet publish -c Release -r win-x64

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ZenCommon;

namespace ZenCS
{
    class Program
    {
        private static string[] GetFiles(string sourceFolder, string filters, System.IO.SearchOption searchOption)
        {
            return filters.Split('|').SelectMany(filter => System.IO.Directory.GetFiles(sourceFolder, filter, searchOption)).ToArray();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("*****************************************************************" + Environment.NewLine +
                                "* *     ZenCS script v1.0.0.0 " + Environment.NewLine +
                                "* *" + Environment.NewLine +
                                "* *     Copyright(c) 2018 Zenodys B.V." + Environment.NewLine +
                                "*****************************************************************");
            try
            {
                ZenMocks.FillMocks();

                Console.WriteLine(Environment.NewLine + "Build started....");
                string scriptFile = args.Length > 0 ? args[0] : string.Empty;

                // No file name is provided.
                // Check if there is an .cs or .txt file in current compile directory
                if (string.IsNullOrEmpty(scriptFile))
                {
                    string[] files = GetFiles(Environment.CurrentDirectory, "*.cs|*.txt", SearchOption.TopDirectoryOnly);
                    // get first cs or txt file and hope that this is script file
                    scriptFile = files.Length > 0 ? files[0] : string.Empty;
                }
                else if (!File.Exists(scriptFile))
                {
                    if (File.Exists(string.Format(scriptFile + "{0}", ".cs")))
                        scriptFile += ".cs";
                    else if (File.Exists(string.Format(scriptFile + "{0}", ".txt")))
                        scriptFile += ".txt";
                }

                string outFile = Path.ChangeExtension(Path.Combine(Environment.CurrentDirectory, scriptFile), "dll");
                if (File.Exists(outFile))
                    File.Delete(outFile);

                string proc = ZenCsScriptCore.GetProcedure(File.ReadAllText(scriptFile));
                ZenCsScriptData script = ZenCsScriptCore.Initialize(proc, ZenMocks.Elements, ZenMocks.Element,
                    outFile, ZenMocks.Board, false);

                Console.WriteLine(string.Format(Environment.NewLine + "Executing {0} script...."
                    + Environment.NewLine, Path.GetFileName(outFile)));

                script.ZenCsScript.Init(ZenMocks.Elements, ZenMocks.Element);
                script.ZenCsScript.RunCustomCode(script.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + "ERROR: " +
                    (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ?
                    ex.InnerException.Message : ex.Message));
            }
            finally
            {
                Console.WriteLine(Environment.NewLine + "Press any key to exit...");
                Console.Read();
            }
        }
    }
}
