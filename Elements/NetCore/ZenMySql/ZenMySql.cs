/*************************************************************************
 * Copyright (c) 2015, 2019 Zenodys BV
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

using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using ZenCommon;

namespace ZenMySql
{
#if NETCOREAPP2_0
    public class ZenMySql
#else
    public class ZenMySql : IZenAction, IZenCallable
#endif
    {
        #region Fields
        #region _script
        Dictionary<string, IZenCsScript> _scripts = new Dictionary<string, IZenCsScript>();
        #endregion

        #region _dynamicSpParams
        Hashtable _dynamicSpParams = new Hashtable();
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenMySql> _implementations = new Dictionary<string, ZenMySql>();
        #endregion

        unsafe public static void InitUnmanagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenMySql());

            ZenNativeHelpers.InitUnmanagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].Execute(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
#else
        #region IZenCallable Implementations
        #region Functions
        public object Call(string actionID, Hashtable param)
        {
            switch (actionID)
            {
                case "ADD_SP_PARAM":
                    foreach (DictionaryEntry pair in param)
                        _dynamicSpParams[pair.Key] = pair.Value;
                    break;

                case "GET_SP_PARAM":
                    foreach (DictionaryEntry pair in param)
                        return _dynamicSpParams[pair.Key];
                    break;

                case "CLEAR_SP_PARAMS":
                    _dynamicSpParams.Clear();
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
            MySqlConnection conn = null;
            try
            {
                CultureInfo tmpCultureInfo = Thread.CurrentThread.CurrentCulture;
                CultureInfo tmpUiCultureInfo = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

                string port = !string.IsNullOrEmpty(element.GetElementProperty("PORT")) ? "PORT=" + element.GetElementProperty("PORT").Trim() + ";" : string.Empty;
                string connectionString = "SERVER=" + element.GetElementProperty("SERVER") + ";" + port + "DATABASE=" + element.GetElementProperty("DATABASE") + ";" + "UID=" + element.GetElementProperty("UID") + ";" + "PASSWORD=" + element.GetElementProperty("PASSWORD") + ";";

                using (conn = new MySqlConnection(connectionString))
                {
                    using (MySqlCommand command = new MySqlCommand())
                    {
                        command.Connection = conn;
                        switch (element.GetElementProperty("COMMAND_TYPE"))
                        {
                            case "1":
                                command.CommandType = CommandType.Text;
                                command.CommandText = element.GetElementProperty("SQL_STATEMENT");
                                break;

                            case "4":
                                command.CommandType = CommandType.StoredProcedure;
                                command.CommandText = element.GetElementProperty("STORED_PROCEDURE");
                                if (_dynamicSpParams.Count > 0)
                                {
                                    foreach (DictionaryEntry kvp in _dynamicSpParams)
                                    {
                                        string preFeedbackScript = "Hashtable ht = new Hashtable();";
                                        preFeedbackScript += "ht.Add(\"" + kvp.Key.ToString() + "\",null);";
                                        string feedbackScript = "_callable.Call(\"GET_SP_PARAM\",ht)";
                                        AddMySqlParam(kvp.Key.ToString(), feedbackScript, preFeedbackScript, elements, element, command);
                                    }
                                }
                                else if (!string.IsNullOrEmpty(element.GetElementProperty("MYSQL_PARAMETERS")))
                                {
                                    foreach (string s in element.GetElementProperty("MYSQL_PARAMETERS").Split('¨'))
                                    {
                                        if (!string.IsNullOrEmpty(s))
                                            AddMySqlParam(s.Split('°')[0], s.Split('°')[1], null, elements, element, command);
                                    }
                                }
                                break;
                            case "512":
                                throw new Exception("ZenMySql : Table direct not supported yet!");
                        }

                        conn.Open();
                        MySqlDataReader reader = command.ExecuteReader();
                        Hashtable results = new Hashtable();
                        if (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                                results.Add(reader.GetName(i), reader[i]);
                        }
                        conn.Close();
                        if (element.GetElementProperty("DO_DEBUG") == "1")
                        {
                            foreach (DictionaryEntry pair in results)
                                Console.WriteLine("{0}={1}", pair.Key, pair.Value);
                        }
                        element.LastResultBoxed = results;
                    }
                }
                Thread.CurrentThread.CurrentCulture = tmpCultureInfo;
                Thread.CurrentThread.CurrentUICulture = tmpUiCultureInfo;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                    Console.WriteLine(ex.InnerException.Message);
                else if (!string.IsNullOrEmpty(ex.Message))
                    Console.WriteLine(ex.Message);
                else
                    Console.WriteLine("Unknown Error in " + element.ID);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();

                element.IsConditionMet = true;
            }
        }
        #endregion

        #region AddMySqlParam
        void AddMySqlParam(string key, string scriptText, string preScriptText, Hashtable elements, IElement element, MySqlCommand command)
        {
            if (!_scripts.ContainsKey(key))
            {
                try
                {
                    //_scripts.Add(key, new ZenCsScriptCore().Initialize(null, scriptText, preScriptText, elements, element, 0, Path.Combine("tmp", "MySqlParams"), key, this, ParentBoard, element.GetElementProperty("PRINT_CODE") == "1"));

                }
                catch (Exception ex)
                {
                    string error = string.Empty;
                    if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                        error = ex.InnerException.Message;
                    else
                        error = ex.Message;

                    throw new Exception("Error in parsing : " + scriptText + Environment.NewLine + error);
                }
            }
            //command.Parameters.AddWithValue(key, _scripts[key].RunFeedbackScript());
        }
    }
    #endregion
    #endregion
}

