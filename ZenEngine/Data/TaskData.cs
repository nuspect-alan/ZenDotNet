using ZenCommon;

namespace ComputingEngine.Data
{
    internal class TaskData
    {
        public string TaskID { get; set; }
        public IElement CurrentElement { get; set; }
        public IElement CurrentElementChild { get; set; }
        public bool ExpectedState { get; set; }


        public string ErrorMessage { get; set; }
        public TaskData(string TaskID, IElement CurrentElement, IElement CurrentElementChild, bool ExpectedState, string ErrorMessage)
        {

            this.CurrentElementChild = CurrentElementChild;
            this.CurrentElement = CurrentElement;
            this.ExpectedState = ExpectedState;
            this.TaskID = TaskID;
            this.ErrorMessage = ErrorMessage;
        }
    }
}
