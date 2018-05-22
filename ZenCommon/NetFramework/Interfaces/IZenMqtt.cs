using System;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace CommonInterfaces
{
    public interface IZenMqtt
    {
        void PublishError(string elementId, string error);
        void Subscribe(string topic, byte qos);
        void Unsubscribe(string[] topics);
        void Publish(string topic, string message);
        void Publish(string topic, byte[] message);
        void Publish(string topic, byte[] message, byte qos);
        event MessageArrivedEventHandler MessageArrivedEvent;
        event OnRecconect OnReconnectEvent;
        event OnBeforeRecconect OnBeforeReconnectEvent;
    }

    public delegate void OnBeforeRecconect(object sender, EventArgs arg);
    public delegate void OnRecconect(object sender, EventArgs arg);
    public delegate void MessageArrivedEventHandler(object sender, MqttMsgPublishEventArgs e);

}
