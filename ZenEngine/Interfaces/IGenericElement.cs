using System;
using System.Collections;
using System.Threading;
using ZenCommon;

namespace ComputingEngine
{
    public delegate void ElementEventHandler(object sender, params Object[] e);
    public delegate void ElementExceptionHandler(object sender, params Object[] e);

    public interface IGenericElement : IElement
    {
        #region events
        event ElementEventHandler ElementEvent;
        #endregion

        #region InstanceLock
        object InstanceLock { get; set; }
        #endregion

        DateTime LastCacheFill { get; set; }
        IGadgeteerBoard ParentBoard { get; set; }
        bool IsStarted { get; set; }
        bool UnregisterEvent { get; set; }
        AutoResetEvent PauseElement { get; }
        ArrayList ManuallyTriggeredStartElements { get; set; }
        ArrayList FirstLevelTrueChilds { get; set; }
        ArrayList FirstLevelFalseChilds { get; set; }
        ArrayList FirstLevelTrueElements { get; set; }
        ArrayList FirstLevelFalseElements { get; set; }
        string Operator { get; set; }

        void StopElementExecuting();
    }
}
