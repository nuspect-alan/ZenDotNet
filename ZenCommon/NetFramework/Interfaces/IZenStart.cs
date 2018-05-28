using System.Collections;

namespace ZenCommon
{
    public interface IZenStart
    {
        ArrayList Dependencies { get; set; }
        string State { get; set; }
        bool IsRepeatable { get; set; }
        bool IsActive { get; set; }
    }
}
