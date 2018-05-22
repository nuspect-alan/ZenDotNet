using CommonInterfaces;
using HtmlAgilityPack;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
#if NETCOREAPP2_0
using ZenCommonNetCore;
#endif
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

        unsafe public static string GetDynamicNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, ZenNativeHelpers.GetNodeProperty getNodePropertyCallback, ZenNativeHelpers.GetNodeResultInfo getNodeResultInfoCallback, ZenNativeHelpers.GetNodeResult getNodeResultCallback, ZenNativeHelpers.ExecuteNode execNodeCallback, ZenNativeHelpers.SetNodeProperty setNodeProperty)
        {
            string pluginsToExecute = string.Empty;

            ZenNativeHelpers.InitManagedNodes(currentNodeId, nodes, nodesCount, projectRoot, projectId, getNodePropertyCallback, getNodeResultInfoCallback, getNodeResultCallback, execNodeCallback, setNodeProperty);
            foreach (Match match in Regex.Matches((ZenNativeHelpers.Nodes[currentNodeId] as IElement).GetElementProperty("SCRIPT_TEXT").Replace("&quot;", "\""), @"exec(.*?);"))
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

        unsafe public static void InitManagedNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, ZenNativeHelpers.GetNodeProperty getNodePropertyCallback, ZenNativeHelpers.GetNodeResultInfo getNodeResultInfoCallback, ZenNativeHelpers.GetNodeResult getNodeResultCallback, ZenNativeHelpers.ExecuteNode executeNodeCallback)
        {
            if (!_implementations.ContainsKey(currentNodeId))
                _implementations.Add(currentNodeId, new ZenCsScript());
        }
        unsafe public static void OnElementInit(string currentNodeId, void** nodes, int nodesCount, IntPtr result)
        {
            lock (_implementations[currentNodeId]._syncCsScript)
            {
                _implementations[currentNodeId]._scriptData = ZenCsScriptCore.Initialize((ZenNativeHelpers.Nodes[currentNodeId] as IElement).GetElementProperty("SCRIPT_TEXT"), ZenNativeHelpers.Nodes, (ZenNativeHelpers.Nodes[currentNodeId] as IElement), Path.Combine("tmp", "CsScript", (ZenNativeHelpers.Nodes[currentNodeId] as IElement).ID + ".zen"), ZenNativeHelpers.ParentBoard, (ZenNativeHelpers.Nodes[currentNodeId] as IElement).GetElementProperty("PRINT_CODE") == "1");
            }
        }

        unsafe public static void ExecuteAction(string currentNodeId, void** nodes, int nodesCount, IntPtr result)
        {
            //Set result here, because can user set it in script
            (ZenNativeHelpers.Nodes[currentNodeId] as IElement).IsConditionMet = true;
            _implementations[currentNodeId]._scriptData.ZenCsScript.RunCustomCode(_implementations[currentNodeId]._scriptData.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value);
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