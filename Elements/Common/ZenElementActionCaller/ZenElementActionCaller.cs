﻿using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenCommon;

namespace ZenElementActionCaller
{
#if NETCOREAPP2_0
    public class ZenElementActionCaller
#else
    public class ZenElementActionCaller : IZenAction
#endif
    {
        #region Fields
        #region _syncCsScript
        object _syncCsScript = new object();
        #endregion

        #region _scripts
        ZenCsScriptData _scripts;
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenElementActionCaller> _implementations = new Dictionary<string, ZenElementActionCaller>();
        #endregion

        #region Core implementations
        #region InitManagedElements
        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenElementActionCaller());
        }
        #endregion

        #region ExecuteAction
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].CallActions(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement, ZenNativeHelpers.ParentBoard);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
        #endregion
        #endregion
#else
        #region IZenAction Implementations
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
            CallActions(elements, element);
        }
        #endregion
        #endregion
        #endregion
#endif
        #region Functions
        #region CallActions
        void CallActions(Hashtable elements, IElement element, IGadgeteerBoard ParentBoard)
        {
            lock (_syncCsScript)
            {
                if (_scripts == null)
                {
                    string sFunctions = "";
                    foreach (string s in element.GetElementProperty("ACTIONS_TO_CALL").Split('¨'))
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            // Order
                            sFunctions += ZenCsScriptCore.GetFunction("return " + (!string.IsNullOrEmpty(s.Split('°')[5]) ? Convert.ToInt32(s.Split('°')[5]) : 0) + ";");
                            // Condition
                            sFunctions += ZenCsScriptCore.GetFunction("return " + s.Split('°')[4] + ";");
                            // Action call
                            string actionCall = "Hashtable ht = new Hashtable(); ht.Add(\"" + s.Split('°')[2] + "\"," + s.Split('°')[3] + ");";
                            actionCall += "call_element_action(\"" + s.Split('°')[0] + "\", \"" + s.Split('°')[1] + "\", ht);";
                            sFunctions += ZenCsScriptCore.GetProcedure(actionCall);
                        }
                    }
                    _scripts = ZenCsScriptCore.Initialize(sFunctions, elements, element, Path.Combine("tmp", "ElementActionCaller", element.ID + ".zen"), ParentBoard, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }

            int iCurrentOrder = 0;
            bool atLeastOneConditionTrue = false;
            for (int i = 0; i < _scripts.ScriptDoc.DocumentNode.Descendants("code").Count(); i += 3)
            {
                int order = (int)_scripts.ZenCsScript.RunCustomCode(_scripts.ScriptDoc.DocumentNode.Descendants("code").ElementAt(i).Attributes["id"].Value);
                if (iCurrentOrder != order)
                {
                    if (atLeastOneConditionTrue)
                        break;

                    atLeastOneConditionTrue = false;
                    iCurrentOrder = order;
                }

                bool condition = (bool)_scripts.ZenCsScript.RunCustomCode(_scripts.ScriptDoc.DocumentNode.Descendants("code").ElementAt(i + 1).Attributes["id"].Value);

                if (condition)
                {
                    _scripts.ZenCsScript.RunCustomCode(_scripts.ScriptDoc.DocumentNode.Descendants("code").ElementAt(i + 2).Attributes["id"].Value);
                    atLeastOneConditionTrue = true;
                }
            }
            element.IsConditionMet = true;
        }
        #endregion
        #endregion
    }
}
