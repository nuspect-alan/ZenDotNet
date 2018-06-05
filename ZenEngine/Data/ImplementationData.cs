using ZenCommon;

namespace ComputingEngine.Data
{
    internal class ImplementationData
    {
        public IZenImplementation Implementation { get; set; }
        public string[] ImplementationParams { get; set; }
        public ImplementationData(IZenImplementation Implementation, string[] ImplementationParams)
        {
            this.ImplementationParams = ImplementationParams;
            this.Implementation = Implementation;
        }
    }
}
