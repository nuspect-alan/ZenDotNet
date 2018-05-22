using System.Collections;

namespace CommonInterfaces
{
    //It's called on first element execution (JIT). It runs on nodes thread and it's not thread safe. Boot time is not affected with this method call
    public interface IZenElementInit
    {
        void OnElementInit(Hashtable elements, IElement element);
    }
}
