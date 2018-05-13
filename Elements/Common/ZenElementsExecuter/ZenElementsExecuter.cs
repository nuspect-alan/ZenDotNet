using CommonInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
#if NETCOREAPP2_0
using ZenCommonNetCore;
#endif
namespace ZenElementsExecuter
{
#if NETCOREAPP2_0
    public class ZenElementsExecuter
#else
    public class ZenElementsExecuter : IZenAction, IZenCallable, IZenExecutor
#endif
    {
        #region Fields
        #region DynamicElements
        string DynamicElements;
        #endregion

        #region _scripts
        Dictionary<string, ZenCsScriptData> _scripts = new Dictionary<string, ZenCsScriptData>();
        #endregion

        #region _sync
        object _sync = new object();
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region Fields
        #region _implementations
        static Dictionary<string, ZenElementsExecuter> _implementations = new Dictionary<string, ZenElementsExecuter>();
        #endregion
        #endregion

        #region Core implementations
        #region GetDynamicNodes
        unsafe public static string GetDynamicNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, ZenNativeHelpers.GetNodeProperty getNodePropertyCallback, ZenNativeHelpers.GetNodeResultInfo getNodeResultInfoCallback, ZenNativeHelpers.GetNodeResult getNodeResultCallback, ZenNativeHelpers.ExecuteNode execNodeCallback, ZenNativeHelpers.SetNodeProperty setNodeProperty)
        {
            ZenNativeHelpers.InitManagedNodes(currentNodeId, nodes, nodesCount, projectRoot, projectId, getNodePropertyCallback, getNodeResultInfoCallback, getNodeResultCallback, execNodeCallback, setNodeProperty);
            string pluginsToExecute = string.Empty;
            foreach (string outer in (Regex.Split((ZenNativeHelpers.Nodes[currentNodeId] as IElement).GetElementProperty("ELEMENTS_TO_EXECUTE"), "#101#")))
            {
                if (!string.IsNullOrEmpty(Regex.Split(outer, "#100#")[0].Trim()))
                    pluginsToExecute += Regex.Split(outer, "#100#")[0].Trim() + ",";
            }
            return pluginsToExecute;
        }
        #endregion

        #region InitManagedNodes
        unsafe public static void InitManagedNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, ZenNativeHelpers.GetNodeProperty getNodePropertyCallback, ZenNativeHelpers.GetNodeResultInfo getNodeResultInfoCallback, ZenNativeHelpers.GetNodeResult getnodeResultCallback)
        {
            if (!_implementations.ContainsKey(currentNodeId))
                _implementations.Add(currentNodeId, new ZenElementsExecuter());
        }
        #endregion

        #region ExecuteAction
        unsafe public static void ExecuteAction(string currentNodeId, void** nodes, int nodesCount, IntPtr result)
        {
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(_implementations[currentNodeId].RunConditions(ZenNativeHelpers.Nodes, ZenNativeHelpers.Nodes[currentNodeId] as IElement, ZenNativeHelpers.ParentBoard), result);
        }
        #endregion
        #endregion

#else
        #region IZenExecutor Implementations
        public List<string> GetElementsToExecute(IElement plugin, Hashtable elements)
        {
            List<string> pluginsToExecute = new List<string>();

            foreach (string outer in Regex.Split( plugin.GetElementProperty("ELEMENTS_TO_EXECUTE"),"#101#"))
                pluginsToExecute.Add(Regex.Split( outer,"#100#")[0].Trim());

            return pluginsToExecute;
        }
        #endregion

        #region IZenCallable Implementations
        #region Functions
        public object Call(string actionID, Hashtable param)
        {
            switch (actionID)
            {
                case "SET_ELEMENTS_TO_EXECUTE":
                    foreach (DictionaryEntry pair in param)
                        DynamicElements += pair.Key.ToString() + "°true°0¨";
                    break;
            }
            return null;
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
            RunConditions(elements, element, ParentBoard);
        }
        #endregion
        #endregion
        #endregion
#endif

        #region Private functions
        #region RunConditions
#if NETCOREAPP2_0
        string RunConditions(Hashtable elements, IElement element, IGadgeteerBoard ParentBoard)
#else
        void RunConditions(Hashtable elements, IElement element, IGadgeteerBoard ParentBoard)
