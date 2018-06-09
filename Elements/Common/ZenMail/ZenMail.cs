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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ZenCommon;

namespace ZenMail
{
#if NETCOREAPP2_0
    public class ZenMail
#else
    public class ZenMail : IZenAction
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
        static Dictionary<string, ZenMail> _implementations = new Dictionary<string, ZenMail>();
        #endregion

        unsafe public static void InitManagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenMail());

            ZenNativeHelpers.InitManagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
        }
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].SendMail(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement, ZenNativeHelpers.ParentBoard);
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
            SendMail(elements, element, ParentBoard);
        }
        #endregion
        #endregion
        #endregion
#endif

        #region Functions
        #region SendMail
        void SendMail(Hashtable elements, IElement element, IGadgeteerBoard parentBoard)
        {
            MailMessage mail = new MailMessage(element.GetElementProperty("FROM"), element.GetElementProperty("TO"));
            SmtpClient client = new SmtpClient();
            client.Port = Convert.ToInt32(element.GetElementProperty("PORT"));
            client.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            client.Credentials = new NetworkCredential(element.GetElementProperty("USERNAME"), element.GetElementProperty("PASSWORD"));
            client.Host = element.GetElementProperty("HOST");
            mail.Subject = element.GetElementProperty("SUBJECT");
            mail.Body = GetBody(element.GetElementProperty("BODY"), Path.Combine("tmp", "Mail", element.ID + ".zen"), elements, element, element.GetElementProperty("PRINT_CODE") == "1", parentBoard);
            mail.IsBodyHtml = true;
            client.Send(mail);
            element.LastResult = mail.Body;
            element.IsConditionMet = true;
        }
        #endregion

        #region GetBody
        string GetBody(string text, string file, Hashtable elements, IElement element, bool debug, IGadgeteerBoard parentBoard)
        {
            lock (_syncCsScript)
            {
                if (_scriptData == null)
                    _scriptData = ZenCsScriptCore.Initialize(text, elements, element, file, parentBoard, debug);
            }
            return ZenCsScriptCore.GetCompiledText(text, _scriptData, elements, element, parentBoard, file, debug);
        }
        #endregion
        #endregion
    }
}
