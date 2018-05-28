namespace ZenCommon
{
    public interface IZenImplementation
    {
        string ID { get; set; }
        IGadgeteerBoard ParentBoard { get; set; }
    }
}