#endif
        {
            lock (_sync)
            {
                string elements2Execute = !string.IsNullOrEmpty(DynamicElements) ? DynamicElements : element.GetElementProperty("ELEMENTS_TO_EXECUTE");
                DynamicElements = string.Empty;
                foreach (string s in Regex.Split(elements2Execute, "#101#"))
                {
                    if (string.IsNullOrEmpty(s))
                        continue;

                    if (!_scripts.ContainsKey(Regex.Split(s, "#100#")[0]))
                    {
                        string id = Regex.Split(s, "#100#")[0];
                        string condition = Regex.Split(s, "#100#")[1];
                        int order = string.IsNullOrEmpty(Regex.Split(s, "#100#")[2]) ? 0 : Convert.ToInt32(Regex.Split(s, "#100#")[2]);
                        condition = string.IsNullOrEmpty(condition) ? "true" : condition;
                        //TO DO : Don't compile for true statements.....
                        _scripts.Add(id, ZenCsScriptCore.Initialize(ZenCsScriptCore.GetFunction(string.Concat("return ", condition, ";")), elements, element, order, Path.Combine("tmp", "ElementsExecuter", element.ID + "_" + id + ".zen"), null, ParentBoard, element.GetElementProperty("PRINT_CODE") == "1"));
                    }
                }

                int iCurrentOrder = 0;
                bool atLeastOneConditionTrue = false;
#if NETCOREAPP2_0
                string sPluginsToStart = string.Empty;
#else
                ArrayList elementsToStart = new ArrayList();
#endif

                if (element.GetElementProperty("DO_DEBUG") == "1")
                {
                    Console.WriteLine("");
                    Console.WriteLine("********" + element.ID + "*******");
                }

                bool error = false;
                foreach (KeyValuePair<string, ZenCsScriptData> item in _scripts.OrderBy(key => key.Value.ZenCsScript.Order))
                {
                    if (iCurrentOrder != item.Value.ZenCsScript.Order)
                    {
                        if (atLeastOneConditionTrue)
                            break;

                        atLeastOneConditionTrue = false;
                        iCurrentOrder = item.Value.ZenCsScript.Order;
                    }
#if NETCOREAPP2_0
                    error = RunCondition(item, elements, element, ref sPluginsToStart, ref atLeastOneConditionTrue, ParentBoard);
#else
                    error = RunCondition(item, elements, element, elementsToStart, ref atLeastOneConditionTrue, ParentBoard);
#endif
                    if (error)
                        break;
                }

                if (error)
                {
#if NETCOREAPP2_0
                    sPluginsToStart = string.Empty;
#else
                    elementsToStart.Clear();
#endif
                    int max = _scripts.Max(key => key.Value.ZenCsScript.Order);

#if NETCOREAPP2_0
                    foreach (KeyValuePair<string, ZenCsScriptData> item in _scripts.Where(c => c.Value.ZenCsScript.Order == max))
                        RunCondition(item, elements, element, ref sPluginsToStart, ref atLeastOneConditionTrue, ParentBoard);
#else
                    foreach (KeyValuePair<string, ZenCsScriptData> item in _scripts.Where(c => c.Value.ZenCsScript.Order == max))
                        RunCondition(item, elements, element, elementsToStart, ref atLeastOneConditionTrue, ParentBoard);
#endif
                }

                if (element.GetElementProperty("DO_DEBUG") == "1")
                    Console.WriteLine("");

                element.IsConditionMet = true;

#if NETCOREAPP2_0
                return sPluginsToStart;
#else
                element.DisconnectedElements = elementsToStart;
#endif
            }
        }
        #endregion

        #region RunCondition
#if NETCOREAPP2_0
        bool RunCondition(KeyValuePair<string, ZenCsScriptData> item, Hashtable elements, IElement element, ref string sPluginsToStart, ref bool atLeastOneConditionTrue, IGadgeteerBoard ParentBoard)
#else
            bool RunCondition(KeyValuePair<string, ZenCsScriptData> item, Hashtable elements, IElement element, ArrayList elementsToStart, ref bool atLeastOneConditionTrue, IGadgeteerBoard ParentBoard)
#endif
        {
            try
            {
                bool scriptResult = (bool)item.Value.ZenCsScript.RunCustomCode(item.Value.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value);
                if (element.GetElementProperty("DO_DEBUG") == "1")
                    Console.WriteLine(item.Key + " ==> " + scriptResult);

                if (scriptResult)
                {
#if NETCOREAPP2_0
                    sPluginsToStart += ((IElement)elements[item.Key]).ID + ",";
#else
                    elementsToStart.Add((IElement)elements[item.Key]);
#endif
                    atLeastOneConditionTrue = true;
                }
            }
            catch (Exception ex)
            {
                string error = (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message)) ? ex.InnerException.Message : ex.Message;
                Console.WriteLine("Error in parsing " + item.Key.ToUpper() + " : " + ZenCsScriptCore.Decode(item.Value.ZenCsScript.RawHtml) + Environment.NewLine + error);
                ParentBoard.PublishInfoPrint(element.ID, "Error in parsing " + item.Key.ToUpper() + " : " + ZenCsScriptCore.Decode(item.Value.ZenCsScript.RawHtml) + Environment.NewLine + error, "error");
                return true;
            }
            return false;
        }
        #endregion
        #endregion
    }
}
