﻿/*************************************************************************
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

#if NETCOREAPP2_0
using Microsoft.Extensions.DependencyInjection;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
#else
using Nancy.Hosting.Self;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Nancy;
using Nancy.Bootstrapper;
#endif
using ZenCommon;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace ZenWebServer
{
#if NETCOREAPP2_0
    public class ZenWebServer
#else
    public class ZenWebServer : IZenEvent, IZenElementInit, IZenExecutor
#endif
    {
        #region Constants
        #region ZEN_CODE
        const string ZEN_CODE =
                "public class ZenSystem" +
                "{" +
                "   public static ZenCommon.IElement _element; public static System.Collections.Hashtable _plugins;" +
                "   public static void exec(string elementId)" +
                "   {" +
                "       if (!_plugins.Contains(elementId))" +
                "       {" +
                "           System.Console.WriteLine(elementId + \" does not exists!\");" +
                "           return;" +
                "       }" +
                "       ((ZenCommon.IElement)_plugins[elementId]).IAmStartedYou = _element;" +
                "       ((ZenCommon.IElement)_plugins[elementId]).StartElement(_plugins, false);" +

                "   }" +

                "   public static object get_result(string elementId)" +
                "   {" +
                "       if (!_plugins.Contains(elementId))" +
                "       {" +
                "           System.Console.WriteLine(elementId + \" does not exists!\");" +
                "           return null;" +
                "       }" +
                "       return ((ZenCommon.IElement)_plugins[elementId]).LastResultBoxed;" +
                "   }" +

                "   public static void set_result(string elementId, object result)" +
                "   {" +
                "       if (!_plugins.Contains(elementId))" +
                "           System.Console.WriteLine(elementId + \" does not exists!\");" +
                "       else" +
                "           ((ZenCommon.IElement)_plugins[elementId]).LastResultBoxed = result;" +
                "   }" +
                "}";
        #endregion
        #endregion

#if NETCOREAPP2_0
        #region Fields
        #region _implementations
        static Dictionary<string, ZenWebServer> _implementations = new Dictionary<string, ZenWebServer>();
        #endregion
        #endregion

        #region Core implementations
        #region GetDynamicElements
        unsafe public static string GetDynamicElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            List<string> tmpElements = new List<string>();
            string pluginsToExecute = string.Empty;
            string confFile = string.Concat("ZenSystemContent", Path.DirectorySeparatorChar, "zeno-edge-conf.json");

            if (!File.Exists(confFile))
            {
                Console.WriteLine(string.Format("WARNING: File {0} does not exists....", confFile));
                return string.Empty;
            }
            JObject json = JsonConvert.DeserializeObject(File.ReadAllText(string.Concat("ZenSystemContent", Path.DirectorySeparatorChar, "zeno-edge-conf.json"))) as JObject;
            foreach (var e in json["widgets"])
            {
                foreach (var element in e["elements"])
                {
                    if (!tmpElements.Contains(element["elementName"].ToString().Trim()) && ZenNativeHelpers.Elements.ContainsKey(element["elementName"].ToString().Trim()))
                        tmpElements.Add(element["elementName"].ToString().Trim());

                    foreach (string dependency in element["nodeDependency"].ToString().Split('$'))
                    {
                        if (ZenNativeHelpers.Elements.ContainsKey(dependency.Trim()) && !tmpElements.Contains(dependency.Trim()))
                            tmpElements.Add(dependency.Trim());
                    }

                    if (element["triggers"] != null)
                    {
                        foreach (var trigger in element["triggers"])
                        {

                            if (ZenNativeHelpers.Elements.ContainsKey(trigger["action"].ToString().Trim()) && !tmpElements.Contains(trigger["action"].ToString().Trim()))
                                tmpElements.Add(trigger["action"].ToString().Trim());

                            if (ZenNativeHelpers.Elements.ContainsKey(trigger["flag"].ToString().Trim()) && !tmpElements.Contains(trigger["flag"].ToString().Trim()))
                                tmpElements.Add(trigger["flag"].ToString().Trim());
                        }
                    }
                }
            }
            foreach (string e in tmpElements)
                pluginsToExecute += e + ",";

            return pluginsToExecute;
        }
        #endregion

        #region InitManagedNodes
        unsafe public static void InitUnmanagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenWebServer());

            ZenNativeHelpers.InitUnmanagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);

            Assembly controllers = _implementations[currentElementId].GenerateControllerCode(ZenNativeHelpers.Elements[currentElementId] as IElement, ZenNativeHelpers.Elements);
            new Task(() => _implementations[currentElementId].StartServer(
                            ZenNativeHelpers.Elements[currentElementId] as IElement,
                            controllers)).Start();
        }
        #endregion
        #endregion

#else
        #region IZenExecutor Implementations
        public List<string> GetElementsToExecute(IElement plugin, Hashtable elements)
        {
            List<string> pluginsToExecute = new List<string>();
            JObject json = JsonConvert.DeserializeObject(File.ReadAllText(string.Concat("ZenSystemContent", Path.DirectorySeparatorChar, "zeno-edge-conf.json"))) as JObject;
            foreach (var widgetElements in json["widgets"])
            {
                foreach (var element in widgetElements["elements"])
                {
                    if (!pluginsToExecute.Contains(element["elementName"].ToString().Trim()) && elements.ContainsKey(element["elementName"].ToString().Trim()))
                        pluginsToExecute.Add(element["elementName"].ToString().Trim());

                    if (element["nodeDependency"] != null)
                    {
                        foreach (string dependency in element["nodeDependency"].ToString().Split('$'))
                        {
                            if (elements.ContainsKey(dependency.Trim()) && !pluginsToExecute.Contains(dependency.Trim()))
                                pluginsToExecute.Add(dependency.Trim());
                        }
                    }
                    if (element["triggers"] != null)
                    {
                        foreach (var trigger in element["triggers"])
                        {
                            if (elements.ContainsKey(trigger["action"].ToString().Trim()) && !pluginsToExecute.Contains(trigger["action"].ToString().Trim()))
                                pluginsToExecute.Add(trigger["action"].ToString().Trim());

                            if (elements.ContainsKey(trigger["flag"].ToString().Trim()) && !pluginsToExecute.Contains(trigger["flag"].ToString().Trim()))
                                pluginsToExecute.Add(trigger["flag"].ToString().Trim());
                        }
                    }
                }
            }
            return pluginsToExecute;
        }
        #endregion

        #region IZenEvent Implementations
        #region Properties
        #region ID
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

#if !NETCOREAPP2_0
        #region Events
        #region ModuleEvent
        public event ModuleEventHandler ModuleEvent;
        #endregion
        #endregion

        #region Functions
        #region CheckInterruptCondition
        public bool CheckInterruptCondition(ModuleEventData eventData, IElement element, Hashtable plugins)
        {
            return true;
        }
        #endregion
        #endregion
#endif
        #endregion

        #region IZenElementInit implementations
        #region OnElementInit
        public void OnElementInit(Hashtable elements, IElement element)
        {
            GenerateControllerCode(element, elements);
            new Task(() => StartServer(element)).Start();
        }
        #endregion
        #endregion
#endif

        #region Functions
#if NETCOREAPP2_0
        #region CreateWebHostBuilder
        public static IWebHostBuilder CreateWebHostBuilder(string[] args,
                        IElement element,
                        string uri,
                        Assembly controllers) =>

           WebHost.CreateDefaultBuilder(args)
                .UseUrls(uri)
                .UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), element.GetElementProperty("VIEWS_ROOT")))
                .ConfigureServices(servicesCollection =>
                {
                    servicesCollection.AddSingleton<ZenoConfigContainer>(new ZenoConfigContainer()
                    {
                        element = element,
                        env = ZenNativeHelpers.ParentBoard,
                        controllers = controllers
                    });
                })
               .UseStartup<Startup>();
        #endregion
#endif

        #region Startserver
        public void StartServer(IElement element, Assembly controllers)
        {
            try
            {
#if NETCOREAPP2_0
                string uri = element.GetElementProperty("BASE_URI").Trim().EndsWith("/") ? element.GetElementProperty("BASE_URI").Trim() : string.Concat(element.GetElementProperty("BASE_URI").Trim(), "/");
                CreateWebHostBuilder(new string[] { }, element, uri, controllers).Build().Run();

#else
                HostConfiguration hostConfigs = new HostConfiguration();
                hostConfigs.UrlReservations.CreateAutomatically = true;
                string uri = element.GetElementProperty("BASE_URI").Trim().EndsWith("/") ? element.GetElementProperty("BASE_URI").Trim() : string.Concat(element.GetElementProperty("BASE_URI").Trim(), "/");

                //using (NancyHost nancyHost = new NancyHost(new Uri(uri), new ZenBootstrapper(element), hostConfigs))
                using (NancyHost nancyHost = new NancyHost(new Uri(uri), new ZenBootstrapper(element)))
                {
                    nancyHost.Start();

                    ParentBoard.PublishInfoPrint(element.ID, "Server started at " + uri, "info");

                    Console.WriteLine("Server started at " + uri);
                    Thread.Sleep(Timeout.Infinite);
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #region GenerateControllerCode
#if NETCOREAPP2_0
        Assembly GenerateControllerCode(IElement element, Hashtable elements)
        {
            Assembly assembly = null;
            string filename = Path.Combine("tmp", "WebServer", "Controller_" + element.ID + ".zen");
            if (!File.Exists(filename))
            {
                List<MetadataReference> coreReferencesPaths = new List<MetadataReference>();
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Console")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Core")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Data")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Xml.Linq")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime.Extensions")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Threading.Tasks")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc.ViewFeatures")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc.Core")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("netstandard")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc.Abstractions")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("ZenCommon")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Newtonsoft.Json")).Location));
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Routing")).Location));

                coreReferencesPaths.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
                string rawModuleCode = string.Concat(element.GetElementProperty("NANCY_MODULE").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&#92;", "\\").Replace("&period;", ".").Replace("&apos;", "'").Replace("&comma;", ",").Replace("&amp;", "&"), Environment.NewLine);
                string code = string.Empty;
                string usings = GetUsings(rawModuleCode, ref code);

                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                options = options.WithOptimizationLevel(OptimizationLevel.Release);
                options = options.WithPlatform(Platform.AnyCpu);
                var tree = CSharpSyntaxTree.ParseText(string.Concat(usings, Environment.NewLine, code, Environment.NewLine, ZEN_CODE));
                var compilation = CSharpCompilation.Create("zenCompile", syntaxTrees: new[] { tree }, references: coreReferencesPaths.ToArray(), options: options);

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                             diagnostic.IsWarningAsError ||
                             diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        }
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                        if (!Directory.Exists(filename))
                            Directory.CreateDirectory(Path.GetDirectoryName(filename));

                        compilation.Emit(filename);
                    }
                }
            }
            else
                assembly = Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), filename));

            assembly.GetType("ZenSystem").GetField("_element").SetValue(this, element);
            assembly.GetType("ZenSystem").GetField("_plugins").SetValue(this, elements);
            return assembly;
        }
#else
        void GenerateControllerCode(IElement element, Hashtable elements)
        {
            var csc = new CSharpCodeProvider();
            var cp = new CompilerParameters() { GenerateExecutable = false, GenerateInMemory = true };
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            cp.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            cp.ReferencedAssemblies.Add(Path.Combine(Environment.CurrentDirectory, "ZenCommonNetFramework.dll"));
            cp.ReferencedAssemblies.Add(Path.Combine(Environment.CurrentDirectory, "Newtonsoft.Json.dll"));
            cp.ReferencedAssemblies.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy", "2.0.0.0", "Nancy.dll"));
            cp.ReferencedAssemblies.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy.Hosting.Self", "2.0.0.0", "Nancy.Hosting.Self.dll"));

            if (element.GetElementProperty("AUTHENTICATION") == "1")
                cp.ReferencedAssemblies.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy.Authentication.Forms", "2.0.0.0", "Nancy.Authentication.Forms.dll"));

            cp.ReferencedAssemblies.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy.ViewEngines.Spark", "2.0.0.0", "Nancy.ViewEngines.Spark.dll"));
            cp.ReferencedAssemblies.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Spark", "1.7.0.0", "Spark.dll"));

            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Nancy.dll")))
                File.Copy(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy", "2.0.0.0", "Nancy.dll"), Path.Combine(Environment.CurrentDirectory, "Nancy.dll"), true);

            if (element.GetElementProperty("AUTHENTICATION") == "1" && !File.Exists(Path.Combine(Environment.CurrentDirectory, "Nancy.Authentication.Forms.dll")))
                File.Copy(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "Nancy.Authentication.Forms", "2.0.0.0", "Nancy.Authentication.Forms.dll"), Path.Combine(Environment.CurrentDirectory, "Nancy.Authentication.Forms.dll"), true);

            string rawModuleCode = string.Concat(element.GetElementProperty("NANCY_MODULE").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&#92;", "\\").Replace("&period;", ".").Replace("&apos;", "'").Replace("&comma;", ",").Replace("&amp;", "&"), Environment.NewLine);
            string rawAuthenticationCode = string.Concat(element.GetElementProperty("AUTHENTICATION_CODE").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&#92;", "\\").Replace("&period;", ".").Replace("&apos;", "'").Replace("&comma;", ",").Replace("&amp;", "&"), Environment.NewLine);

            string code = string.Empty;
            string usings = GetUsings(rawModuleCode, ref code);
            usings += GetUsings(rawAuthenticationCode, ref code, "public class ZenAuthentication{", "}");

            var results = csc.CompileAssemblyFromSource(cp, string.Concat(usings, Environment.NewLine, code, Environment.NewLine, ZEN_CODE));
            if (results.Errors.HasErrors)
            {
                Console.WriteLine(element.ID + " errors : ");
                foreach (CompilerError error in results.Errors)
                    Console.WriteLine("Line : " + error.Line.ToString() + " " + error.ErrorText + "; ");
            }
            else
            {
                results.CompiledAssembly.GetType("ZenSystem").GetField("_element").SetValue(this, element);
                results.CompiledAssembly.GetType("ZenSystem").GetField("_plugins").SetValue(this, elements);
                UserMapper._compiledAssembly = results.CompiledAssembly;
            }
        }
#endif
        #endregion

        #region GetUsings
        static string GetUsings(string rawCode, ref string code, string prefix = "", string postfix = "")
        {
            var blockComments = @"/\*(.*?)\*/
        ";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            rawCode = Regex.Replace(rawCode, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                    me =>
                    {
                        if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                            return me.Value.StartsWith("//") ? Environment.NewLine : "";

                        return me.Value;
                    }, RegexOptions.Singleline).Trim();


            string userUsings = string.Empty;
            bool isUsingLoop = true;

            foreach (string line in rawCode.Split(';'))
            {
                if (line.Trim().StartsWith("using") && isUsingLoop)
                {
                    string[] values = Regex.Split(line, "using");
                    if (values.Length == 2)
                    {
                        if (values[0].Trim() == string.Empty)
                            userUsings += line.Trim() + ";";
                    }
                    else
                    {
                        code += prefix + line;
                        isUsingLoop = false;
                    }
                }
                else
                {
                    if (isUsingLoop)
                        code += prefix;

                    code += line.Trim() + ";";
                    isUsingLoop = false;
                }
            }
            if (code.IndexOf(';') > -1)
                code = code.Remove(code.LastIndexOf(';'));

            code += postfix;
            return userUsings;
        }
        #endregion
        #endregion
    }
}