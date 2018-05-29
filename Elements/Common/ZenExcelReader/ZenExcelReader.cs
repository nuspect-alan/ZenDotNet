﻿using ExcelDataReader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenCommon;

namespace ZenExcelReader
{
#if NETCOREAPP2_0
    public class ZenExcelReader
#else
    public class ZenExcelReader : IZenAction
#endif
    {
#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenExcelReader> _implementations = new Dictionary<string, ZenExcelReader>();
        #endregion

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenExcelReader());

            ZenNativeHelpers.InitManagedElements(currentElementId, elements, elementsCount, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].ReadFile(ZenNativeHelpers.Elements[currentElementId] as IElement);
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
            ReadFile(element);
        }
        #endregion
        #endregion
        #endregion
#endif
        #region Function
        void ReadFile(IElement element)
        {
            var file = new FileInfo(Path.Combine(Environment.CurrentDirectory, element.GetElementProperty("EXCEL_FILE_PATH")));
            using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, element.GetElementProperty("EXCEL_FILE_PATH")), FileMode.Open))
            {
                IExcelDataReader reader = null;
                if (file.Extension == ".xls")
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);

                else if (file.Extension == ".xlsx")
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                if (reader == null)
                    return; 
                
                element.LastResultBoxed = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = element.GetElementProperty("IS_FIRST_ROW_AS_COLUMN_NAMES") == "1"
                    }
                });
            }
        }
        #endregion
    }
}