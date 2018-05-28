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
using System.Collections;

namespace ZenCommon
{
    public class ZenNativeHelpers
    {
        public enum ElementResultType
        {
            RESULT_TYPE_INT,
            RESULT_TYPE_BOOL,
            RESULT_TYPE_DOUBLE,
            RESULT_TYPE_CHAR_ARRAY,
            RESULT_TYPE_JSON_STRING
        }

        static Hashtable _elements;
        static IGadgeteerBoard _elementBoard;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GetElementProperty(string elementId, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SetElementProperty(string elementId, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetElementResultInfo(string elementId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe public delegate void** GetElementResult(string elementId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe public delegate void ExecuteElement(string elementId);

        static GetElementProperty _getElementPropertyCallback;
        static SetElementProperty _setElementPropertyCallback;
        static GetElementResultInfo _getElementResultInfoCallback;
        static GetElementResult _getElementResultCallback;
        static ExecuteElement _executeElementCallback;

        public static void SetElementPropertyCallback(string elementId, string key, string value)
        {
            _setElementPropertyCallback(elementId, key, value);
        }

        public static string GetElementPropertyCallback(string elementId, string key)
        {
            return Marshal.PtrToStringUTF8(_getElementPropertyCallback(elementId, key));
        }

        public static int GetElementResultInfoCallback(string elementId)
        {
            return _getElementResultInfoCallback(elementId);
        }

        unsafe public static void** GetElementResultCallback(string elementId)
        {
            return _getElementResultCallback(elementId);
        }

        unsafe public static void ExecuteElementCallback(string elementId)
        {
            _executeElementCallback(elementId);
        }

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, string projectRoot, string projectId, GetElementProperty getElementPropertyCallback, GetElementResultInfo getElementResultInfoCallback, GetElementResult getElementResultCallback, ExecuteElement executeElementCallback, SetElementProperty setElementPropertyCallback)
        {
            if (_elements == null)
            {
                _elements = new Hashtable();
                IntPtr ptr = (IntPtr)((IntPtr)elements);
                for (int i = 0; i < elementsCount; i++)
                {
                    STRUCT_ELEMENT element = (STRUCT_ELEMENT)Marshal.PtrToStructure(ptr, typeof(STRUCT_ELEMENT));
                    _elements.Add(element.id, new Element (element.id));
                    ptr += Marshal.SizeOf(typeof(STRUCT_ELEMENT));
                }
                _getElementPropertyCallback = getElementPropertyCallback;
                _getElementResultInfoCallback = getElementResultInfoCallback;
                _getElementResultCallback = getElementResultCallback;
                _executeElementCallback = executeElementCallback;
                _setElementPropertyCallback = setElementPropertyCallback;
            }

            (_elements[currentElementId] as IElement).IsManagedElement = true;

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

        public static Hashtable Elements
        {
            get { return _elements; }
        }

        public static IGadgeteerBoard ParentBoard
        {
            get { return _elementBoard; }
        }

    }
}
