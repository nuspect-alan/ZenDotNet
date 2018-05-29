using ZenCommon;
using System.Collections;

public interface IZenCsScript
{
    string RawHtml { get; }
    object RunCustomCode(string functionName);
#if NETCOREAPP2_0
    void Init(Hashtable elements, IElement element);
#else
    void Init(Hashtable elements, IElement element, IZenCallable callable);
#endif
    int Order { get; }
}
