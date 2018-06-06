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

using System;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ZenCommon
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
