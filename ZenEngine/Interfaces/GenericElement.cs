using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.ExceptionServices;
using ZenCommon;

namespace ComputingEngine
{
    public class GenericElement : IGenericElement
    {
        #region Structs
        #region PostponedEventStruct
        internal struct PostponedEventStruct
        {
            internal ModuleEventData moduleEventData;
            internal IZenEvent sender;
        }
        #endregion

        #endregion

        #region Constants
        #region BUFFERED_TRIGGERS
        const string BUFFERED_TRIGGERS = "__BUFFER_TRIGGERS__";
        #endregion

        #region EVENTS_BUFFER_LENGTH
        const string EVENTS_BUFFER_LENGTH = "__EVENTS_BUFFER_LENGTH__";
        #endregion
        #endregion

        #region Fields
        #region m_EventsBufferLength
        int m_EventsBufferLength;
        #endregion

        #region m_EventableBufferTriggers
        List<string> m_EventableBufferTriggers = new List<string>();
        #endregion

        #region bGreenLightToProcede
        bool bGreenLightToProcede = true;
        #endregion

        #region m_PostponedEvents
        Queue<PostponedEventStruct> m_PostponedEvents = new Queue<PostponedEventStruct>();
        #endregion

        #region m_PauseElement
        private AutoResetEvent m_PauseElement = new AutoResetEvent(false);
        #endregion

        #region InstanceLock
        public object InstanceLock { get; set; }
        #endregion

