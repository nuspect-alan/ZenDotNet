using System.Collections;

namespace ZenCommon
{
    public interface IZenCallable
    {
        object Call(string actionID, Hashtable param);
    }
}
