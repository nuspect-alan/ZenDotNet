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

using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using ZenCommon;

namespace ZenHttpRequest
{
#if NETCOREAPP2_0
    public class ZenHttpRequest
#else
    public class ZenHttpRequest : IZenAction, IZenCallable
#endif
    {
        #region Fields
        #region _scriptHeaders
        ZenCsScriptData _scriptHeaders;
        #endregion

        #region _scriptUrl
        ZenCsScriptData _scriptUrl;
        #endregion

        #region _scriptBody
        ZenCsScriptData _scriptBody;
        #endregion

        #region _syncCsScript
        object _syncCsScript = new object();
        #endregion

        #region _dynamicHeaders
        Hashtable _dynamicHeaders = new Hashtable();
        #endregion

        #region _sync
        static object _sync = new object();
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region _implementations
        static Dictionary<string, ZenHttpRequest> _implementations = new Dictionary<string, ZenHttpRequest>();
        #endregion

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenHttpRequest());

            ZenNativeHelpers.InitManagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].MakeRequest(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement, ZenNativeHelpers.ParentBoard);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
#else
        #region IZenCallable Implementations
        #region Functions
        public object Call(string actionID, Hashtable param)
        {
            switch (actionID)
            {
                case "SET_HEADER":
                    foreach (DictionaryEntry pair in param)
                        _dynamicHeaders[pair.Key] = pair.Value;
                    break;

                case "REMOVE_HEADER":
                    _dynamicHeaders.Remove(param);
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
            MakeRequest(elements, element, ParentBoard);
        }
        #endregion
        #endregion
        #endregion
#endif

        #region Functions
        #region MakeRequest
        void MakeRequest(Hashtable elements, IElement element, IGadgeteerBoard parentBoard)
        {
            lock (_sync)
            {
                try
                {
                    bool bIsSuccessStatusCode = false;
                    string statusCode = string.Empty;

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(element.GetElementProperty("ACCEPT")));
                        AddHeaders(element, elements, client, parentBoard);

                        switch (element.GetElementProperty("METHOD").ToUpper().Trim())
                        {
                            case "POST":
                                using (var response = client.PostAsync(GetUrl(element.GetElementProperty("URL"), elements, element, parentBoard), GetBody(element.GetElementProperty("HTTP_BODY"), elements, element, parentBoard)).Result)
                                {
                                    bIsSuccessStatusCode = response.IsSuccessStatusCode;
                                    statusCode = response.StatusCode.ToString();
                                    AssignResult(element, response);
                                }
                                break;

                            case "GET":
                                using (var response = client.GetAsync(GetUrl(element.GetElementProperty("URL"), elements, element, parentBoard)).Result)
                                {
                                    bIsSuccessStatusCode = response.IsSuccessStatusCode;
                                    statusCode = response.StatusCode.ToString();
                                    AssignResult(element, response);
                                }
                                break;
                        }
                    }

                    if (element.GetElementProperty("DO_DEBUG") == "1")
                    {
                        DataSet ds = element.LastResultBoxed as DataSet;
                        if (ds != null)
                        {
                            Console.WriteLine();
                            Console.WriteLine(element.ID + " result details");
                            for (int i = 0; i < ds.Tables.Count; i++)
                            {
                                Console.WriteLine("Table : " + ds.Tables[i].TableName);
                                Console.WriteLine("Columns: ");
                                for (int j = 0; j < ds.Tables[i].Columns.Count; j++)
                                {
                                    Console.WriteLine("Column name : " + ds.Tables[i].Columns[j].ColumnName);
                                    Console.WriteLine("Column type : " + ds.Tables[i].Columns[j].DataType.Name.ToString());
                                    Console.WriteLine("--------------------------------------");
                                }
                            }
                        }
                        else
                        {
                            parentBoard.PublishInfoPrint(element.ID, element.LastResult, "info");
                            Console.WriteLine(element.LastResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (element.GetElementProperty("DO_DEBUG") == "1")
                    {
                        if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                            Console.WriteLine(ex.InnerException.Message);
                        else
                            Console.WriteLine(ex.Message);
                    }
                    element.ErrorCode = 1;
                }
                finally
                {
                    element.IsConditionMet = true;
                }
            }
        }
        #endregion

        #region AddHeaders
        void AddHeaders(IElement element, Hashtable elements, HttpClient client, IGadgeteerBoard parentBoard)
        {
            foreach (DictionaryEntry kvp in _dynamicHeaders)
                client.DefaultRequestHeaders.Add(kvp.Key.ToString(), kvp.Value.ToString());

            lock (_syncCsScript)
            {
                if (_scriptHeaders == null)
                {
                    string sFunctions = string.Empty;
                    foreach (string s in element.GetElementProperty("HTTP_HEADERS").Split('¨'))
                    {
                        if (string.IsNullOrEmpty(s))
                            continue;

                        string key = s.Split('°')[0];
                        string value = s.Split('°')[1];
                        sFunctions += ZenCsScriptCore.GetFunction("return " + value + ";");
                    }
                    _scriptHeaders = ZenCsScriptCore.Initialize(sFunctions, elements, element, Path.Combine("tmp", "HttpRequest", element.ID + "_Headers.zen"), parentBoard, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }

            int ipos = 0;
            foreach (string s in element.GetElementProperty("HTTP_HEADERS").Split('¨'))
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                string key = s.Split('°')[0];
                string value = _scriptHeaders.ZenCsScript.RunCustomCode(_scriptHeaders.ScriptDoc.DocumentNode.Descendants("code").ElementAt(ipos).Attributes["id"].Value).ToString();
                client.DefaultRequestHeaders.Add(s.Split('°')[0], value);
                ipos++;
            }
        }
        #endregion

        #region AssignResult
        void AssignResult(IElement element, HttpResponseMessage response)
        {
            if (element.GetElementProperty("HAS_RESPONSE") == "1")
            {
                string sResponse = response.Content.ReadAsStringAsync().Result;
                if (element.GetElementProperty("ACCEPT").ToLower().IndexOf("json") > -1)
                {
                    if (element.GetElementProperty("CONVERT_TO_DATA_TABLE") == "1")
                    {
                        DataSet ds = new DataSet();
                        ds.ReadXml(JsonConvert.DeserializeXNode(string.Concat("{\"root\":", sResponse, "}"), "root").CreateReader(ReaderOptions.None));
                        element.LastResultBoxed = ds;
                    }
                    else if (element.GetElementProperty("CONVERT_TO_JSON_OBJECT") == "1")
                        element.LastResultBoxed = JsonConvert.DeserializeObject(sResponse) as JObject;
                }
                else
                    element.LastResultBoxed = sResponse;

                element.LastResult = sResponse;
            }
        }
        #endregion

        #region GetCacheUrlFileName
        string GetCacheUrlFileName(IElement element)
        {
            return Path.Combine("tmp", "HttpRequest", element.ID + "_URL.zen");
        }
        #endregion

        #region GetCacheBodyFileName
        string GetCacheBodyFileName(IElement element)
        {
            return Path.Combine("tmp", "HttpRequest", element.ID + "_Body.zen");
        }
        #endregion

        #region GetBody
        HttpContent GetBody(string script, Hashtable elements, IElement element, IGadgeteerBoard parentBoard)
        {
            lock (_syncCsScript)
            {
                if (_scriptBody == null)
                    _scriptBody = ZenCsScriptCore.Initialize(ZenCsScriptCore.GetFunction(string.Concat("return ", script, ";")), elements, element, GetCacheBodyFileName(element), null, parentBoard, element.GetElementProperty("PRINT_CODE") == "1");
            }

            switch (element.GetElementProperty("REQUEST_CONTENT_TYPE"))
            {
                case "STRING":
                    return new StringContent(_scriptBody.ZenCsScript.RunCustomCode(_scriptBody.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value).ToString(), Encoding.UTF8, element.GetElementProperty("CONTENT_TYPE"));

                case "FORM_URL_ENCODED":
                    throw new Exception("Url encoded content is not yet supported");

                case "BYTE_ARRAY":
                    return new ByteArrayContent((byte[])_scriptBody.ZenCsScript.RunCustomCode(_scriptBody.ScriptDoc.DocumentNode.Descendants("code").FirstOrDefault().Attributes["id"].Value));
            }
            return null;
        }
        #endregion

        #region GetUrl
        string GetUrl(string text, Hashtable elements, IElement element, IGadgeteerBoard parentBoard)
        {
            lock (_syncCsScript)
            {
                if (_scriptUrl == null)
                    _scriptUrl = ZenCsScriptCore.Initialize(text, elements, element, GetCacheUrlFileName(element), parentBoard, element.GetElementProperty("PRINT_CODE") == "1");
            }
            string url = ZenCsScriptCore.GetCompiledText(text, _scriptUrl, elements, element, parentBoard, GetCacheUrlFileName(element), element.GetElementProperty("PRINT_CODE") == "1");
            if (element.GetElementProperty("DO_DEBUG") == "1")
                Console.WriteLine(url);

            return url;
        }
        #endregion
        #endregion
    }
}