        #region IAmStartedYou
        public IElement IAmStartedYou { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion

        #region m_allElements
        private Hashtable m_allElements;
        #endregion

        #region Status
        public ElementStatus Status { get; set; }
        #endregion

        #region m_IsEventHandledSync
        object m_IsEventHandledSync = new object();
        #endregion

        #region m_IsEventHandled
        bool m_IsEventHandled;
        #endregion

        #region m_IsInitialized
        bool m_IsInitialized;
        #endregion

        #region LastCacheFill
        public DateTime LastCacheFill { get; set; }
        #endregion
        #endregion

        #region IElement implementations
        #region Properties
        #region DisconnectedElements
        public ArrayList DisconnectedElements { get; set; }
        #endregion

        #region UnregisterEvent
        public bool UnregisterEvent { get; set; }
        #endregion

        #region PauseElement
        public AutoResetEvent PauseElement { get { return m_PauseElement; } }
        #endregion

        #region IsStarted
        public bool IsStarted { get; set; }
        #endregion

        #region LoopLock
        public object LoopLock { get; set; }
        #endregion

        #region MsElapsed
        public long MsElapsed { get; set; }
        #endregion

        #region Started
        public DateTime Started { get; set; }
        #endregion

        #region ManuallyTriggeredStartElements
        public ArrayList ManuallyTriggeredStartElements { get; set; }
        #endregion

        #region LastExecutionDate
        public DateTime LastExecutionDate { get; set; }
        #endregion

        #region ErrorMessage
        public string ErrorMessage { get; set; }
        #endregion

        #region ErrorCode
        public int ErrorCode { get; set; }
        #endregion

        #region Operator
        public string Operator
        {
            get;
            set;
        }
        #endregion

        #region FirstLevelTrueElements
        public ArrayList FirstLevelTrueElements
        {
            get;
            set;
        }
        #endregion

        #region FirstLevelFalseElements
        public ArrayList FirstLevelFalseElements
        {
            get;
            set;
        }
        #endregion

        #region FirstLevelTrueChilds
        public ArrayList FirstLevelTrueChilds
        {
            get;
            set;
        }
        #endregion

        #region FirstLevelFalseChilds
        public ArrayList FirstLevelFalseChilds
        {
            get;
            set;
        }
        #endregion

        #region LastResult
        public string LastResult { get; set; }
        #endregion

        #region LastResultBoxed
        public object LastResultBoxed { get; set; }
        #endregion

        #region DebugString
        public string DebugString { get; set; }
        #endregion

        #region DoDebug
        public bool DoDebug { get; set; }
        #endregion

        #region Conditions
        public Hashtable Conditions { get; set; }
        #endregion

        #region ImplementationModule
        public IZenImplementation ImplementationModule { get; set; }
        #endregion

        #region ID
        public string ID { get; set; }
        #endregion

        #region IsConditionMet
        public bool IsConditionMet { get; set; }
        #endregion

        #region ResultUnit
        public string ResultUnit { get; set; }
        #endregion

        #region ResultSource
        public string ResultSource { get; set; }
        #endregion
        #endregion

        #region Events
        #region ElementEvent
        public event ElementEventHandler ElementEvent;
        #endregion
        #endregion

        #region Functions
        #region SetElementProperty
        public void SetElementProperty(string key, string value)
        {
            try
            {
                this.Conditions[key] = value;
            }
            catch (Exception e)
            {
                SetException(e);
            }
        }
        #endregion

        #region GetElementProperty
        public string GetElementProperty(string sValue)
        {
            string sReturn = "";
            try
            {
                if (this.Conditions[sValue] == null)
                    return "";

                return this.Conditions[sValue].ToString();
            }
            catch (Exception e)
            {
                SetException(e);
                return sReturn;
            }
        }
        #endregion

        #region StopElementExecuting
        public void StopElementExecuting()
        {
            this.Status = ElementStatus.STOPPED;
            if (this.ImplementationModule as IZenEvent != null && UnregisterEvent)
                (this.ImplementationModule as IZenEvent).ModuleEvent -= module_ModuleEvent;
        }
        #endregion

        #region StartElement
        #region StartElementMain
        [HandleProcessCorruptedStateExceptions]
        private void StartElementMain(Hashtable elements, IZenAction actionable, IZenEvent eventable, IZenElementInit initializable, bool m_IsEventRegistered)
        {
            try
            {
                this.m_IsEventHandled = false;
                this.ErrorCode = 0;
                this.ErrorMessage = string.Empty;
                this.Status = ElementStatus.RUNNING;

                if (m_allElements == null)
                    m_allElements = elements;

                if (!m_IsInitialized && initializable != null)
                    initializable.OnElementInit(elements, this);

                m_IsInitialized = true;

                if (actionable != null)
                {
                    this.Started = DateTime.Now;
                    if (this.GetElementProperty("_CACHE_").Trim() != string.Empty)
                    {
                        lock (this.InstanceLock)
                        {
                            if (this.LastCacheFill == DateTime.MinValue || this.LastCacheFill.AddSeconds(Convert.ToInt32(this.GetElementProperty("_CACHE_").Trim())) < DateTime.Now)
                            {
                                actionable.ExecuteAction(elements, this, this.IAmStartedYou);
                                this.LastCacheFill = DateTime.Now;
                            }
                        }
                    }
                    else
                        actionable.ExecuteAction(elements, this, this.IAmStartedYou);

                    FireElementEvent();
                }
                else if (eventable != null && (UnregisterEvent || !m_IsEventRegistered))
                {
                    eventable.ModuleEvent += new ModuleEventHandler(module_ModuleEvent);
                    m_IsEventRegistered = true;
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }
        #endregion

        #region StartElementWrapper
        private void StartElementWrapper(Hashtable elements, IZenAction actionable, IZenEvent eventable, IZenElementInit initializable, bool Async)
        {
            try
            {
                bool m_IsEventRegistered = false;
                if (Async)
                {
                    while (true)
                    {
                        StartElementMain(elements, actionable, eventable, initializable, m_IsEventRegistered);
                        m_IsEventRegistered = true;
                        PauseElement.WaitOne();
                    }
                }
                else
                    StartElementMain(elements, actionable, eventable, initializable, m_IsEventRegistered);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(this.ID + " has been terminated...");
            }
        }
        #endregion

        #region StartElement
        //Interface implementation
        public void StartElement(Hashtable elements, bool Async)
        {
            IZenAction actionable = this.ImplementationModule as IZenAction;
            IZenEvent eventable = this.ImplementationModule as IZenEvent;
            IZenElementInit initializable = this.ImplementationModule as IZenElementInit;

            IsStarted = true;

            if (eventable != null && !string.IsNullOrEmpty(GetElementProperty(BUFFERED_TRIGGERS)))
            {
                foreach (string s in Regex.Split(GetElementProperty(BUFFERED_TRIGGERS), ","))
                {
                    if (!string.IsNullOrEmpty(s))
                        m_EventableBufferTriggers.Add(s.Trim());
                }
                if (!string.IsNullOrEmpty(GetElementProperty(EVENTS_BUFFER_LENGTH)))
                    m_EventsBufferLength = Convert.ToInt32(GetElementProperty(EVENTS_BUFFER_LENGTH));

                ParentBoard.OnElementStopExecutingEvent += elementBoard_OnElementStopExecutingEvent;
            }

            if (Async)
                Task.Factory.StartNew(() => StartElementWrapper(elements, actionable, eventable, initializable, Async));
            else
                StartElementWrapper(elements, actionable, eventable, initializable, Async);
        }

        #endregion
        #endregion
        #endregion
        #endregion

        #region Event handlers
        #region elementBoard_OnElementStopExecutingEvent
        protected void elementBoard_OnElementStopExecutingEvent(object sender, IElement args)
        {
            lock (this.LoopLock)
            {
                if (m_EventableBufferTriggers.Contains(args.ID))
                {
                    bGreenLightToProcede = true;
                    if (m_PostponedEvents.Count > 0)
                    {
                        PostponedEventStruct postponedEvent = m_PostponedEvents.Dequeue();
                        this.IsConditionMet = postponedEvent.sender.CheckInterruptCondition(postponedEvent.moduleEventData, this, m_allElements);
                        FireElementEvent();
                    }
                }
            }
        }
        #endregion

        #region module_ModuleEvent
        private void module_ModuleEvent(object sender, ModuleEventData e)
        {
            lock (m_IsEventHandledSync)
            {
                if (UnregisterEvent && m_IsEventHandled)
                    return;

                m_IsEventHandled = true;
            }

            try
            {
                //Is it for me? If not, just ignore event, that shared implementation fired
                if (e.ID == "-1" || e.ID == this.ID)
                {
                    this.Started = DateTime.Now;
                    FireEvent((sender as IZenEvent), e);
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }
        #endregion
        #endregion

        #region Private Functions
        #region FireEvent
        private void FireEvent(IZenEvent sender, ModuleEventData data)
        {
            lock (this.LoopLock)
            {
                if (m_EventableBufferTriggers.Count == 0 || bGreenLightToProcede && m_PostponedEvents.Count == 0)
                {
                    bGreenLightToProcede = false;
                    this.IsConditionMet = sender.CheckInterruptCondition(data, this, m_allElements);
                    FireElementEvent();
                }
                else
                {
                    PostponedEventStruct postponedEvent = new PostponedEventStruct();
                    postponedEvent.moduleEventData = data;
                    postponedEvent.sender = sender;

                    if (m_EventsBufferLength > 0 && m_PostponedEvents.Count > m_EventsBufferLength)
                        m_PostponedEvents.Dequeue();

                    m_PostponedEvents.Enqueue(postponedEvent);
                }
            }
        }
        #endregion

        #region SetException
        private void SetException(Exception e)
        {
            this.IsConditionMet = true;

            if (e == null)
            {
                this.ErrorMessage = "";
                return;
            }

            if (e.InnerException != null && e.InnerException.Message != null && e.InnerException.Message != "")
                this.ErrorMessage = e.InnerException.Message;
            else if (e.Message != null && e.Message != "")
                this.ErrorMessage = e.Message;

            this.ErrorCode = 1;

            Console.WriteLine(this.ID + " : " + this.ErrorMessage);

            ParentBoard.PublishError(this.ID, ErrorMessage);
            ParentBoard.PublishInfoPrint(this.ID, ErrorMessage, "error");
            this.IsConditionMet = true;
            FireElementEvent();
        }
        #endregion

        #region FireElementEvent
        private void FireElementEvent()
        {
            try
            {
                if (this.ManuallyTriggeredStartElements != null)
                {
                    foreach (IGenericElement p in this.ManuallyTriggeredStartElements)
                    {
                        p.IAmStartedYou = this;
                        p.StartElement(m_allElements, true);
                    }
                    return;
                }

                ElementEventHandler temp = ElementEvent;
                if (temp != null)
                {
                    DateTime eventHandlerStarted = DateTime.Now;
                    temp(this, null);
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }
        #endregion
        #endregion
    }
}
