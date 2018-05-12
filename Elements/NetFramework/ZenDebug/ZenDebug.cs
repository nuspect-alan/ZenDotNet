using CommonInterfaces;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
#if NETCOREAPP2_0
using ZenCommonNetCore;
#endif

namespace ZenDebug
{
#if NETCOREAPP2_0
    public class ZenDebug
#else
    public class ZenDebug : IZenAction
#endif
    {
        #region Fields
        #region _syncCsScript
        object _syncCsScript = new object();
        #endregion

        #region _scriptData
        ZenCsScriptData _scriptData;
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenDebug> _implementations = new Dictionary<string, ZenDebug>();
        #endregion

        unsafe public static void InitManagedNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, ZenNativeHelpers.GetNodeProperty getNodePropertyCallback, ZenNativeHelpers.GetNodeResultInfo getNodeResultInfoCallback, ZenNativeHelpers.GetNodeResult getNodeResultCallback, ZenNativeHelpers.ExecuteNode executeNodeCallback, ZenNativeHelpers.SetNodeProperty setNodeProperty)
        {
            if (!_implementations.ContainsKey(currentNodeId))
                _implementations.Add(currentNodeId, new ZenDebug());

            ZenNativeHelpers.InitManagedNodes(currentNodeId, nodes, nodesCount, projectRoot, projectId, getNodePropertyCallback, getNodeResultInfoCallback, getNodeResultCallback, executeNodeCallback, setNodeProperty);
        }
        unsafe public static void ExecuteAction(string currentNodeId, void** nodes, int nodesCount, IntPtr result)
        {
            _implementations[currentNodeId].PrintText(ZenNativeHelpers.Nodes, ZenNativeHelpers.Nodes[currentNodeId] as IElement, ZenNativeHelpers.ParentBoard);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
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
            PrintText(elements, element, ParentBoard);
        }
        #endregion
        #endregion
        #endregion
#endif

        #region Functions
        #region PrintText
        void PrintText(Hashtable elements, IElement element, IGadgeteerBoard ParentBoard)
        {
            lock (_syncCsScript)
            {
                if (_scriptData == null)
                    _scriptData = ZenCsScriptCore.Initialize(element.GetElementProperty("TEXT"), elements, element, GetCachePath(element), ParentBoard, false);
            }
            string text = ZenCsScriptCore.GetCompiledText(element.GetElementProperty("TEXT"), _scriptData, elements, element, ParentBoard, GetCachePath(element), false);
            Console.WriteLine(text);
            ParentBoard.PublishInfoPrint(text, "info");
            element.IsConditionMet = true;
        }
        #endregion

        #region GetCachePath
        string GetCachePath(IElement element)
        {
            return Path.Combine("tmp", "Debug", element.ID + ".zen");
        }
        #endregion
        #endregion
    }
}
