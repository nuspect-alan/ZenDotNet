namespace ZenCommon
{
    //This is pre-element state. Method is fired just once, on initial implementation creation.
    //Method is useful for actions that are common to all elements of current implementation (dll copying, etc...) and do not require nodes data
    //It runs on main thread, and it's thread safe
    //Warning : boot time is affected
    public interface IZenImplementationInit
    {
        void OnImplementationInit(string args);
    }
}
