using System.Collections;

namespace ZenCommon
{
    public interface IZenEvent : IZenImplementation
    {
        event ModuleEventHandler ModuleEvent;
        bool CheckInterruptCondition(ModuleEventData data, IElement element, Hashtable elements);
    }
}
