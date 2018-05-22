using System.Collections;

namespace CommonInterfaces
{
    public interface IZenAction : IZenImplementation
    {
        void ExecuteAction(Hashtable elements, IElement element, IElement iAmStartedYou);
    }
}
