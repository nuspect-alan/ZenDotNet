using System.Collections;
using System.Collections.Generic;

namespace CommonInterfaces
{
    public interface IZenExecutor
    {
        List<string> GetElementsToExecute(IElement element, Hashtable elements);
    }
}
