using System;
using ZenCommon;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
#if NETCOREAPP2_0
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#else
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

public class ZenCsScriptCore
{
    #region Public 
    #region GetCompiledText
    public static string GetCompiledText(string text, ZenCsScriptData scriptData, Hashtable elements, IElement element, IGadgeteerBoard ParentBoard, string folder, bool printCode)
    {
        return GetCompiledText(text, scriptData, elements, element, ParentBoard, folder, string.Empty, printCode);
    }
    public static string GetCompiledText(string text, ZenCsScriptData scriptData, Hashtable elements, IElement element, IGadgeteerBoard ParentBoard, string folder, string fileName, bool printCode)
    {
        if (text.IndexOf("result") == -1 && text.IndexOf("last_executed_date") == -1 && text.IndexOf("error_code") == -1 && text.IndexOf("error_message") == -1 && text.IndexOf("status") == -1 && text.IndexOf("started") == -1 && text.IndexOf("ms_elapsed") == -1 && text.IndexOf("code") == -1)
            return ZenCsScriptCore.Decode(text);

        string outerHtml = scriptData.ScriptDoc.DocumentNode.OuterHtml;
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "code");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "result");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "status");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "ms_elapsed");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "last_executed_date");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "error_message");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "error_code");
        outerHtml = ReplaceNodeText(outerHtml, scriptData, "started");
        return ZenCsScriptCore.Decode(outerHtml);
    }
    #endregion

#if !NETCOREAPP2_0
    #region Build
    public static string Build(string rawScript, List<string> defaultReferences)
    {
        rawScript = RemoveComments(rawScript);
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(MakeZenoTagsValid(rawScript));
        HtmlNode headerNode = doc.DocumentNode.Element("header");
        string[] references = GetReferences(headerNode, defaultReferences, null).ToArray();

        string usings = string.Empty;
        if (headerNode != null)
        {
            usings = GetUsings(Decode(headerNode.InnerText));
            doc.DocumentNode.RemoveChild(headerNode);
        }

        string sCode = ZenCsScriptCore.GenerateCode(rawScript, null, null, 0, null, null, doc, null, false, usings);
        return ZenCsScriptCore.CompileAndSaveAssembly(sCode, null, null, null, references);
    }
    #endregion
