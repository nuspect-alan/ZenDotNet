using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using System.Threading;
using CommonInterfaces;
using System.Collections;

namespace ZenCommonNetCore
{
    public class ZenNativeHelpers
    {
        public enum NodeResultType
        {
            RESULT_TYPE_INT,
            RESULT_TYPE_BOOL,
            RESULT_TYPE_DOUBLE,
            RESULT_TYPE_CHAR_ARRAY,
            RESULT_TYPE_JSON_STRING
        }

        static Hashtable _nodes;
        static IGadgeteerBoard _elementBoard;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GetNodeProperty(string nodeId, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SetNodeProperty(string nodeId, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetNodeResultInfo(string nodeId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe public delegate void** GetNodeResult(string nodeId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe public delegate void ExecuteNode(string nodeId);

        static GetNodeProperty _getNodePropertyCallback;
        static SetNodeProperty _setNodePropertyCallback;
        static GetNodeResultInfo _getNodeResultInfoCallback;
        static GetNodeResult _getNodeResultCallback;
        static ExecuteNode _executeNodeCallback;

        public static void SetNodePropertyCallback(string nodeId, string key, string value)
        {
            _setNodePropertyCallback(nodeId, key, value);
        }

        public static string GetNodePropertyCallback(string nodeId, string key)
        {
            return Marshal.PtrToStringUTF8(_getNodePropertyCallback(nodeId, key));
        }

        public static int GetNodeResultInfoCallback(string nodeId)
        {
            return _getNodeResultInfoCallback(nodeId);
        }

        unsafe public static void** GetNodeResultCallback(string nodeId)
        {
            return _getNodeResultCallback(nodeId);
        }

        unsafe public static void ExecuteNodeCallback(string nodeId)
        {
            _executeNodeCallback(nodeId);
        }

        unsafe public static void InitManagedNodes(string currentNodeId, void** nodes, int nodesCount, string projectRoot, string projectId, GetNodeProperty getNodePropertyCallback, GetNodeResultInfo getNodeResultInfoCallback, GetNodeResult getnodeResultCallback, ExecuteNode executeNodeCallback, SetNodeProperty setNodePropertyCallback)
        {
            if (_nodes == null)
            {
                _nodes = new Hashtable();
                IntPtr ptr = (IntPtr)((IntPtr)nodes);
                for (int i = 0; i < nodesCount; i++)
                {
                    STRUCT_NODE node = (STRUCT_NODE)Marshal.PtrToStructure(ptr, typeof(STRUCT_NODE));
                    _nodes.Add(node.id, new Node(node.id));
                    ptr += Marshal.SizeOf(typeof(STRUCT_NODE));
                }
                _getNodePropertyCallback = getNodePropertyCallback;
                _getNodeResultInfoCallback = getNodeResultInfoCallback;
                _getNodeResultCallback = getnodeResultCallback;
                _executeNodeCallback = executeNodeCallback;
                _setNodePropertyCallback = setNodePropertyCallback;
            }

            (_nodes[currentNodeId] as IElement).IsManagedElement = true;

            if (_elementBoard == null)
                _elementBoard = new GadgeteerBoard(projectRoot, projectId);
        }

        public static void CopyManagedStringToUnmanagedMemory(string s, IntPtr output)
        {
            //convert the managed string into a unmanaged ANSI string
            IntPtr ptr = Marshal.StringToHGlobalAnsi(s);
            //get the bytes of the unmanaged string
            byte[] bytes = new byte[s.Length + 1];
            Marshal.Copy(ptr, bytes, 0, s.Length);
            //copy these bytes into myString
            Marshal.Copy(bytes, 0, output, bytes.Length);
            //free the unmanaged memory
            Marshal.FreeHGlobal(ptr);
        }

        public static Hashtable Nodes
        {
            get { return _nodes; }
        }

        public static IGadgeteerBoard ParentBoard
        {
            get { return _elementBoard; }
        }

    }
}
