using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ZenCsScriptData
{
    public IZenCsScript ZenCsScript { get; set; }
    public HtmlDocument ScriptDoc { get; set; }

    public ZenCsScriptData(IZenCsScript ZenCsScript, HtmlDocument ScriptDoc)
    {
        this.ScriptDoc = ScriptDoc;
        this.ZenCsScript = ZenCsScript;

    }
}