#endif

    #region Initialize
    public static ZenCsScriptData Initialize(string rawScript, Hashtable elements, IElement element, string fileName, IGadgeteerBoard ParentBoard, bool debug)
    {
        return Initialize(rawScript, elements, element, 0, fileName, null, ParentBoard, debug);
    }

    public static ZenCsScriptData Initialize(string rawScript, Hashtable elements, IElement element, string fileName, IZenCallable callable, IGadgeteerBoard ParentBoard, bool debug)
    {
        return Initialize(rawScript, elements, element, 0, fileName, callable, ParentBoard, debug);
    }
    public static ZenCsScriptData Initialize(string rawScript, Hashtable elements, IElement element, int Order, string fileName, IZenCallable callable, IGadgeteerBoard ParentBoard, bool debug)
    {
        //Make zeno tags (result, code...) recognizable by Html doc parser
        HtmlDocument doc = null;
        if (!File.Exists(fileName))
        {
            doc = new HtmlDocument();
            doc.LoadHtml(MakeZenoTagsValid(RemoveComments(rawScript)));
            HtmlNode headerNode = doc.DocumentNode.Element("header");
            List<string> deaultReferences = new List<string>();
            //if native, read reference from current implementation folder
#if NETCOREAPP2_0
            deaultReferences.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Implementations", "ZenCsScriptCore.dll"));
            deaultReferences.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Implementations", "Newtonsoft.Json.dll"));
            deaultReferences.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Implementations", "ZenCommon.dll"));
#else
            deaultReferences.Add(Path.Combine(ParentBoard.TemplateRootDirectory, "Dependencies", "ZenCsScriptCore", "1.0.0.0", "ZenCsScriptCore.dll"));
            deaultReferences.Add(Path.Combine(Environment.CurrentDirectory, "Newtonsoft.Json.dll"));
            deaultReferences.Add(Path.Combine(Environment.CurrentDirectory, "ZenCommon.dll"));
#endif

            string[] references = GetReferences(headerNode, deaultReferences, ParentBoard).ToArray();

            string usings = string.Empty;
            if (headerNode != null)
            {
                usings = GetUsings(Decode(headerNode.InnerText));
                doc.DocumentNode.RemoveChild(headerNode);
            }

            //Replace result tags with element castings based on their current values and return code as string
            string generatedCode = GenerateCode(rawScript, elements, element, Order, fileName, callable, doc, ParentBoard, debug, usings);


#if NETCOREAPP2_0
            string coreClrReferences = string.Empty;
            foreach (string s in references)
                coreClrReferences += s + ",";

            string root = Directory.GetParent(Directory.GetParent(Path.Combine(ParentBoard.TemplateRootDirectory)).FullName).FullName;
            fileName = Path.Combine(root, fileName);
            string errors = CompileAndSaveAssembly(generatedCode, coreClrReferences, fileName);
#else
            string errors = CompileAndSaveAssembly(generatedCode, element, fileName, ParentBoard, references);
#endif
            if (!string.IsNullOrEmpty(errors))
            {
                element.LastResult = errors;
                Console.WriteLine(element.LastResult);
                ParentBoard.PublishInfoPrint(element.ID, element.LastResult, "error");
                return null;
            }
        }
        Assembly _compiledScript = Assembly.Load(File.ReadAllBytes(fileName));
        IZenCsScript script = GetScript(_compiledScript);

#if NETCOREAPP2_0
        script.Init(elements, element);
#else
        script.Init(elements, element, callable);
#endif
        //Load cached raw html
        if (doc == null)
        {
            doc = new HtmlDocument();
            //restore newlines back. They were masked because of property syntax error. For example:
            //string RawHtml
            //{
            //    get {
            //           return 
            //           "test
            //           string";
            //         }
            //}
            doc.LoadHtml(MakeZenoTagsValid(script.RawHtml.Replace("&n;", "\n").Replace("&rn;", "\r\n")));
        }
        return new ZenCsScriptData(script, doc);
    }
    #endregion

    #region GetFunction
    public static string GetFunction(string body)
    {
        return string.Concat(string.Concat("<code run=\"true\" type=\"function\">", body, "</code>"));
    }
    #endregion

    #region GetProcedure
    public static string GetProcedure(string body)
    {
        return string.Concat("<code run=\"true\" type=\"procedure\">", body, "</code>");
    }
    #endregion

    #region Decode
    public static string Decode(string original)
    {
        return original.Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&#92;", "\\").Replace("&period;", ".").Replace("&apos;", "'").Replace("&comma;", ",").Replace("&amp;", "&");
    }
    #endregion

    #region Encode
    public static string Encode(string original)
    {
        return original.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\\", "&#92;").Replace(".", "&period;").Replace("'", "&apos;").Replace(",", "&comma;");
    }
    #endregion
    #endregion

    #region Private
    #region ReplaceNodeText
    static string ReplaceNodeText(string outerHtml, ZenCsScriptData scriptData, string nodeName)
    {
        foreach (HtmlNode node in scriptData.ScriptDoc.DocumentNode.Descendants(nodeName))
        {
            if (node.Attributes["id"] != null)
                outerHtml = outerHtml.Replace(node.OuterHtml, scriptData.ZenCsScript.RunCustomCode(node.Attributes["id"].Value).ToString());
        }
        return outerHtml;
    }
    #endregion

    #region AddFunction
    static void AddFunction(HtmlNode node, string script, Dictionary<string, string> functions, IGadgeteerBoard ParentBoard, IElement element, bool debug)
    {
        if (node.ParentNode.Name != "code")
        {
            node.Attributes.Add("id", "Funct_" + Guid.NewGuid().ToString("N"));
            string tmp = node.OuterHtml.Replace(node.OuterHtml, script);
            functions.Add(node.Attributes["id"].Value, "return " + tmp + ";");
            if (debug)
                ParentBoard.PublishInfoPrint(element.ID, tmp, "info");
        }
    }
    #endregion

    #region GenerateCode
    /*
    1)
            <code run="true" type="procedure">
                var i = 0;
                Test(i);
            </code>
            Converted to:
            object Funct_123(object a)
            {
                var i = 0;
                Test(i);
                return null;
            }
          
    2)
            <code run="true" type="function">
                var i = 0;
                Test(i);
                return "abc";
            </code>

            Converted to:
            object Funct_123(object a)
            {
                var i = 0;
                Test(i);
                return "abc";
            }
          
    3)
            <code>
                string CastString(int i)
                {
                    return i.ToString();
                }
                string CastDouble(int i)
                {
                    return Convert.ToDouble (i);
                }
            </code>

            Converter to:
            string CastString(int i)
            {
                return i.ToString();
            }
        
            string CastDouble(int i)
            {
                return Convert.ToDouble (i);
            }

         */
    static string GenerateCode(string rawScript, Hashtable elements, IElement element, int Order, string fileName, IZenCallable callable, HtmlDocument doc, IGadgeteerBoard ParentBoard, bool debug, string usings)
    {
        Dictionary<string, string> functions = new Dictionary<string, string>();
        string userFunctions = string.Empty;

        //*********************  BEGIN INSIDE CODE TAGS LOGIC******************************************/
        foreach (HtmlNode node in doc.DocumentNode.Descendants("code"))
        {
            if (string.IsNullOrEmpty(node.InnerHtml))
                continue;

            string nodeContent = node.InnerHtml;
            foreach (HtmlNode tagNode in node.Descendants("result"))
                nodeContent = ReplaceResultTagWithCast(tagNode, elements, nodeContent, false);

            foreach (HtmlNode tagNode in node.Descendants("error_code"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).ErrorCode");

            foreach (HtmlNode tagNode in node.Descendants("error_message"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).ErrorMessage");

            foreach (HtmlNode tagNode in node.Descendants("status"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "(((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).Status).ToString()");

            foreach (HtmlNode tagNode in node.Descendants("started"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).Started");

            foreach (HtmlNode tagNode in node.Descendants("ms_elapsed"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).MsElapsed");

            foreach (HtmlNode tagNode in node.Descendants("last_executed_date"))
                nodeContent = nodeContent.Replace(tagNode.OuterHtml, "((IElement)_elements[\"" + tagNode.InnerHtml + "\"]).LastExecutionDate");

            if (node.Attributes.Contains("run") && node.Attributes["run"].Value == "true")
            {
                node.Attributes.Add("id", "Funct_" + Guid.NewGuid().ToString("N"));
                functions.Add(node.Attributes["id"].Value, Decode(!node.Attributes.Contains("type") || node.Attributes["type"].Value == "procedure" ? string.Concat(nodeContent, ";return null;") : nodeContent));
            }
            else
                userFunctions += Decode(nodeContent);

            if (debug)
                ParentBoard.PublishInfoPrint(element.ID, Decode(nodeContent), "info");
        }
        //**********************************************************************************************/

        //*********************  BEGIN OUTSIDE CODE TAGS LOGIC******************************************/
        /*
        Example of script input:
        "SELECT * FROM t_Table WHERE Col = '<result>MyElementID</result>'"
        Example of output:
        return "SELECT * FROM t_Table WHERE Col = Cast.ToString("ElementID")";
        */

        foreach (HtmlNode node in doc.DocumentNode.Descendants("result"))
        {
            if (node.ParentNode.Name != "code")
            {
                node.Attributes.Add("id", "Funct_" + Guid.NewGuid().ToString("N"));
                string tmp = ReplaceResultTagWithCast(node, elements, node.OuterHtml, false);
                functions.Add(node.Attributes["id"].Value, "return " + Decode(tmp) + ";");
                if (debug)
                    ParentBoard.PublishInfoPrint(element.ID, Decode(tmp), "info");
            }
        }

        foreach (HtmlNode node in doc.DocumentNode.Descendants("status"))
            AddFunction(node, "(((IElement)_elements[\"" + node.InnerHtml + "\"]).Status).ToString()", functions, ParentBoard, element, debug);

        foreach (HtmlNode node in doc.DocumentNode.Descendants("ms_elapsed"))
            AddFunction(node, "((IElement)_elements[\"" + node.InnerHtml + "\"]).MsElapsed", functions, ParentBoard, element, debug);

        foreach (HtmlNode node in doc.DocumentNode.Descendants("last_executed_date"))
            AddFunction(node, "((IElement)_elements[\"" + node.InnerHtml + "\"]).LastExecutionDate", functions, ParentBoard, element, debug);

        foreach (HtmlNode node in doc.DocumentNode.Descendants("error_code"))
            AddFunction(node, "((IElement)_elements[\"" + node.InnerHtml + "\"]).ErrorCode", functions, ParentBoard, element, debug);

        foreach (HtmlNode node in doc.DocumentNode.Descendants("error_message"))
            AddFunction(node, "((IElement)_elements[\"" + node.InnerHtml + "\"]).ErrorMessage", functions, ParentBoard, element, debug);

        foreach (HtmlNode node in doc.DocumentNode.Descendants("started"))
            AddFunction(node, "((IElement)_elements[\"" + node.InnerHtml + "\"]).Started", functions, ParentBoard, element, debug);
        //**********************************************************************************************/

        string sFunctions = string.Empty;
        foreach (KeyValuePair<string, string> function in functions)
            sFunctions += string.Concat("object ", function.Key, "() {", function.Value, "}");

        string customCodeWrapper = "public object RunCustomCode(string functionName){";
        foreach (KeyValuePair<string, string> s in functions)
            customCodeWrapper += "if (functionName == \"" + s.Key + "\") return " + s.Key + "();";

        customCodeWrapper += " return null;}";
        //For cache html string replace lt and gt back, so that next time will MakeZenoTagsValid parse correctly
        return GetCode(Order, string.Concat(sFunctions, userFunctions), customCodeWrapper, element, doc.DocumentNode.OuterHtml.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r\n", "&rn;").Replace("\n", "&n;"), usings);
    }
    #endregion

    #region MakeZenoTagsValid
    static string MakeZenoTagsValid(string html)
    {
        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)code(.*?)(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)/(\s*)code(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)header(.*?)(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)/(\s*)header(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)result(.+?)/result(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)error_code(.+?)/error_code(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)error_message(.+?)/error_message(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)status(.+?)/status(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)started(.+?)/started(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)ms_elapsed(.+?)/ms_elapsed(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        foreach (Match match in Regex.Matches(html, @"&lt;(\s*)last_executed_date(.+?)/last_executed_date(\s*)&gt;"))
            html = html.Replace(match.Value, Decode(match.Value));

        return html;
    }
    #endregion

    #region GetCode
    static string GetCode(int Order, string functions, string runCustomCodeWrapper, IElement element, string htmlDoc, string usings)
    {
        return
                        "using System;" +
                        "using ZenCommon;" +
                        "using Newtonsoft.Json;" +
                        "using Newtonsoft.Json.Linq;" +
                        "using System.Collections;" +
#if !NETCOREAPP2_0
                        "using System.Data;" +
                        "using System.Linq;" +
                        "using System.Globalization;" +
                        "using System.Threading;" +
                        "using System.Collections.Generic;" +
                        "using System.IO;" +
                        "using System.Text.RegularExpressions;" +
#endif
                        usings +

                        "namespace ZenCsScript" +
                        "{" +
                            "public class CsScript : IZenCsScript" +
                            "{" +
                                "IElement _element; Hashtable _elements;" +
#if !NETCOREAPP2_0
                                "IZenCallable _callable;" +
#endif
                                functions +

                                (string.IsNullOrEmpty(runCustomCodeWrapper) ? "public object RunCustomCode(string functionName){ return null; }" : runCustomCodeWrapper) +

                                "public string RawHtml" +
                                "{" +
                                    "get { return " + (string.IsNullOrEmpty(htmlDoc) ? "string.Empty" : "\"" + htmlDoc + "\"") + ";}" +
                                "}" +

                                 "public int Order { get { return " + Order + "; } }" +

#if NETCOREAPP2_0
                                 "public void Init(Hashtable elements, IElement element)" +
                                 "{" +
                                        "_element = element; _elements = elements;" +
                                  "}" +
#else
                                 "public void Init(Hashtable elements, IElement element, IZenCallable callable)" +
                                 "{" +
                                        "_element = element; _elements = elements; _callable = callable;" +
                                  "}" +
#endif
                                  "void exec(string elementId)" +
                                  "{" +
                                        "((IElement)_elements[elementId]).IAmStartedYou = _element;" +
                                        "((IElement)_elements[elementId]).StartElement(_elements, false);" +
                                  "}" +

                                  "void set_result(string elementId, object result)" +
                                  "{" +
                                      "((IElement)_elements[elementId]).LastResultBoxed = result;" +
                                   "}" +

                                   "void set_result_from_element(string elementId, string elementIdResult)" +
                                   "{" +
                                       "((IElement)_elements[elementId]).LastResultBoxed = get_result_raw(elementIdResult);" +
                                   "}" +

                                   "object get_result_raw(string elementId)" +
                                   "{" +
                                       "return ((IElement)_elements[elementId]).LastResultBoxed;" +
                                   "}" +

                                   "string get_result(string elementId)" +
                                   "{" +
                                       "return ((IElement)_elements[elementId]).LastResult;" +
                                   "}" +

                                   "void set_element_property(string elementId, string key, string value)" +
                                   "{" +
                                       "((_elements as Hashtable)[elementId] as IElement).SetElementProperty(key, value);" +
                                   "}" +

                                   "string get_element_property(string elementId, string key)" +
                                   "{" +
                                       "return ((_elements as Hashtable)[elementId] as IElement).GetElementProperty(key);" +
                                   "}" +

                                   "string get_property(string key)" +
                                   "{" +
                                       "return _element.GetElementProperty(key);" +
                                   "}" +
#if !NETCOREAPP2_0
                                   "object call_element_action(string elementId, string actionId, Hashtable param)" +
                                   "{" +
                                       "if ((((_elements as Hashtable)[elementId] as IElement).ImplementationModule as IZenCallable) == null) throw new Exception (\"Element \"+ elementId +\" is not callable!\"); return ((((_elements as Hashtable)[elementId] as IElement).ImplementationModule as IZenCallable).Call(actionId,param));" +
                                   "}" +
#endif
                                   "void set_condition(bool condition)" +
                                   "{" +
                                        "((_elements as Hashtable)[\"" + (element != null ? element.ID : string.Empty) + "\"] as IElement).IsConditionMet = condition;" +
                                   "}" +

                                   "void set_condition(string elementId, bool condition)" +
                                   "{" +
                                        "((_elements as Hashtable)[elementId] as IElement).IsConditionMet = condition;" +
                                   "}" +

                              "}" +
                        "}";
    }
    #endregion

    #region CompileAndSaveAssembly
#if NETCOREAPP2_0
    static string CompileAndSaveAssembly(string code, string references, string fileName)
    {
        List<MetadataReference> coreReferencesPaths = new List<MetadataReference>();
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System")).Location));
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Console")).Location));
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Collections")).Location));
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime")).Location));
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime.Extensions")).Location));
        coreReferencesPaths.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        foreach (string s in Regex.Split(references, ","))
        {
            if (!string.IsNullOrEmpty(s))
                coreReferencesPaths.Add(MetadataReference.CreateFromFile(s));
        }

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        options = options.WithOptimizationLevel(OptimizationLevel.Release);
        options = options.WithPlatform(Platform.X64);
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("zenCompile", syntaxTrees: new[] { tree }, references: coreReferencesPaths.ToArray(), options: options);
        string errors = string.Empty;
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Length > 0)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.IsWarningAsError)
                {
                    errors += diagnostic.GetMessage();
                    Console.WriteLine(diagnostic.GetMessage());
                }
                Console.WriteLine(diagnostic.GetMessage());
            }
        }

        if (string.IsNullOrEmpty(errors))
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            compilation.Emit(fileName);
        }
        return errors;
    }
#else
    static string CompileAndSaveAssembly(string code, IElement element, string fileName, IGadgeteerBoard ParentBoard, string[] referencesPaths)
    {

        CSharpCodeProvider csProvider = new CSharpCodeProvider();

        CompilerParameters options = new CompilerParameters();
        options.GenerateExecutable = false;
        options.GenerateInMemory = string.IsNullOrEmpty(fileName);

        if (!string.IsNullOrEmpty(fileName))
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            options.OutputAssembly = fileName;
        }

        foreach (string s in referencesPaths)
            options.ReferencedAssemblies.Add(s);

        CompilerResults result;
        result = csProvider.CompileAssemblyFromSource(options, code);

        string errors = string.Empty;

        if (result.Errors.HasErrors)
        {
            foreach (CompilerError error in result.Errors)
                errors += "Line : " + error.Line.ToString() + " " + error.ErrorText + "; ";
        }

        if (result.Errors.HasWarnings)
        { }
        return errors;
    }
