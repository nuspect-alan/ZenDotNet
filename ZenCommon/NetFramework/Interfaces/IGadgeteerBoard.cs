using System;
using System.Collections;
using System.Threading;

namespace CommonInterfaces
{
    public delegate void OnElementStopExecuting(object sender, IElement args);
    public delegate void OnReboot(object sender, EventArgs arg);

    public interface IGadgeteerBoard
    {
        void PublishInfoPrint(string elementId, string text, string type);
        void PublishInfoPrint(string text, string type);
        void PublishError(string elementId, string error);
        void ClearProcesses();
        void FireElementStopExecutingEvent(IElement element);
        event OnReboot OnRebootEvent;    
        event OnElementStopExecuting OnElementStopExecutingEvent;
        bool AuthenticateMqttWithPrivateKey { get; set; }
        string MqttHost { get; set; }
        string MqttSslProtocolVersion { get; set; }
        int MqttPort { get; set; }
        bool IsDebugEnabled { get; set; }
        bool AttachDebugger { get; set; }
        bool IsRemoteDebugEnabled { get; set; }
        bool IsRemoteUpdateEnabled { get; set; }
        string WorkstationID { get; set; }
        string TemplateID { get; set; }
        byte[] EncryptionKey { get; set; }
        string TemplateRootDirectory { get; }
        Hashtable Modules { get; }
        string Relations { get; }
        string Implementations { get; }
        string Dependencies { get; }
        bool IsBreakpointMode { get; set; }
        void UnregisterMqttEvents();
    }
}
