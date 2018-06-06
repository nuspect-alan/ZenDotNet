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

using IniParser;
using IniParser.Model;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ComputingEngine
{
    class Program
    {
        #region Fields
        internal static ZenEngine GadgeterBoard;
        internal static string WorkstationName;
        internal static string Views;
        internal static string ElementsVersion;
        internal static string CoreVersion;
        #endregion

        #region Main
        static void Main(string[] args)
        {
            CoreVersion = "1.0.0";
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Console.WriteLine(string.Concat("ZenEngine v", CoreVersion, "..."));

            CreateKeyStore();
            List<string> PreStartErrors = new List<string>();
            try
            {
                foreach (string s in ConfigurationManager.AppSettings["FoldersToClean"].Split(';'))
                {
                    if (!string.IsNullOrEmpty(s) && Directory.Exists(s))
                    {
                        Directory.Delete(s, true);
                        Console.WriteLine("CLEANING " + s + " ....");
                    }
                }
            }
            catch (Exception ex)
            {
                PreStartErrors.Add("Unable to clear cache. " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message));
            }

            try
            {
                ExtractArchive();
            }
            catch (Exception ex)
            {
                PreStartErrors.Add("Unable to install new template version. " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message));
            }

            DirSearch(TemplatesPath);
            if (GadgeterBoard == null || string.IsNullOrEmpty(GadgeterBoard.TemplateRootDirectory))
            {
                Console.WriteLine("Cannot find any project in " + TemplatesPath + ".");
                Console.WriteLine("Please go to https://app.zenodys.com, download project, and extract it to project path.");
                Console.ReadLine();
                return;
            }

            try
            {
                GadgeterBoard.ClearProcesses();
                StartThread(GadgeterBoard.TemplateRootDirectory, PreStartErrors);
                if (ConfigurationManager.AppSettings["IsMqttEnabled"] == "0")
                    Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                GadgeterBoard.PublishInfoPrint("Runtime", "Critical error in executing template. " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message), "error");
                Console.WriteLine("Critical error in executing template. " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message));
                Console.ReadLine();
            }
        }
        #endregion

        #region Event handlers
        #region MqttUtils_OnBeforeReconnectEvent
        protected static void MqttUtils_OnBeforeReconnectEvent(object sender, EventArgs args)
        {
            UnsubscribeSystemTopics();
        }
        #endregion

        #region MqttUtils_OnReconnectEvent
        protected static void MqttUtils_OnReconnectEvent(object sender, EventArgs args)
        {
            SubscribeSystemTopics();
        }
        #endregion

        #region CurrentDomain_AssemblyResolve
        protected static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (args.Name.IndexOf(".resources") > -1)
                    return null;

                string sVersion = "0.0.0.0";
                if (args.Name.IndexOf(',') > -1)
                    sVersion = args.Name.Split(',')[1].Split('=')[1];

                Assembly asm = Assembly.Load(File.ReadAllBytes(Path.Combine(GadgeterBoard.TemplateRootDirectory, "Dependencies", args.Name.Split(',')[0], sVersion, args.Name.Split(',')[0] + ".dll")));
                if (asm == null)
                    Console.WriteLine(args.Name + " is null!");

                return asm;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical exception in loading " + args.Name + " : " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message.Trim()) ? ex.InnerException.Message : ex.Message));
                Console.ReadLine();
                return null;
            }
        }
        #endregion
        #endregion

        #region Properties
        #region TemplatesPath
        static string TemplatesPath
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ConfigurationManager.AppSettings["TemplatesPathEnvVar"])))
                    return Path.Combine(Environment.GetEnvironmentVariable(ConfigurationManager.AppSettings["TemplatesPathEnvVar"]), "project");
                else
                    return Path.Combine(Environment.CurrentDirectory, "project");
            }
        }
        #endregion

        #region TopicPrefix
        internal static string TopicPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(GadgeterBoard.WorkstationID))
                    return "/" + GadgeterBoard.TemplateID;
                else
                    return "/" + GadgeterBoard.TemplateID + "/" + GadgeterBoard.WorkstationID;
            }
        }
        #endregion

        #region SystemTopics
        internal static List<string> SystemTopics
        {
            get
            {
                List<string> systemTopics = new List<string>();
                systemTopics.Add(string.Concat(TopicPrefix, "/info/get"));
                systemTopics.Add(string.Concat("/", GadgeterBoard.TemplateID, "/logOff"));

                if (GadgeterBoard.IsRemoteDebugEnabled)
                {
                    systemTopics.Add(string.Concat(TopicPrefix, "/debug/get"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/debug/getSnapshot"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/debug/stop"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/breakpoint/continue"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/breakpoint/stop"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/debug/logOff"));
                    systemTopics.Add(string.Concat(TopicPrefix, "/info/fileListRequest"));
                }

                if (GadgeterBoard.IsRemoteUpdateEnabled)
                    systemTopics.Add(string.Concat(TopicPrefix, "/update"));

                if (GadgeterBoard.IsRemoteRestartEnabled)
                    systemTopics.Add(string.Concat(TopicPrefix, "/restart"));

                if (GadgeterBoard.IsRemoteInfoEnabled)
                    systemTopics.Add(string.Concat(TopicPrefix, "/info/gatewayRequest"));

                return systemTopics;
            }
        }
        #endregion
        #endregion

        #region Functions
        #region CreateKeyStore
        static void CreateKeyStore()
        {
            if (!File.Exists("keystore/public.xml") || !File.Exists("keystore/public.xml"))
            {
                if (Directory.Exists("keystore"))
                    Directory.Delete("keystore", true);

                Directory.CreateDirectory("keystore");
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    File.WriteAllText("keystore/public.xml", rsa.ToXmlString(false));
                    File.WriteAllText("keystore/private.xml", rsa.ToXmlString(true));
                }
            }
        }
        #endregion

        #region ExtractArchive
        static void ExtractArchive()
        {
            if (File.Exists(Path.Combine(TemplatesPath, "zenodys.zip")))
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(TemplatesPath).GetDirectories())
                    dir.Delete(true);

                switch (ConfigurationManager.AppSettings["EnvMode"])
                {
                    case "0":
                    case "1":
                        using (FileStream fs = new FileStream(Path.Combine(TemplatesPath, "zenodys.zip"), FileMode.Open))
                        {
                            using (ZipFile zenoZip = ZipFile.Read(fs))
                            {
                                foreach (ZipEntry e in zenoZip)
                                {
                                    string templateId = e.FileName.Substring(0, e.FileName.IndexOf('/')).Trim();
                                    if (Directory.Exists(Path.Combine(TemplatesPath, templateId)))
                                        Directory.Delete(Path.Combine(TemplatesPath, templateId), true);

                                    zenoZip.ExtractAll(TemplatesPath);
                                    break;
                                }
                            }
                        }
                        break;

                    case "2":
                        ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "unzip", Arguments = Path.Combine(TemplatesPath, "zenodys.zip") + " -d " + TemplatesPath };
                        Process proc = new Process() { StartInfo = startInfo, };
                        proc.Start();
                        proc.WaitForExit();
                        break;
                }
                File.Delete(Path.Combine(TemplatesPath, "zenodys.zip"));
            }
        }
        #endregion

        #region StartMqtt
        internal static void StartMqtt()
        {
            if (ConfigurationManager.AppSettings["IsMqttEnabled"] == "1")
            {
                MqttUtils.OnReconnectInternalEvent += MqttUtils_OnReconnectEvent;
                MqttUtils.OnBeforeReconnectInternalEvent += MqttUtils_OnBeforeReconnectEvent;

                while (MqttUtils.ZenMqtt == null || (MqttUtils.ZenMqtt != null && !MqttUtils.ZenMqtt.IsConnected))
                {
                    MqttUtils.Connect();
                    if (MqttUtils.ZenMqtt != null && MqttUtils.ZenMqtt.IsConnected)
                        break;

                    Thread.Sleep(3000);
                }
                new Thread(MqttUtils.ConnectionThreadProc).Start();
            }
        }
        #endregion

        #region SubscribeSystemTopics
        static void SubscribeSystemTopics()
        {
            foreach (string topic in SystemTopics)
                MqttUtils.Subscribe(topic);
        }
        #endregion

        #region UnsubscribeSystemTopics
        static void UnsubscribeSystemTopics()
        {
            if (MqttUtils.ZenMqtt != null)
                MqttUtils.ZenMqtt.Unsubscribe(SystemTopics.ToArray());
        }
        #endregion

        #region DirSearch
        static void DirSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    //Find template directory
                    if (File.Exists(Path.Combine(d + Path.DirectorySeparatorChar, "Settings.ini")))
                    {
                        GadgeterBoard = new ZenEngine();
                        GadgeterBoard.TemplateRootDirectory = d + Path.DirectorySeparatorChar;
                        var parser = new FileIniDataParser();
                        IniData parsedData = parser.ReadFile(Path.Combine(Program.GadgeterBoard.TemplateRootDirectory, "Settings.ini"));
                        GadgeterBoard.TemplateID = parsedData["Template"]["ID"];
                        return;
                    }
                    DirSearch(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
        #endregion

        #region StartThread
        static void StartThread(string dir, List<string> PreStartErrors)
        {
            Console.WriteLine("[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] Starting " + GadgeterBoard.TemplateID);
            GadgeterBoard.Start(PreStartErrors);
        }
        #endregion
        #endregion
    }
}
