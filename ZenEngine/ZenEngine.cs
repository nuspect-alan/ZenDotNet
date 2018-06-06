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
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using ZenCommon;

namespace ComputingEngine
{
    public class ZenEngine : IGadgeteerBoard, IZenMqtt
    {
        #region Fields
        #region TemplateKey
        internal string TemplateKey;
        #endregion

        #region _tasks
        internal Tasks _tasks;
        #endregion
        #endregion

        #region IZenMqtt Implementations
        #region Events
        public event MessageArrivedEventHandler MessageArrivedEvent;
        public event OnRecconect OnReconnectEvent;
        public event OnBeforeRecconect OnBeforeReconnectEvent;
        #endregion

        #region Functions
        #region Subscribe
        public void Subscribe(string topic, byte qos)
        {
            MqttUtils.Subscribe(topic, qos);
        }
        #endregion

        #region Unsubscribe
        public void Unsubscribe(string[] topics)
        {
            MqttUtils.ZenMqtt.Unsubscribe(topics);
        }
        #endregion

        #region PublishError
        public void PublishError(string elementId, string error)
        {
            if (this.IsErrorReportEnabled && MqttUtils.ZenMqtt != null && MqttUtils.ZenMqtt.IsConnected)
            {
                try
                {
                    Publish(string.Concat("err/", TemplateID, "/", elementId).Replace("#", "").Replace("+", ""), Encoding.UTF8.GetBytes(error));
                }
                catch { Console.WriteLine("ZENODYS SYS : Error repoting failed!"); }
            }
        }
        #endregion

