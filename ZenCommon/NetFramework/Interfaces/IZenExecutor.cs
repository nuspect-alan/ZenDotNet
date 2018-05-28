using System.Collections;
using System.Collections.Generic;

namespace ZenCommon
{
    public interface IZenExecutor
    {
        List<string> GetElementsToExecute(IElement element, Hashtable elements);
    }
}
