using System.Collections;

namespace ZenCommon
{
    public interface IZenAction : IZenImplementation
    {
        void ExecuteAction(Hashtable elements, IElement element, IElement iAmStartedYou);
    }
}
