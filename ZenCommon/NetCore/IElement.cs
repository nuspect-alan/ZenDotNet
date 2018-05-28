using System;
using System.Collections;
using System.Collections.Generic;

namespace ZenCommon
{
    public enum ElementStatus { STOPPED, RUNNING, ARRIVED }
    public interface IElement
    {

        #region Properties
        IElement IAmStartedYou { get; set; }

        string LastResult { get; set; }
        object LastResultBoxed { get; set; }
        bool IsConditionMet { get; set; }
        string ID { get; set; }
        string ErrorMessage { get; set; }
        int ErrorCode { get; set; }
        string DebugString { get; set; }
        bool DoDebug { get; set; }
        ArrayList DisconnectedElements { get; set; }
        DateTime Started { get; set; }
        bool IsManagedElement { get; set; }
        string ResultUnit { get; set; }
        string ResultSource { get; set; }
        #endregion

        #region Methods
        void SetElementProperty(string key, string value);
        string GetElementProperty(string key);
        void StartElement(Hashtable elements, bool Async);
        #endregion
    }
}
