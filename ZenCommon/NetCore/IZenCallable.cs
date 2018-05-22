using System.Collections;

namespace CommonInterfaces
{
    public interface IZenCallable
    {
        object Call(string actionID, Hashtable param);
    }
}
