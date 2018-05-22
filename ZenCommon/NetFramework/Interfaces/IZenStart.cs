using System.Collections;

namespace CommonInterfaces
{
    public interface IZenStart
    {
        ArrayList Dependencies { get; set; }
        string State { get; set; }
        bool IsRepeatable { get; set; }
        bool IsActive { get; set; }
    }
}
