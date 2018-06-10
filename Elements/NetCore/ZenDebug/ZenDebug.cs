/*************************************************************************
 * Copyright (c) 2015, 2018 Zenodys BV
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Contributors:
 *    Tomaž Vinko
 *   
 **************************************************************************/

using ZenCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenDebug());

            ZenNativeHelpers.InitManagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].PrintText(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement, ZenNativeHelpers.ParentBoard);
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
