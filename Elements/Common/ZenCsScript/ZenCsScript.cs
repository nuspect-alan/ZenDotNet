using HtmlAgilityPack;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using ZenCommon;
using System;

namespace ZenCsScript
{
#if NETCOREAPP2_0
    public class ZenCsScript
#else
    public class ZenCsScript : IZenAction, IZenElementInit, IZenExecutor
#endif
    {
        #region Fields
        #region _scriptData
        ZenCsScriptData _scriptData;
        #endregion

        #region _syncCsScript
        object _syncCsScript = new object();
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenCsScript> _implementations = new Dictionary<string, ZenCsScript>();
        #endregion

        unsafe public static string GetDynamicElements(string currentElementId, void** elements, int elementsCount, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement execElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty)
        {
            string pluginsToExecute = string.Empty;

            ZenNativeHelpers.InitManagedElements(currentElementId, elements, elementsCount, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, execElementCallback, setElementProperty);
            foreach (Match match in Regex.Matches((ZenNativeHelpers.Elements[currentElementId] as IElement).GetElementProperty("SCRIPT_TEXT").Replace("&quot;", "\""), @"exec(.*?);"))
            {
                var elementMatch = match.Groups[1].Value;
                int i = 0;
                while (i < elementMatch.Length && elementMatch[i] != '"') i++;

                i++;
                string elementId = string.Empty;
                while (i < elementMatch.Length && elementMatch[i] != '"')
                {
                    elementId += elementMatch[i].ToString();
                    i++;
                }
                pluginsToExecute += elementId.Trim() + ",";
            }
            return pluginsToExecute;
        }

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenCsScript());
        }
        unsafe public static void OnElementInit(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            lock (_implementations[currentElementId]._syncCsScript)
            {
                _implementations[currentElementId]._scriptData = ZenCsScriptCore.Initialize((ZenNativeHelpers.Elements[currentElementId] as IElement).GetElementProperty("SCRIPT_TEXT"), ZenNativeHelpers.Elements, (ZenNativeHelpers.Elements[currentElementId] as IElement), Path.Combine("tmp", "CsScript", (ZenNativeHelpers.Elements[currentElementId] as IElement).ID + ".zen"), ZenNativeHelpers.ParentBoard, (ZenNativeHelpers.Elements[currentElementId] as IElement).GetElementProperty("PRINT_CODE") == "1");
            }
        }

        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            //Set result here, because can user set it in script
            (ZenNativeHelpers.Elements[currentElementId] as IElement).IsConditionMet = true;
            _implementations[currentElementId]._scriptData.ZenCsScript.RunCustomCode(_implementations[currentElementId]._scriptData.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
#else
        #region IZenExecutor implementations
        public List<string> GetElementsToExecute(IElement plugin, Hashtable elements)
        {
            List<string> pluginsToExecute = new List<string>();
            foreach (Match match in Regex.Matches(plugin.GetElementProperty("SCRIPT_TEXT").Replace("&quot;", "\""), @"exec(.*?);"))
            {
                var elementMatch = match.Groups[1].Value;
                int i = 0;
                while (i < elementMatch.Length && elementMatch[i] != '"') i++;

                i++;
                string elementId = string.Empty;
                while (i < elementMatch.Length && elementMatch[i] != '"')
                {
                    elementId += elementMatch[i].ToString();
                    i++;
                }
                pluginsToExecute.Add(elementId.Trim());
            }
            return pluginsToExecute;
        }
        #endregion

        #region IZenElementInit implementations
        #region OnElementInit
        public void OnElementInit(Hashtable elements, IElement element)
        {
            lock (_syncCsScript)
            {
                _scriptData = ZenCsScriptCore.Initialize(element.GetElementProperty("SCRIPT_TEXT"), elements, element, Path.Combine("tmp", "CsScript", element.ID + ".zen"), ParentBoard, element.GetElementProperty("PRINT_CODE") == "1");
                if (_scriptData == null)
                    (ParentBoard as IZenMqtt).PublishError(element.ID, element.GetElementProperty("SCRIPT_TEXT"));
            }
        }
        #endregion
        #endregion

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
            //Set result here, because can user set it in script
            element.IsConditionMet = true;
            _scriptData.ZenCsScript.RunCustomCode(_scriptData.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value);
        }
        #endregion
        #endregion
        #endregion
#endif
    }
}