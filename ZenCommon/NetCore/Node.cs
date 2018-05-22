using CommonInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ZenCommonNetCore
{
    public class Node : IElement
    {
        #region Constructor
        public Node(string id)
        {
            this.ID = id;
        }
        #endregion

        #region Fields
        #region _lastResultBoxed
        object _lastResultBoxed;
        #endregion

        #region _resultType
        int _resultType = -1;
        #endregion

        #region _ptrResult
        unsafe void** _ptrResult;
        unsafe void** PtrResultInstance
        {
            get
            {
                // Obtain pointer to node result. Warning - node result pointer must already be initialized in this step.
                // Unmanaged code is responsible to allocate pointer before this function call
                if (_ptrResult == null)
                    _ptrResult = ZenNativeHelpers.GetNodeResultCallback(this.ID);

                return _ptrResult;
            }
        }
        #endregion
        #endregion

        #region Properties
        public IElement IAmStartedYou { get; set; }

        public string LastResult { get; set; }

        public bool IsConditionMet { get; set; }
        public string ID { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public string DebugString { get; set; }
        public bool DoDebug { get; set; }
        public ArrayList DisconnectedElements { get; set; }
        public DateTime Started { get; set; }
        public bool IsManagedElement { get; set; }
        public string ResultSource { get; set; }
        public string ResultUnit { get; set; }

        unsafe public object LastResultBoxed
        {
            get
            {
                if (!this.IsManagedElement)
                {
                    if (_resultType < 0)
                        _resultType = ZenNativeHelpers.GetNodeResultInfoCallback(this.ID);

                    switch (_resultType)
                    {
                        case (int)ZenNativeHelpers.NodeResultType.RESULT_TYPE_INT:
                            return *((int*)PtrResultInstance[0]);

                        default:
                            break;
                    }
                    return null;
                }
                else
                    return _lastResultBoxed;
            }
            set
            {
                if (!this.IsManagedElement)
                {
                    switch (value.GetType().Name)
                    {
                        case "Int32":
                            *((int*)PtrResultInstance[0]) = (int)value;
                            break;

                        default:
                            Console.WriteLine("Type " + value.GetType().Name + " is not supported...");
                            break;
                    }
                }
                else
                    _lastResultBoxed = value;
            }
        }
        #endregion

        #region Methods
        public void SetElementProperty(string key, string value)
        {
            ZenNativeHelpers.SetNodePropertyCallback(this.ID, key, value);
        }

        public string GetElementProperty(string key)
        {
            return ZenNativeHelpers.GetNodePropertyCallback(this.ID, key);
        }

        public void StartElement(Hashtable elements, bool Async)
        {
            ZenNativeHelpers.ExecuteNodeCallback(this.ID);
        }
        #endregion
    }
}
