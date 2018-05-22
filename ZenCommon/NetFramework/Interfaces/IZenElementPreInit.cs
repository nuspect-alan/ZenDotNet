using System.Collections;

namespace CommonInterfaces
{
    //It runs on main thread, and it's executed for each element when nodes are filled to collection. It's thread safe
    //Warning : boot time is affected
    public interface IZenElementPreInit
    {
        void OnElementPreInit(IElement element, Hashtable elements);
    }
}