#endif
    #endregion

    #region IsPrimitiveType
    static bool IsPrimitiveType(string sType)
    {
        switch (sType.ToLower())
        {
            case "bool":
            case "ushort":
            case "uint":
            case "string":
            case "short":
            case "sbyte":
            case "int":
            case "float":
            case "double":
            case "byte":
            case "uint16":
            case "int32":
            case "int64":
            case "byte[]":
            case "uint16[]":
            case "uint32[]":
            case "int16[]":
            case "int32[]":
            case "double[]":
            case "single[]":
                return true;
        }
        return false;
    }
    #endregion

    #region GetReplacedComplexTypeScript
    static string GetReplacedComplexTypeScript(HtmlNode node, string scriptText, string sType, string elementId, Hashtable elements)
    {
        switch (sType.ToLower())
        {
            case "jobject":
                string jObjectPostfix = (node.InnerHtml.IndexOf('[') > -1 ? node.InnerHtml.Substring(node.InnerHtml.IndexOf('[')) : string.Empty);
                string currentJObjectTag = string.Concat("((((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed) as JObject)" + jObjectPostfix);

                if (node.Attributes["cast"] == null)
                    scriptText = scriptText.Replace(node.OuterHtml, currentJObjectTag);
                else
                    scriptText = scriptText.Replace(node.OuterHtml, string.Concat(GetPrimitiveTypeCastString(node.Attributes["cast"].Value), currentJObjectTag, ")"));
                break;

            case "dataset":
                string currentDatasetTag = "((DataSet)((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)" + (node.InnerHtml.Replace("&period;", ".").IndexOf('.') > -1 ? node.InnerHtml.Substring(node.InnerHtml.Replace("&period;", ".").IndexOf('.')) : string.Empty);

                if (node.Attributes["cast"] == null)
                {
                    //If row cast string returns null then just DataSet is placed between result tags: <result>MyDataSet</result>. Mostly within <code>
                    string rowCastString = GetRowCastFromDataSet(elementId, currentDatasetTag, elements);
                    scriptText = scriptText.Replace(node.OuterHtml, string.IsNullOrEmpty(rowCastString) ? currentDatasetTag : string.Concat(rowCastString, currentDatasetTag, ")"));
                }
                else
                    scriptText = scriptText.Replace(node.OuterHtml, string.Concat(GetPrimitiveTypeCastString(node.Attributes["cast"].Value), currentDatasetTag, ")"));
                break;

            case "datatable":
                string currentDatatableTag = "((DataTable)((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)" + (node.InnerHtml.Replace("&period;", ".").IndexOf('.') > -1 ? node.InnerHtml.Substring(node.InnerHtml.Replace("&period;", ".").IndexOf('.')) : string.Empty);
                if (node.Attributes["cast"] == null)
                {
                    //If row cast string returns null then just DataTable is placed between result tags: <result>MyDataTable</result>. Mostly within <code>
                    string rowCastString = GetRowCastFromDataTable(elementId, currentDatatableTag, elements);
                    scriptText = scriptText.Replace(node.OuterHtml, string.IsNullOrEmpty(rowCastString) ? currentDatatableTag : string.Concat(rowCastString, currentDatatableTag, ")"));
                }
                else
                    scriptText = scriptText.Replace(node.OuterHtml, string.Concat(GetPrimitiveTypeCastString(node.Attributes["cast"].Value), currentDatatableTag, ")"));
                break;

            case "hashtable":
                string castString = string.Empty;
                //Currently integer and string key types are supported : ht[0] & ht["0"]
                bool isHashKeyStringType = false;
                object hashKey;
                if (node.InnerHtml.Split('[')[1].IndexOf('"') > -1)
                {
                    hashKey = node.InnerHtml.Split('[')[1].Replace("]", string.Empty).Replace("\"", string.Empty).Trim();
                    isHashKeyStringType = true;
                }
                else
                    hashKey = Convert.ToInt32(node.InnerHtml.Split('[')[1].Replace("]", string.Empty).Replace("\"", string.Empty).Trim());

                if (!((Hashtable)(((IElement)elements[elementId]).LastResultBoxed)).ContainsKey(hashKey))
                    throw new Exception("ERROR : Element " + elementId + " does not contains key " + hashKey);

                castString = GetPrimitiveTypeCastString(((Hashtable)(((IElement)elements[elementId]).LastResultBoxed))[hashKey].GetType().Name);
                string formattedHashKey = isHashKeyStringType ? "\"" + hashKey + "\"" : hashKey.ToString();
                scriptText = scriptText.Replace(node.OuterHtml, castString + "((Hashtable)((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)[" + formattedHashKey + "]" + (!string.IsNullOrEmpty(castString) ? ")" : string.Empty));
                break;
        }
        return scriptText;
    }
    #endregion

    #region GetPrimitiveTypeCastString
    static string GetPrimitiveTypeCastString(string sType)
    {
        switch (sType.ToLower())
        {
            case "bool":
                return "Convert.ToBoolean(";

            case "ushort":
                return "Convert.ToUInt16(";

            case "uint":
            case "uint32":
                return "Convert.ToUInt32(";

            case "string":
                return "Convert.ToString(";

            case "short":
                return "Convert.ToInt16(";

            case "sbyte":
                return "Convert.ToSByte(";

            case "int":
                return "Convert.ToInt32(";

            case "int16":
                return "Convert.ToInt16(";

            case "float":
                return "Convert.ToSingle(";

            case "double":
                return "Convert.ToDouble(";

            case "byte":
                return "Convert.ToByte(";

            case "uint16":
                return "Convert.ToUInt16(";

            case "int32":
                return "Convert.ToInt32(";

            case "int64":
                return "Convert.ToInt64(";

            case "byte[]":
                return "((byte[])";

            case "uint16[]":
                return "((ushort[])";

            case "uint32[]":
                return "((uint[])";

            case "int16[]":
                return "((short[])";

            case "int32[]":
                return "((int[])";

            case "double[]":
                return "((double[])";

            case "single[]":
                return "((float[])";
        }
        return string.Empty;
    }
    #endregion

    #region GetReplacedPrimitiveTypeScript
    static string GetReplacedPrimitiveTypeScript(HtmlNode node, string scriptText, string sType, string elementId)
    {
        return node.Attributes["cast"] == null ? scriptText.Replace(node.OuterHtml, GetPrimitiveTypeCastString(sType) + "((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)") : scriptText.Replace(node.OuterHtml, "((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed");
    }
    #endregion

    #region ReplaceResultTagWithCast
    /// <summary>
    /// Replaces result tag with actual cast statement for given node and raw string
    /// </summary>
    /// <param name="node">Node that contains result tag</param>
    /// <param name="elements">All system elements</param>
    /// <param name="scriptText">Complete raw script text</param>
    /// <param name="debug">Debug</param>
    /// <returns></returns>
    static string ReplaceResultTagWithCast(HtmlNode node, Hashtable elements, string scriptText, bool debug)
    {
        string elementId = Decode(node.InnerHtml);

        //DataSet etc...
        if (elementId.IndexOf('[') > -1)
            elementId = elementId.Split('[')[0].Trim();

        //Hashtable etc....
        if (elementId.IndexOf('.') > -1)
            elementId = elementId.Split('.')[0].Trim();

        string convertStatement = string.Empty;

        if (!elements.ContainsKey(elementId))
            throw new Exception("ERROR : Element " + elementId + " does not exists!");

        if (((IElement)elements[elementId]).LastResultBoxed == null)
        {
            if (debug && node.Attributes["cast"] == null)
            {
                Console.WriteLine("");
                Console.WriteLine("[" + DateTime.Now.ToShortTimeString() + "] [" + elementId + "] has no result yet. It needs to be casted correctly in template!");
            }
            return node.Attributes["cast"] == null ? scriptText.Replace(node.OuterHtml, "((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed") : scriptText.Replace(node.OuterHtml, GetPrimitiveTypeCastString(node.Attributes["cast"].Value) + "((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)");
        }

        if (IsPrimitiveType(((IElement)elements[elementId]).LastResultBoxed.GetType().Name))
        {
            if (node.Attributes["cast"] == null)
                scriptText = GetReplacedPrimitiveTypeScript(node, scriptText, ((IElement)elements[elementId]).LastResultBoxed.GetType().Name, elementId);
            else
                scriptText = scriptText.Replace(node.OuterHtml, GetPrimitiveTypeCastString(node.Attributes["cast"].Value) + "((IElement)_elements[\"" + elementId + "\"]).LastResultBoxed)");
        }
        else
            scriptText = GetReplacedComplexTypeScript(node, scriptText, ((IElement)elements[elementId]).LastResultBoxed.GetType().Name, elementId, elements);

        return scriptText;
    }
    #endregion

    #region GetScript
    static IZenCsScript GetScript(Assembly script)
    {
        // Now that we have a compiled script, lets run them
        foreach (Type type in script.GetExportedTypes())
        {
            foreach (Type iface in type.GetInterfaces())
            {
                if (iface == typeof(IZenCsScript))
                {
                    ConstructorInfo constructor = type.GetConstructor(System.Type.EmptyTypes);
                    if (constructor != null && constructor.IsPublic)
                    {
                        IZenCsScript scriptObject = constructor.Invoke(null) as IZenCsScript;
                        if (scriptObject != null)
                            return scriptObject;

                        else
                        { }
                    }
                    else
                    { }
                }
            }
        }
        return null;
    }

    #region GetRowCastFromDataSet
    static string GetRowCastFromDataSet(string elementId, string dataset, Hashtable elements)
    {
        DataSet ds = ((DataSet)((IElement)elements[elementId]).LastResultBoxed);
        dataset = Decode(dataset);
        if (dataset.IndexOf("Tables") > -1)
        {
            string[] splittesDatasetClause = dataset.Substring(dataset.IndexOf("Tables")).Split('.');
            if (splittesDatasetClause.Length > 1 && splittesDatasetClause[0].IndexOf("Tables") > -1 && splittesDatasetClause[1].IndexOf("Rows") > -1)
            {
                string tableName = splittesDatasetClause[0].Substring(splittesDatasetClause[0].IndexOf('[') + 1).Substring(0, splittesDatasetClause[0].Substring(splittesDatasetClause[0].IndexOf('[') + 1).IndexOf(']')).Trim();
                string colName = splittesDatasetClause[1].Substring(splittesDatasetClause[1].LastIndexOf('[') + 1).Substring(0, splittesDatasetClause[1].Substring(splittesDatasetClause[1].LastIndexOf('[') + 1).LastIndexOf(']')).Trim();

                DataTable dt = null;

                if (tableName.Contains("\""))
                    dt = ds.Tables[tableName.Replace("\"", string.Empty)];
                else
                    dt = ds.Tables[Convert.ToInt16(tableName)];

                DataColumn dc = null;

                if (colName.Contains("\""))
                    dc = dt.Columns[colName.Replace("\"", string.Empty)];
                else
                    dc = dt.Columns[Convert.ToInt16(colName)];

                return GetPrimitiveTypeCastString(dc.DataType.Name.ToLower());
            }
        }
        return string.Empty;
    }
    #endregion

    #region GetRowCastFromDataTable
    static string GetRowCastFromDataTable(string elementId, string datatable, Hashtable elements)
    {
        DataTable dt = ((DataTable)((IElement)elements[elementId]).LastResultBoxed);
        datatable = Decode(datatable);
        if (datatable.IndexOf("Rows") > -1 && datatable.IndexOf("[") > -1 && datatable.IndexOf("]") > -1)
        {
            string colName = datatable.Substring(datatable.LastIndexOf('[') + 1).Substring(0, datatable.Substring(datatable.LastIndexOf('[') + 1).LastIndexOf(']')).Trim();
            DataColumn dc = null;

            if (colName.Contains("\""))
                dc = dt.Columns[colName.Replace("\"", string.Empty)];
            else
                dc = dt.Columns[Convert.ToInt16(colName)];

            return GetPrimitiveTypeCastString(dc.DataType.Name.ToLower());
        }
        return string.Empty;
    }
    #endregion
    #endregion

    #region GetReferences
    static List<string> GetReferences(HtmlNode headerNode, List<string> references, IGadgeteerBoard ParentBoard)
    {
#if !NETCOREAPP2_0
        references.Add("System.dll");
        references.Add("System.Core.dll");
        references.Add("System.Data.dll");
        references.Add("System.Xml.dll");

        //User defined references are not valid if build is called from web app
        if (headerNode != null)
        {
            string code = Decode(headerNode.InnerText);
            foreach (string line in code.Split(';'))
            {
                if (line.Trim().StartsWith("reference"))
                {
                    string[] values = Regex.Split(line, "reference");
                    if (values.Length == 2)
                    {
                        if (values[0].Trim() == string.Empty)
                        {
                            if (ParentBoard != null && (values[1].Trim().IndexOf('\\') > -1 || values[1].Trim().IndexOf('/') > -1))
                            {
                                string[] libPath = null;
                                if (values[1].Trim().IndexOf('\\') > -1)
                                    libPath = values[1].Split('\\');
                                else
                                    libPath = values[1].Split('/');

                                string wholePath = Environment.CurrentDirectory;
                                foreach (string sPath in libPath)
                                {
                                    if (sPath != string.Empty)
                                        wholePath = Path.Combine(wholePath, sPath.Trim());
                                }
                                references.Add(wholePath);

                                //Copy reference to common dependencies folder, so that Core can resolve it
                                FileVersionInfo referenceFileInfo = FileVersionInfo.GetVersionInfo(wholePath);
                                if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "project", ParentBoard.TemplateID, "Dependencies", Path.GetFileNameWithoutExtension(wholePath), referenceFileInfo.FileVersion)))
                                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "project", ParentBoard.TemplateID, "Dependencies", Path.GetFileNameWithoutExtension(wholePath), referenceFileInfo.FileVersion));

                                File.Copy(wholePath, Path.Combine(Environment.CurrentDirectory, "project", ParentBoard.TemplateID, "Dependencies", Path.GetFileNameWithoutExtension(wholePath), referenceFileInfo.FileVersion, Path.GetFileName(wholePath)), true);
                            }
                            else
                                references.Add(values[1].Trim());
                        }
                    }
                    else
                        break;
                }
                else if (line.Trim().StartsWith("//"))
                {
                    //Ignore comments in references section
                }
                else
                    break;
            }
        }
#endif
        return references;
    }
    #endregion

    #region RemoveComments
    static string RemoveComments(string code)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var strings = @"""((\\[^\n]|[^""\n])*)""";
        var verbatimStrings = @"@(""[^""]*"")+";

        return Regex.Replace(code, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                    me =>
                    {
                        if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                            return me.Value.StartsWith("//") ? Environment.NewLine : "";

                        return me.Value;
                    }, RegexOptions.Singleline).Trim();
    }
    #endregion

    #region GetUsings
    static string GetUsings(string code)
    {
        code = RemoveComments(code);
        string userUsings = string.Empty;
        foreach (string line in code.Split(';'))
        {
            if (line.Trim().StartsWith("using"))
            {
                string[] values = Regex.Split(line, "using");
                if (values.Length == 2)
                {
                    if (values[0].Trim() == string.Empty)
                        userUsings += line.Trim() + ";";
                }
                else
                    break;
            }
            else if (line.Trim().StartsWith("reference") || line.Trim().StartsWith("//"))
            {
                //Ignore comments and references in using section
            }
            else
                break;
        }
        return userUsings;
    }
    #endregion
    #endregion
}