using CommonInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ZenCommonNetCore
{
    public class GadgeteerBoard : IGadgeteerBoard
    {
        public GadgeteerBoard(string projectRoot, string projectId)
        {
            this.TemplateRootDirectory = projectRoot;
            this.TemplateID = projectId;
        }

        public void PublishInfoPrint(string elementId, string text, string type)
        {
           // Console.WriteLine("Not implemented");
        }

        public void PublishInfoPrint(string text, string type)
        {
            //Console.WriteLine("Not implemented");
        }

        public void PublishError(string elementId, string error)
        {
            //Console.WriteLine("Not implemented");
        }

        public void ClearProcesses()
        {
            //Console.WriteLine("Not implemented");
        }

        public void FireElementStopExecutingEvent(IElement element)
        {
            //Console.WriteLine("Not implemented");
        }

        public event OnReboot OnRebootEvent;
        public event OnElementStopExecuting OnElementStopExecutingEvent;
        public bool AuthenticateMqttWithPrivateKey { get; set; }
        public string MqttHost { get; set; }
        public string MqttSslProtocolVersion { get; set; }
        public int MqttPort { get; set; }
        public bool IsDebugEnabled { get; set; }
        public bool AttachDebugger { get; set; }
        public bool IsRemoteDebugEnabled { get; set; }
        public bool IsRemoteUpdateEnabled { get; set; }
        public string WorkstationID { get; set; }
        public string TemplateID { get; set; }
        public byte[] EncryptionKey { get; set; }
        public string TemplateRootDirectory { get; set; }
        public Hashtable Modules { get; }
        public string Relations { get; }
        public string Implementations { get; }
        public string Dependencies { get; }
        public bool IsBreakpointMode { get; set; }
        public void UnregisterMqttEvents()
        {
            //Console.WriteLine("Not implemented");
        }
    }
}
