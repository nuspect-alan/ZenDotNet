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

using ComputingEngine.Data;
using IniParser;
using IniParser.Model;
using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using ZenCommon;

namespace ComputingEngine
{
    internal class MqttUtils
    {
        #region Constants
        #region BUFFER_INT_LENGTH
        const int BUFFER_INT_LENGTH = 4;
        #endregion
        #endregion

        #region Events
        #region OnRecconectInternal
        internal delegate void OnRecconectInternal(object sender, EventArgs args);
        internal static event OnRecconectInternal OnReconnectInternalEvent;
        #endregion

        #region OnBeforeRecconectInternal
        internal delegate void OnBeforeRecconectInternal(object sender, EventArgs args);
        internal static event OnBeforeRecconectInternal OnBeforeReconnectInternalEvent;
        #endregion
        #endregion

        #region 
        #region _runConnectionThreadProc
        static bool _runConnectionThreadProc = true;
        #endregion

        #region _connectionThreadProcSync
        static object _connectionThreadProcSync = new object();
        #endregion

        #region ZenMqtt
        internal static MqttClient ZenMqtt;
        #endregion
        #endregion

        #region Internal functions
        #region PublishGatewayInfo
        internal static void PublishGatewayInfo()
        {
            try
            {
                dynamic gatewayData = new ExpandoObject();
                List<Hashtable> drivesData = new List<Hashtable>();
                gatewayData.Views = new List<string>();
                //gatewayData.NetworkInterfaces = new List<NetworkInterfaceData>();

                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    try
                    {
                        Hashtable ht = new Hashtable();
                        ht.Add("DriveType", di.DriveType.ToString());
                        ht.Add("AvailableFreeSpace", di.AvailableFreeSpace);
                        ht.Add("TotalSize", di.TotalSize);
                        ht.Add("Name", di.Name);
                        ht.Add("VolumeLabel", di.VolumeLabel);
                        drivesData.Add(ht);
                    }
                    catch { }
                }

                foreach (string s in Regex.Split(Program.Views, ","))
                {
                    if (!string.IsNullOrEmpty(s))
                        gatewayData.Views.Add(s.Trim());
                }

                gatewayData.Drives = drivesData;
                gatewayData.PrivateMemorySize64 = Process.GetCurrentProcess().PrivateMemorySize64;
                gatewayData.WorkingSet64 = Process.GetCurrentProcess().WorkingSet64;
                gatewayData.StartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("O");
                gatewayData.ElementsVersion = Program.ElementsVersion;
                gatewayData.CoreVersion = Program.CoreVersion;

                gatewayData.IsDebugMode = Program.GadgeterBoard._tasks.IsDebugInProgress;
                gatewayData.IsBreakpointMode = Program.GadgeterBoard.IsBreakpointMode;

                gatewayData.ProcessId = Process.GetCurrentProcess().Id;
                gatewayData.CurrentDirectory = Environment.CurrentDirectory;

                var parser = new FileIniDataParser();
                IniData parsedData = parser.ReadFile(Path.Combine(Program.GadgeterBoard.TemplateRootDirectory, "Settings.ini"));

                gatewayData.LastTemplateUpdate = parsedData["Update"]["LastUpdate"];
                gatewayData.TemplateUpdatedBy = parsedData["Update"]["UpdatedBy"];

                /*
                try
                {
                    List<NetworkInterfaceData> networkInterfaces = new List<NetworkInterfaceData>();
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        NetworkInterfaceData networkInterface = new NetworkInterfaceData(ni.Name);
                        List<IpData> ips = new List<IpData>();
                        foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                            ips.Add(new IpData(ip.Address.ToString(), ip.Address.AddressFamily.ToString()));
                        
                        networkInterface.Ips = ips;
                        networkInterfaces.Add(networkInterface);
                    }
                    gatewayData.NetworkInterfaces = networkInterfaces;
                }
                catch { }
                */

                ZenMqtt.Publish(Program.TopicPrefix + "/info/gatewayResponse", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gatewayData)));
            }
            catch (Exception ex)
            {
                string error = "Invalid error.";
                if (ex != null && ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                    error = ex.InnerException.Message;
                else if (ex != null && !string.IsNullOrEmpty(ex.Message))
                    error = ex.Message;

                Console.WriteLine("Error in publishing info response : " + error);
                ZenMqtt.Publish(Program.TopicPrefix + "/info/gatewayResponseError", Encoding.UTF8.GetBytes(error));
            }
        }
        #endregion

        #region PublishTemplateState
        internal static void PublishTemplateState()
        {
            dynamic gatewayRemotePermissions = new ExpandoObject();
            gatewayRemotePermissions.IsRemoteDebugEnabled = Program.GadgeterBoard.IsRemoteDebugEnabled;
            gatewayRemotePermissions.IsRemoteInfoEnabled = Program.GadgeterBoard.IsRemoteInfoEnabled;
            gatewayRemotePermissions.IsRemoteRestartEnabled = Program.GadgeterBoard.IsRemoteRestartEnabled;
            gatewayRemotePermissions.IsRemoteUpdateEnabled = Program.GadgeterBoard.IsRemoteUpdateEnabled;
            gatewayRemotePermissions.CoreVersion = Program.CoreVersion;
            gatewayRemotePermissions.ElementsVersion = Program.ElementsVersion;
            ZenMqtt.Publish(Program.TopicPrefix + "/info/send", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gatewayRemotePermissions)));
        }
        #endregion

        #region Connect
        #region VerifyServerCertificate
        static bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
                Console.WriteLine("Warning : " + sslPolicyErrors.ToString());

            return true;
        }
        #endregion

        #region ConnectHelper
        internal static byte ConnectHelper(ref MqttClient mqttClient)
        {
            try
            {
                byte oReturn = 0;
                if (Program.GadgeterBoard.AuthenticateMqttWithPrivateKey)
                {
                    mqttClient = new MqttClient(Program.GadgeterBoard.MqttHost, Program.GadgeterBoard.MqttPort, true, null, (MqttSslProtocols)Enum.Parse(typeof(MqttSslProtocols), Program.GadgeterBoard.MqttSslProtocolVersion), VerifyServerCertificate);
                    oReturn = mqttClient.Connect(Guid.NewGuid().ToString(), Program.GadgeterBoard.TemplateID, Program.GadgeterBoard.TemplateKey, false, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true, string.Concat(Program.TopicPrefix, "/shutDown"), Program.GadgeterBoard.TemplateID, true, 60);
                    Console.WriteLine("MQTT STATE : " + Program.GadgeterBoard.MqttHost + ":" + Program.GadgeterBoard.MqttPort + ", " + Program.GadgeterBoard.MqttSslProtocolVersion + ", " + "0x" + oReturn.ToString("X2"));
                }
                else
                {
                    string ip = GetIp(Program.GadgeterBoard.MqttHost);
                    mqttClient = new MqttClient(ip, Program.GadgeterBoard.MqttPort, false, null, (MqttSslProtocols)Enum.Parse(typeof(MqttSslProtocols), Program.GadgeterBoard.MqttSslProtocolVersion));
                    oReturn = mqttClient.Connect(Guid.NewGuid().ToString(), null, null, false, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true, string.Concat(Program.TopicPrefix, "/shutDown"), Program.GadgeterBoard.TemplateID, true, 60);
                    Console.WriteLine("MQTT STATE : " + ip + ", " + "0x" + oReturn.ToString("X2"));
                }
                return oReturn;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                    Console.WriteLine("[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + ex.InnerException.Message);

                else if (!string.IsNullOrEmpty(ex.Message))
                    Console.WriteLine("[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + ex.Message);

                else
                    Console.WriteLine("[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + "Error connecting to mqtt host...");

                return 0x55;
            }
        }
        #endregion

        #region Connect
        internal static void Connect()
        {
            try
            {
                if (OnBeforeReconnectInternalEvent != null && ZenMqtt != null)
                    OnBeforeReconnectInternalEvent(ZenMqtt, new EventArgs());

                if (ConnectHelper(ref ZenMqtt) == MqttMsgConnack.CONN_ACCEPTED)
                {
                    ZenMqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    if (OnReconnectInternalEvent != null)
                        OnReconnectInternalEvent(ZenMqtt, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                string error = "";
                if (ex.InnerException != null && ex.InnerException.Message != "")
                    error = ex.InnerException.Message;
                else if (ex != null)
                    error = ex.Message;
            }
        }
        #endregion
        #endregion

        #region Subscribe
        internal static void Subscribe(string[] topics, byte qos = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
        {
            ZenMqtt.Subscribe(topics, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        internal static void Subscribe(string topic, byte qos = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
        {
            ZenMqtt.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
        #endregion

        #region Publish
        internal static void Publish(string topic, string message)
        {
            try
            {
                ZenMqtt.Publish(topic, Encoding.UTF8.GetBytes(message));
            }
            catch { }
        }

        internal static void Publish(string topic, byte[] message, byte qos)
        {
            try
            {
                ZenMqtt.Publish(topic, message, qos, false);
            }
            catch { }
        }
        #endregion
        #endregion

        #region Event Handlers
        #region gadgeteerBoard_DebugDataReceived
        internal static void gadgeteerBoard_DebugDataReceived(object sender, DebugData e)
        {
            MqttUtils.PublishElementsData((sender as Tasks), e.elements);
        }
        #endregion

        #region client_MqttMsgPublishReceived
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic == "/" + Program.GadgeterBoard.TemplateID + "/logOff")
            {
                Hashtable htLogOffMessage = JsonConvert.DeserializeObject<Hashtable>(Encoding.UTF8.GetString(e.Message));
                if (string.IsNullOrEmpty(Program.GadgeterBoard.WorkstationID) && htLogOffMessage["gatewayId"].ToString() == "0" || htLogOffMessage["gatewayId"].ToString() == Program.GadgeterBoard.WorkstationID)
                    UnsubscribeDebugEvent();
            }
            else
            {
                switch (e.Topic.Replace(Program.TopicPrefix, string.Empty))
                {
                    case "/restart":
                        RestartMe(Encoding.UTF8.GetString(e.Message));
                        break;

                    case "/update":
                        UpdateMe(e.Message);
                        break;

                    case "/info/get":
                        PublishTemplateState();
                        break;

                    case "/info/gatewayRequest":
                        PublishGatewayInfo();
                        break;

                    case "/debug/getSnapshot":
                        PublishSnapshotElements();
                        break;

                    case "/breakpoint/continue":
                        ContinueWithBreakpoint();
                        break;

                    case "/breakpoint/stop":
                        StopBreakpoint();
                        break;

                    case "/debug/stop":
                    case "/debug/logOff":
                        UnsubscribeDebugEvent();
                        break;

                    case "/info/fileListRequest":
                        PublishFileInfos();
                        break;
                }
            }
        }
        #endregion
        #endregion

        #region Private functions
        #region GetElementErrorMessage
        static string GetElementErrorMessage(IElement p)
        {
            string errorMessage = string.Empty;
            if (p.ErrorCode != 0)
            {
                if (string.IsNullOrEmpty(p.ErrorMessage))
                    errorMessage = "UNKNOWN ERROR";
                else
                    errorMessage = p.ErrorMessage;
            }
            return errorMessage;
        }
        #endregion

        #region GetElementResult
        static string GetElementResult(IElement e)
        {
            string result = string.Empty;
            if (e.LastResultBoxed as DataSet != null)
            {
                Dictionary<string, Dictionary<string, string>> datasetSchema = new Dictionary<string, Dictionary<string, string>>();
                foreach (DataTable table in (e.LastResultBoxed as DataSet).Tables)
                {
                    Dictionary<string, string> cols = new Dictionary<string, string>();
                    foreach (DataColumn dc in table.Columns)
                        cols.Add(dc.ColumnName, dc.DataType.Name);

                    datasetSchema.Add(table.TableName, cols);
                }
                result = JsonConvert.SerializeObject(datasetSchema);
            }
            else
                result = e.LastResult;

            return result;
        }
        #endregion

        #region GetElementResultType
        static string GetElementResultType(IElement e)
        {
            return e.LastResultBoxed != null ? e.LastResultBoxed.GetType().Name : string.Empty;
        }
        #endregion

        #region DisconnectMqtt
        static void DisconnectMqtt()
        {
            lock (_connectionThreadProcSync)
            {
                if (ZenMqtt != null && ZenMqtt.IsConnected)
                {
                    ZenMqtt.Publish(Program.TopicPrefix + "/shutDown", Encoding.UTF8.GetBytes(Program.GadgeterBoard.TemplateID));
                    _runConnectionThreadProc = false;
                    ZenMqtt.Disconnect();
                    ZenMqtt.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
                    ZenMqtt = null;
                }
                GC.WaitForPendingFinalizers();
                Thread.Sleep(2000);
            }
        }
        #endregion

        #region StartUpdater
        static void StartUpdater()
        {
            DisconnectMqtt();
            Program.GadgeterBoard.FireRebootEvent();
            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.WindowStyle = ProcessWindowStyle.Normal;
            procInfo.UseShellExecute = false;
            switch (ConfigurationManager.AppSettings["EnvMode"])
            {
                case "0":
                    procInfo.FileName = Path.Combine(Environment.CurrentDirectory, "Updater.exe");
                    Process.Start(procInfo);
                    break;

                case "1":
                    procInfo.FileName = "mono";
                    procInfo.Arguments = Path.Combine(Environment.CurrentDirectory, "Updater.exe");
                    Process.Start(procInfo);
                    break;

                case "2":
                    var parser = new FileIniDataParser();
                    IniData parsedData = parser.ReadFile(Path.Combine(Program.GadgeterBoard.TemplateRootDirectory, "Settings.ini"));

                    if (parsedData["Compile"]["Clean"] == "1")
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
                    ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "-c \"systemctl reload-or-restart snap.zenocore.zenocore\"" };
                    Process proc = new Process() { StartInfo = startInfo, };
                    proc.Start();
                    break;
            }
        }
        #endregion

        #region RestartMe
        static void RestartMe(string userID)
        {
            if (ConfigurationManager.AppSettings["IsMqttEnabled"] == "1")
            {
                ZenMqtt.Publish(Program.TopicPrefix + "/info/updateResponse", Encoding.UTF8.GetBytes("RESTARTING..."));
                File.WriteAllText("RestartProgress", userID);
            }

            Console.WriteLine("RESTARTING....");
            StartUpdater();
        }
        #endregion

        #region PrintUpdateError
        static void PrintUpdateError(Exception ex)
        {
            string error = "Unable to clear cache. Remote update stopped. Error details : " + ((ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message)) ? ex.InnerException.Message : ex.Message);
            Program.GadgeterBoard.PublishInfoPrint(error, "systemInfo");
            Console.WriteLine(error);
        }
        #endregion

        #region UpdateMe
        static void UpdateMe(byte[] message)
        {
            bool errorOccured = false;
            message = Convert.FromBase64CharArray(Encoding.UTF8.GetChars(message), 0, Encoding.UTF8.GetChars(message).Length);

            switch (ConfigurationManager.AppSettings["EnvMode"])
            {
                case "0":
                case "1":
                    using (MemoryStream memstream = new MemoryStream(message))
                    {
                        try
                        {
                            using (ZipFile zip = ZipFile.Read(memstream))
                            {
                                //Check if there are some obsolete elements (deleted in Visual Designer)
                                if (zip[Program.GadgeterBoard.TemplateID + "/ObsoleteElements.zen"] != null)
                                {
                                    ZipEntry e = zip[Program.GadgeterBoard.TemplateID + "/ObsoleteElements.zen"];
                                    e.Extract(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, ExtractExistingFileAction.OverwriteSilently);
                                    //Delete elements also on the instance
                                    foreach (string s in File.ReadAllText(Path.Combine(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, Program.GadgeterBoard.TemplateID, "ObsoleteElements.zen")).Split(','))
                                    {
                                        //Check also if file exists. It may happen then some other dependency in same subfolder already deleted entire path
                                        if (!string.IsNullOrEmpty(s) && File.Exists(s))
                                        {
                                            //If this is dependency, then delete entire dependency directory "/Dependencies/x/1.0.0/y/x.dll"
                                            if (s.Replace(@"\", "/").IndexOf("/Dependencies/") > -1)
                                            {
                                                DirectoryInfo di = new FileInfo(s).Directory;
                                                while (di.Name.ToLower() != "dependencies")
                                                    di = di.Parent;

                                                di.Delete(true);
                                            }
                                            //If this is implementation, just delete file
                                            else
                                                File.Delete(s.Trim());
                                        }
                                    }
                                    zip.RemoveEntry(e);
                                    File.Delete(Path.Combine(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, Program.GadgeterBoard.TemplateID, "ObsoleteElements.zen"));
                                }
                                zip.ExtractAll(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                        catch (Exception ex)
                        {
                            PrintUpdateError(ex);
                            errorOccured = true;
                        }
                        finally
                        {
                            memstream.Close();
                        }
                    }
                    break;

                case "2":
                    try
                    {
                        File.WriteAllBytes(Path.Combine(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, "zenodys.zip"), message);
                        ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "unzip", Arguments = Path.Combine(Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName, "zenodys.zip") + " -d " + Directory.GetParent(Program.GadgeterBoard.TemplateRootDirectory).Parent.FullName };
                        Process proc = new Process() { StartInfo = startInfo, };
                        proc.Start();
                        proc.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        PrintUpdateError(ex);
                        errorOccured = true;
                    }
                    break;
            }

            if (errorOccured)
                return;

            if (ConfigurationManager.AppSettings["IsMqttEnabled"] == "1")
            {
                ZenMqtt.Publish(Program.TopicPrefix + "/info/updateDoneResponse", Encoding.UTF8.GetBytes(""));
                //Snappy problem - it doesn't recognize Settings.ini file
                if (File.Exists(Path.Combine(Program.GadgeterBoard.TemplateRootDirectory, "Settings.ini")))
                {
                    var parser = new FileIniDataParser();
                    IniData parsedData = parser.ReadFile(Path.Combine(Program.GadgeterBoard.TemplateRootDirectory, "Settings.ini"));
                    File.WriteAllText("UpdateProgress", parsedData["Update"]["UpdatedBy"]);
                }
                else
                    File.WriteAllText("UpdateProgress", string.Empty);
            }

            Console.WriteLine("UPDATE COMPLETE....RESTARTING....");
            StartUpdater();
        }
        #endregion

        #region GetIp
        static string GetIp(string ip)
        {
            string ValidIpAddressRegex = "^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9‌​]{2}|2[0-4][0-9]|25[0-5])$";    // IP validation 
            Regex r = new Regex(ValidIpAddressRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = r.Match(ip.Trim());
            if (m.Success)
                return ip;
            else
                return Dns.GetHostEntry(ip.Trim()).AddressList[0].ToString();
        }
        #endregion

        #region ConnectionThreadProc
        internal static void ConnectionThreadProc()
        {
            while (_runConnectionThreadProc)
            {
                lock (_connectionThreadProcSync)
                {
                    if (ZenMqtt == null || !ZenMqtt.IsConnected)
                        Connect();
                }
                Thread.Sleep(15000);
            }
        }
        #endregion

        #region UnsubscribeDebugEvent
        public static void UnsubscribeDebugEvent()
        {
            try
            {
                if (Program.GadgeterBoard != null)
                {
                    Program.GadgeterBoard.IsBreakpointMode = false;
                    Program.GadgeterBoard._tasks.m_BreakpointWait.Set();
                    Program.GadgeterBoard._tasks.DebugDataReceived -= MqttUtils.gadgeteerBoard_DebugDataReceived;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1000 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region StopBreakpoint
        public static void StopBreakpoint()
        {
            try
            {
                if (Program.GadgeterBoard != null)
                {
                    Program.GadgeterBoard.IsBreakpointMode = false;
                    Program.GadgeterBoard._tasks.m_BreakpointWait.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1001 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region ContinueWithBreakpoint
        public static void ContinueWithBreakpoint()
        {
            try
            {
                if (Program.GadgeterBoard != null)
                {
                    Program.GadgeterBoard._tasks.m_waitDebugger.Set();
                    Program.GadgeterBoard.IsBreakpointMode = true;
                    Program.GadgeterBoard._tasks.m_BreakpointWait.Set();
                    ZenMqtt.Publish("/breakpoint/stepover" + Program.TopicPrefix, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1001 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region PublishSnapshotElements
        //Get current state of template immediatelly. Don't wait for first plugin to complete.
        public static void PublishSnapshotElements()
        {
            try
            {
                if (Program.GadgeterBoard != null)
                {
                    Program.GadgeterBoard.IsBreakpointMode = false;
                    Program.GadgeterBoard._tasks.m_BreakpointWait.Set();
                    Program.GadgeterBoard._tasks.DebugDataReceived -= gadgeteerBoard_DebugDataReceived;
                    Program.GadgeterBoard._tasks.DebugDataReceived += gadgeteerBoard_DebugDataReceived;
                    List<ElementInfoData> elementsData = new List<ElementInfoData>();
                    foreach (DictionaryEntry element in Program.GadgeterBoard._tasks.m_AllElements)
                    {
                        IElement e = element.Value as IElement;
                        elementsData.Add(new ElementInfoData(e.ID, GetElementResult(e), e.Started, false, GetElementErrorMessage(e), GetElementResultType(e)));
                    }
                    elementsData.Sort(delegate (ElementInfoData p1, ElementInfoData p2) { return p2.Started.CompareTo(p1.Started); });
                    //Hack, make copy of pluginsData array, because serialization doesn't works well with obfuscated classes
                    ZenMqtt.Publish(Program.TopicPrefix + "/debug/send", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(MakeElementInfoCopy(elementsData))));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1001 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region PublishElementsData
        public static void PublishElementsData(Tasks task, ArrayList elementsToStart)
        {
            try
            {
                List<ElementInfoData> elementsData = new List<ElementInfoData>();
                foreach (DictionaryEntry p in task.m_AllElements)
                {
                    IElement plug = p.Value as IElement;
                    bool bMark = elementsToStart.Cast<IElement>().Any(arg => arg.ID == plug.ID);

                    elementsData.Add(new ElementInfoData(plug.ID, GetElementResult(plug), plug.Started, bMark, GetElementErrorMessage(plug), GetElementResult(plug)));
                    if (bMark)
                    {
                        foreach (DictionaryEntry currElement in task.m_AllElements)
                        {
                            if (plug.LoopLock == (currElement.Value as IElement).LoopLock && !elementsData[elementsData.Count - 1].CurrentLoopElements.Contains((currElement.Value as IElement)))
                                elementsData[elementsData.Count - 1].CurrentLoopElements.Add((currElement.Value as IElement).ID);
                        }
                    }
                }
                elementsData.Sort(delegate (ElementInfoData p1, ElementInfoData p2) { return p2.Started.CompareTo(p1.Started); });
                //Hack, make copy of pluginsData array, because serialization doesn't works well with obfuscated classes
                ZenMqtt.Publish(Program.TopicPrefix + "/debug/send", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(MakeElementInfoCopy(elementsData))));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1002 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region PublishFileInfos
        public static void PublishFileInfos()
        {
            try
            {
                dynamic fileInfos = new ExpandoObject();
                fileInfos.ElementsVersion = Program.ElementsVersion;
                fileInfos.Files = Directory.GetFiles("project", "*.*", SearchOption.AllDirectories);
                ZenMqtt.Publish(Program.TopicPrefix + "/info/fileListResponse", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fileInfos)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR 1101 : " + ex == null ? "" : ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
        #endregion

        #region MakeElementInfoCopy
        static List<dynamic> MakeElementInfoCopy(List<ElementInfoData> elementsData)
        {
            List<dynamic> copyArray = new List<dynamic>();
            foreach (ElementInfoData o in elementsData)
            {
                dynamic dataObject = new ExpandoObject();
                dataObject.ID = o.ID;
                dataObject.Started = o.Started;
                dataObject.Result = o.Result;
                dataObject.ResultType = o.ResultType;
                dataObject.Mark = o.Mark;
                dataObject.StartedJsFormat = o.StartedJsFormat;
                dataObject.CurrentLoopElements = o.CurrentLoopElements;
                dataObject.Error = o.Error;
                copyArray.Add(dataObject);
            }
            return copyArray;
        }
        #endregion
        #endregion
    }
}
