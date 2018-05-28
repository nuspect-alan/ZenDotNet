using System.Collections;

namespace ZenCommon
{
    public delegate void ModuleEventHandler(object sender, ModuleEventData e);
    public class ModuleEventData
    {
        public string Data { get; set; }
        public string ID { get; set; }
        public object Tag { get; set; }
        public ModuleEventData(string ID, string Data)
        {
            this.Data = Data;
            this.ID = ID;
        }

        public ModuleEventData(string ID, string Data, object Tag)
            : this(ID, Data)
        {
            this.Tag = Tag;
        }

    }
}
