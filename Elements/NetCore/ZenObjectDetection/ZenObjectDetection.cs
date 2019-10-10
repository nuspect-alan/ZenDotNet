using System;
using System.Collections;
using System.Collections.Generic;
using ZenCommon;

namespace ZenObjectDetection
{
#if NETCOREAPP2_0
    public class ZenObjectDetection
#else
    public class ZenObjectDetection : IZenAction, IZenCallable
#endif
    {
        #region Fields
        ObjectDetection _objectDetection = new ObjectDetection();
        //#region _script
        //Dictionary<string, IZenCsScript> _scripts = new Dictionary<string, IZenCsScript>();
        //#endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenObjectDetection> _implementations = new Dictionary<string, ZenObjectDetection>();
        #endregion

        unsafe public static void InitUnmanagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenObjectDetection());

            ZenNativeHelpers.InitUnmanagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].Execute(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement);
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
            Execute(elements, element);
        }
        #endregion
        #endregion
        #endregion
#endif
        #region Functions
        #region Execute
        void Execute(Hashtable elements, IElement element)
        {
            _objectDetection.Detect();
            element.IsConditionMet = true;
        }
        #endregion
    }
    #endregion
    
}