        #region PublishInfoPrint
        public void PublishInfoPrint(string elementId, string text, string type)
        {
            if (MqttUtils.ZenMqtt != null && MqttUtils.ZenMqtt.IsConnected)
            {
                try
                {
                    Hashtable ht = new Hashtable();
                    ht.Add("text", text);
                    ht.Add("type", type);
                    if (!string.IsNullOrEmpty(elementId))
                        ht.Add("elementId", elementId);

                    Publish(string.Concat(Program.TopicPrefix, "/info/print"), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ht)));
                }
                catch { Console.WriteLine("ZENODYS SYS : Info print failed!"); }
            }
        }
        public void PublishInfoPrint(string text, string type)
        {
            PublishInfoPrint(null, text, type);
        }
        #endregion

        #region Publish
        public void Publish(string topic, string message)
        {
            MqttUtils.Publish(topic, message);
        }

        public void Publish(string topic, byte[] message)
        {
            MqttUtils.Publish(topic, message, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE);
        }

        public void Publish(string topic, byte[] message, byte qos)
        {
            MqttUtils.Publish(topic, message, qos);
        }
        #endregion
        #endregion
        #endregion

        #region IGadgeteerBoard Implementations
        #region Functions
        #region ClearProcesses
        public void ClearProcesses()
        {
            Process[] processes = Process.GetProcesses();
            if (File.Exists("pid"))
            {
                try
                {
                    string pids = File.ReadAllText("pid");
                    foreach (string s in pids.Split(';'))
                    {
                        if (!string.IsNullOrEmpty(s.Trim()))
                        {
                            string[] idName = s.Split(',');
                            foreach (Process p in processes)
                            {
                                if (p.Id == Convert.ToInt32(idName[0].Trim()) && p.ProcessName == idName[1])
                                    p.Kill();
                            }
                        }
                    }
                }
                catch { }
                File.Delete("pid");
            }
        }
        #endregion

        #region FireElementStopExecutingEvent
        public void FireElementStopExecutingEvent(IElement element)
        {
            if (OnElementStopExecutingEvent != null)
                OnElementStopExecutingEvent(this, element);
        }
        #endregion
        #endregion

        #region Events
        #region OnElementStopExecutingEvent
        public event OnElementStopExecuting OnElementStopExecutingEvent;
        #endregion
        #endregion

        #region Properties
        #region OnRebootEvent
        public event OnReboot OnRebootEvent;
        #endregion

        #region MqttHost
        public string MqttHost { get; set; }
        #endregion

        #region MqttSslProtocolVersion
        public string MqttSslProtocolVersion { get; set; }
        #endregion

        #region AuthenticateMqttWithPrivateKey
        public bool AuthenticateMqttWithPrivateKey { get; set; }
        #endregion

        #region MqttPort
        public int MqttPort { get; set; }
        #endregion

        #region UnregisterMqttEvents
        public void UnregisterMqttEvents()
        {
            MqttUtils.ZenMqtt.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
            MqttUtils.OnReconnectInternalEvent -= MqttUtils_OnReconnectEvent;
            MqttUtils.OnBeforeReconnectInternalEvent -= MqttUtils_OnBeforeReconnectEvent;
        }
        #endregion

        #region AttachDebugger
        public bool AttachDebugger
        {
            get;
            set;
        }
        #endregion

        #region IsBreakpointMode
        public bool IsBreakpointMode
        {
            get;
            set;
        }
        #endregion

        #region TemplateID
        public string TemplateID { get; set; }
        #endregion

        #region Modules
        public Hashtable Modules
        {
            get
            {
                return JsonConvert.DeserializeObject<Hashtable>(Utils.Decrypt(Path.Combine(TemplateRootDirectory, "DB", "Modules.zen"), this.TemplateKey));
            }
        }
        #endregion

        #region Relations
        public string Relations
        {
            get
            {
                return Utils.Decrypt(Path.Combine(TemplateRootDirectory, "DB", "Relations.zen"), this.TemplateKey);
            }
        }
        #endregion

        #region Implementations
        public string Implementations
        {
            get
            {
                return Utils.Decrypt(Path.Combine(TemplateRootDirectory, "DB", "Implementations.zen"), this.TemplateKey);
            }
        }
        #endregion

        #region Dependencies
        public string Dependencies
        {
            get
            {
                return Utils.Decrypt(Path.Combine(TemplateRootDirectory, "DB", "ImplementationDependencies.zen"), this.TemplateKey);
            }
        }
        #endregion

        #region TemplateRootDirectory
        public string TemplateRootDirectory { get; set; }
        #endregion

        #region WorkstationID
        public string WorkstationID { get; set; }
        #endregion

        #region EncryptionKey
        public byte[] EncryptionKey { get; set; }
        #endregion

        #region ConnectionTimeout
        public int ConnectionTimeout { get; set; }
        #endregion

        #region ConnectionTimeoutRetries
        public int ConnectionTimeoutRetries { get; set; }
        #endregion

        #region GPRSApn
        public string GPRSApn { get; set; }
        #endregion

        #region GPRSUserName
        public string GPRSUserName { get; set; }
        #endregion

        #region GPRSPassword
        public string GPRSPassword { get; set; }
        #endregion

        #region IsDebugEnabled
        public bool IsDebugEnabled { get; set; }
        #endregion

        #region IsRemoteDebugEnabled
        public bool IsRemoteDebugEnabled { get; set; }
        #endregion

        #region IsRemoteUpdateEnabled
        public bool IsRemoteUpdateEnabled { get; set; }
        #endregion

        #region IsRemoteRestartEnabled
        public bool IsRemoteRestartEnabled { get; set; }
        #endregion

        #region IsRemoteInfoEnabled
        public bool IsRemoteInfoEnabled { get; set; }
        #endregion

        #region IsErrorReportEnabled
        public bool IsErrorReportEnabled { get; set; }
        #endregion
        #endregion
        #endregion

        #region Public functions
        #region Start
        public void Start(List<string> PreStartErrors)
        {
            try
            {
                try
                {
                    ReadSettings();
                    Console.WriteLine("INSTANCE : " + Program.WorkstationName);
                    Program.StartMqtt();
                    RegisterEventsOnMqttBroker();
                    _tasks = new Tasks();
                    _tasks.StartElements(this);
                }
                catch (Exception ex)
                {
                    PreStartErrors.Add((ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message));
                }

                foreach (string s in PreStartErrors)
                {
                    Console.WriteLine(s);
                    PublishInfoPrint("Runtime", s, "error");
                }

                Console.WriteLine("END START...");

                PublishInfoPrint("Starting " + TemplateID + "...", "systemInfo");

                if (MqttUtils.ZenMqtt != null && MqttUtils.ZenMqtt.IsConnected)
                {
                    if (File.Exists("UpdateProgress"))
                    {
                        string user = File.ReadAllText("UpdateProgress");
                        MqttUtils.Publish(Program.TopicPrefix + "/info/updateRestartResponse", user);
                        File.Delete("UpdateProgress");
                    }
                    else if (File.Exists("RestartProgress"))
                    {
                        string user = File.ReadAllText("RestartProgress");
                        MqttUtils.Publish(Program.TopicPrefix + "/info/restartResponse", user);
                        File.Delete("RestartProgress");
                    }
                    MqttUtils.PublishTemplateState();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
                Console.WriteLine("Stack : " + ex.StackTrace);
            }
        }
        #endregion
        #endregion

        #region Private functions
        #region FireRebootEvent
        internal void FireRebootEvent()
        {
            if (this.OnRebootEvent != null)
                this.OnRebootEvent(this, new EventArgs());
        }
        #endregion

        #region ReadSettings
        private void ReadSettings()
        {
            var parser = new FileIniDataParser();
            IniData parsedData = parser.ReadFile(Path.Combine(TemplateRootDirectory, "Settings.ini"));
            this.WorkstationID = parsedData["Template"]["WorkstationID"];
            Program.WorkstationName = parsedData["Template"]["WorkstationName"];
            Program.ElementsVersion = parsedData["Elements"]["Version"];
            Program.Views = parsedData["Template"]["Views"];
            this.TemplateKey = parsedData["Template"]["Key"];

            int iPos = 0;
            this.EncryptionKey = new byte[16];
            foreach (string s in this.TemplateKey.Split(' '))
                if (!string.IsNullOrEmpty(s))
                    this.EncryptionKey[iPos++] = Convert.ToByte(s);

            this.IsErrorReportEnabled = string.IsNullOrEmpty(parsedData["Template"]["ErrorReportEnabled"]) ? false : parsedData["Template"]["ErrorReportEnabled"] == "1";

            this.MqttHost = parsedData["Mqtt"]["Host"];
            this.MqttSslProtocolVersion = parsedData["Mqtt"]["SslProtocolType"];
            this.MqttPort = string.IsNullOrEmpty(parsedData["Mqtt"]["Port"]) ? 0 : Convert.ToInt16(parsedData["Mqtt"]["Port"]);
            this.AuthenticateMqttWithPrivateKey = parsedData["Mqtt"]["AuthenticateWithPrivateKey"] == "1";

            this.IsDebugEnabled = parsedData["Debug"]["Enabled"] == "1";

            this.IsRemoteDebugEnabled = parsedData["RemoteOperations"]["Debug"] == "1";
            this.IsRemoteUpdateEnabled = parsedData["RemoteOperations"]["Update"] == "1";
            this.IsRemoteInfoEnabled = parsedData["RemoteOperations"]["Info"] == "1";
            this.IsRemoteRestartEnabled = parsedData["RemoteOperations"]["Restart"] == "1";

            this.AttachDebugger = parsedData["Debug"]["AttachDebugger"] == "1";
            Console.WriteLine("ELEMENTS VERSION : " + parsedData["Elements"]["Version"]);
        }
        #endregion

        #region RegisterEventsOnMqttBroker
        void RegisterEventsOnMqttBroker()
        {
            if (ConfigurationManager.AppSettings["IsMqttEnabled"] == "1")
            {
                MqttUtils.ZenMqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                MqttUtils.OnReconnectInternalEvent += MqttUtils_OnReconnectEvent;
                MqttUtils.OnBeforeReconnectInternalEvent += MqttUtils_OnBeforeReconnectEvent;
            }
        }
        #endregion
        #endregion

        #region Event Handlers
        #region client_MqttMsgPublishReceived
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (this.MessageArrivedEvent != null && !Program.SystemTopics.Contains(e.Topic))
                this.MessageArrivedEvent(this, e);
        }
        #endregion

        #region MqttUtils_OnBeforeReconnectEvent
        void MqttUtils_OnBeforeReconnectEvent(object sender, EventArgs args)
        {
            if (this.OnBeforeReconnectEvent != null)
                this.OnBeforeReconnectEvent(sender, args);

            MqttUtils.ZenMqtt.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
        }
        #endregion

        #region MqttUtils_OnReconnectEvent
        void MqttUtils_OnReconnectEvent(object sender, EventArgs args)
        {
            if (this.OnReconnectEvent != null)
                this.OnReconnectEvent(sender, args);

            MqttUtils.ZenMqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        }
        #endregion
        #endregion
    }
}
