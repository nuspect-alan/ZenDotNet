using CommonInterfaces;
using System;
using System.Collections;

namespace ZenStart
{
    public class ZenStart : IZenStart, IZenAction
    {
        #region Fields
        #region IsStarted
        bool IsStarted;
        #endregion
        #endregion

        #region IZenStartable implementations
        #region Properties
        #region Dependencies
        public ArrayList Dependencies { get; set; }
        #endregion

        #region State
        public string State { get; set; }
        #endregion

        #region IsRepeatable
        public bool IsRepeatable { get; set; }
        #endregion

        #region IsActive
        public bool IsActive { get; set; }
        #endregion
        #endregion
        #endregion

        #region IZenAction implementations
        #region Properties
        #region ID
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

        #region Functions
        #region ExecuteAction
        public void ExecuteAction(Hashtable elements, IElement element, IElement iAmStartedYou)
        {
            element.IsConditionMet = !IsStarted || (IsStarted && (this as IZenStart).IsRepeatable);
            IsStarted = true;
            if (element.IsConditionMet)
                element.LastResultBoxed = DateTime.Now;
        }
        #endregion
        #endregion
        #endregion
    }
}
