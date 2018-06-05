using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComputingEngine.Data;
using ZenCommon;

namespace ComputingEngine
{
    internal class Tasks
    {
        #region Core
        #region HandleElementEvent
        private void HandleElementEvent(IGenericElement oEventCausedElement)
        {
            oEventCausedElement.Status = ElementStatus.ARRIVED;
            lock (oEventCausedElement.LoopLock)
            {
                ArrayList aStartPlg = new ArrayList();
                ArrayList aStopPlg = new ArrayList();
                ArrayList aChildConditions = new ArrayList();

                oEventCausedElement.StopElementExecuting();
                //Pojdi čez vse childe od elementa, ki je sprožil event
                foreach (IGenericElement oEventCausedElementChild in GetTrueAndFalseChilds(oEventCausedElement))
                {
                    aChildConditions.Clear();

                    //pojdi čez vse true elemente od childa.
                    if (oEventCausedElementChild.FirstLevelTrueElements != null)
                    {
                        foreach (IGenericElement pTrueelement in oEventCausedElementChild.FirstLevelTrueElements)
                            aChildConditions.Add(pTrueelement.IsConditionMet);
                    }

                    //pojdi čez vse false elemente od childa.
                    if (oEventCausedElementChild.FirstLevelFalseElements != null)
                    {
                        foreach (IGenericElement pFalseelement in oEventCausedElementChild.FirstLevelFalseElements)
                            aChildConditions.Add(!pFalseelement.IsConditionMet);
                    }

                    //This is synchronous check -> stop here and wait for child to finish processing
                    while (oEventCausedElementChild.Status == ElementStatus.RUNNING)
                    {
                        ZenDebugPrint("(1000) : " + oEventCausedElement.ID + " is waiting " + oEventCausedElementChild.ID + " to finish...");
                        Thread.Sleep(10);
                    }
                    //Prvi pogoj   : Child ne sme še obstajati v kolekciji
                    //Drugi pogoj  : Če ima AND operator, ne sme obstajati false conditiona
                    //tretji pogoj  : Če ima OR operator, mora obstajati vsaj en true condition
                    if (!aStartPlg.Contains(oEventCausedElementChild) &&
                        (oEventCausedElementChild.Operator == "&" && !aChildConditions.Contains(false)
                        || oEventCausedElementChild.Operator == "||" && aChildConditions.Contains(true)))
                        aStartPlg.Add(oEventCausedElementChild);
                }

                if (oEventCausedElement.DisconnectedElements != null)
                    aStartPlg.AddRange(oEventCausedElement.DisconnectedElements);

                StartElementsHelper(oEventCausedElement, aStartPlg, aStopPlg);

                foreach (IGenericElement p in aStopPlg)
                    p.StopElementExecuting();

                aStopPlg = null;
                aStartPlg = null;
                aChildConditions = null;
                oEventCausedElement.DisconnectedElements = null;

                m_gadgeteerBoard.FireElementStopExecutingEvent(oEventCausedElement);
                oEventCausedElement.MsElapsed = (DateTime.Now.Subtract(oEventCausedElement.Started).Ticks / TimeSpan.TicksPerMillisecond);
                oEventCausedElement.LastExecutionDate = DateTime.Now;
            }
        }
        #endregion
        #endregion

        #region Fields
        #region m_DebuggerAttached
        bool m_DebuggerAttached;
        #endregion

        #region m_waitDebugger
        public ManualResetEvent m_waitDebugger = new ManualResetEvent(false);
        #endregion

        #region m_BreakpointWait
        public ManualResetEvent m_BreakpointWait = new ManualResetEvent(true);
        #endregion

        #region m_gadgeteerBoard
        public IGadgeteerBoard m_gadgeteerBoard;
        #endregion

        #region m_ImplementationList
        Hashtable m_CommonImplementationList = new Hashtable();
        #endregion

        #region m_AllElementChilds
        Hashtable m_AllElementChilds = new Hashtable();
        #endregion

        #region m_AllElements
        public Hashtable m_AllElements = new Hashtable();
        #endregion
        #endregion

        #region Event Declarations
        #region DebugDataReceived
        public event DebugDataReceivedEventHandler DebugDataReceived;
        #endregion

        #region DebugDataReceivedEventHandler
        public delegate void DebugDataReceivedEventHandler(object sender, DebugData e);
        #endregion
        #endregion

        #region Event Handlers
        #region IElement_ElementEvent
        void IElement_ElementEvent(object sender, params object[] e)
        {
            HandleElementEvent(sender as IGenericElement);
        }
        #endregion
        #endregion

        #region Private functions
        #region Core Related

        #region ZenDebugPrint
        void ZenDebugPrint(params string[] lines)
        {
            if (m_gadgeteerBoard.IsDebugEnabled)
            {
                foreach (string s in lines)
                    Console.WriteLine(s);
            }
        }
        #endregion

        #region ThreadSafeElementStart
        //If element is still running, wait until it became available
        //The problem arises in while loop. If PauseElement is signalled in middle of execution, it's discarged
        void ThreadSafeElementStart(IGenericElement p, IElement EventCausedElementID, ArrayList startElements, ArrayList stopElements)
        {
            lock (p.InstanceLock)
            {
                while (p.Status == ElementStatus.RUNNING)
                {
                    ZenDebugPrint("(1001) : " + EventCausedElementID.ID + " is waiting for " + p.ID + " to finish...");
                    Thread.Sleep(10);
                }

                AddStopElementsToList(p.FirstLevelTrueElements, stopElements);
                AddStopElementsToList(p.FirstLevelFalseElements, stopElements);

                p.IAmStartedYou = EventCausedElementID;
                if (!p.IsStarted)
                    new Task(() => p.StartElement(m_AllElements, true)).Start();
                else
                    p.PauseElement.Set();
            }
        }
        #endregion

        #region StartElementsHelper
        void StartElementsHelper(IElement EventCausedElementID, ArrayList startElements, ArrayList stopElements)
        {
            if (m_gadgeteerBoard.AttachDebugger && !m_DebuggerAttached)
            {
                m_gadgeteerBoard.PublishInfoPrint("WAITING FOR DEBUGGER ATTACH...", "systemInfo");
                Console.WriteLine("WAITING FOR DEBUGGER ATTACH...");
                m_waitDebugger.WaitOne();
                m_gadgeteerBoard.PublishInfoPrint("DEBUGGER ATTACHED...", "systemInfo");
                Console.WriteLine("DEBUGGER ATTACHED...");
                m_DebuggerAttached = true;
            }

            if (startElements.Count == 0)
            {
                if (DebugDataReceived != null)
                    DebugDataReceived(this, new DebugData(startElements));

                return;
            }

            if (m_gadgeteerBoard.IsBreakpointMode)
                m_BreakpointWait.WaitOne();

            m_BreakpointWait.Reset();

            foreach (IGenericElement p in startElements)
                new Task(() => ThreadSafeElementStart(p, EventCausedElementID, startElements, stopElements)).Start();

            if (DebugDataReceived != null)
                DebugDataReceived(this, new DebugData(startElements));
        }
        #endregion

        #region AddStopElementsToList
        private void AddStopElementsToList(ArrayList trueOrFalseElements, ArrayList stopElements)
        {
            if (trueOrFalseElements == null)
                return;

            foreach (IGenericElement pTrueElements in trueOrFalseElements)
            {
                if (!stopElements.Contains(pTrueElements) && pTrueElements.Status != ElementStatus.STOPPED)
                    stopElements.Add(pTrueElements);
            }
        }
        #endregion

        #region GetAllChilds
        static object _syncGetChilds = new object();
        IGenericElement[] GetTrueAndFalseChilds(IGenericElement element)
        {
            lock (_syncGetChilds)
            {
                if (!m_AllElementChilds.Contains(element.ID))
                {
                    int i = 0;
                    IGenericElement[] elements = new IGenericElement[element.FirstLevelFalseChilds.Count + element.FirstLevelTrueChilds.Count];
                    foreach (IGenericElement pFalseChild in element.FirstLevelFalseChilds)
                        elements[i++] = pFalseChild;

                    foreach (IGenericElement pTrueChild in element.FirstLevelTrueChilds)
                        elements[i++] = pTrueChild;

                    m_AllElementChilds.Add(element.ID, elements);
                }
                return m_AllElementChilds[element.ID] as IGenericElement[];
            }
        }
        #endregion

        #region SetLoopLockableElements
        void SetLoopLockableElements(IGenericElement startElement, IGenericElement currentElement, object syncObject)
        {
            if (currentElement.LoopLock != null)
                return;

            currentElement.LoopLock = syncObject;

            ZenDebugPrint(startElement.ID + " <-- " + currentElement.ID);

            if (currentElement.FirstLevelTrueElements != null && currentElement.FirstLevelTrueElements.Count > 0)
                foreach (IGenericElement p in currentElement.FirstLevelTrueElements)
                    SetLoopLockableElements(startElement, p, syncObject);

            if (currentElement.FirstLevelFalseElements != null && currentElement.FirstLevelFalseElements.Count > 0)
                foreach (IGenericElement p in currentElement.FirstLevelFalseElements)
                    SetLoopLockableElements(startElement, p, syncObject);

            if (currentElement.DisconnectedElements != null)
                foreach (IGenericElement p in currentElement.DisconnectedElements)
                    SetLoopLockableElements(startElement, p, syncObject);
        }
        #endregion

        #region SetLoopLockable
        void SetLoopLockable(IGenericElement startElement, IGenericElement currentElement, object syncObject)
        {
            if (currentElement.LoopLock != null)
                return;

            currentElement.LoopLock = syncObject;

            ZenDebugPrint(startElement.ID + " --> " + currentElement.ID);
            foreach (IGenericElement p in currentElement.FirstLevelTrueChilds)
                SetLoopLockable(startElement, p, syncObject);

            foreach (IGenericElement p in currentElement.FirstLevelFalseChilds)
                SetLoopLockable(startElement, p, syncObject);

            if (currentElement.DisconnectedElements != null)
                foreach (IGenericElement p in currentElement.DisconnectedElements)
                    SetLoopLockable(startElement, p, syncObject);
        }
        #endregion
        #endregion

        #region MakeVirtualConnections
        void MakeVirtualConnections()
        {
            foreach (DictionaryEntry p in m_AllElements)
            {
                if ((p.Value as IGenericElement).ImplementationModule as IZenExecutor != null)
                {
                    if ((p.Value as IGenericElement).DisconnectedElements == null)
                        (p.Value as IGenericElement).DisconnectedElements = new ArrayList();

                    foreach (string elementId in ((p.Value as IGenericElement).ImplementationModule as IZenExecutor).GetElementsToExecute((p.Value as IGenericElement), m_AllElements))
                    {
                        if (!string.IsNullOrEmpty(elementId) && !(p.Value as IGenericElement).DisconnectedElements.Contains(elementId))
                            (p.Value as IGenericElement).DisconnectedElements.Add(m_AllElements[elementId]);
                    }
                }
            }
        }
        #endregion

        #region ClearRam
        private void ClearRam()
        {
            m_DependenciesFileContent = null;
            m_ImplementationFileContent = null;
            m_ModulesFileContent = null;
            m_RelationFileContent = null;
            m_CommonImplementationList = null;

            foreach (DictionaryEntry p in m_AllElements)
            {
                if ((p.Value as IGenericElement).DisconnectedElements != null)
                    (p.Value as IGenericElement).DisconnectedElements = null;
            }
        }
        #endregion

        #region CreateOrReuseImplementation
        private ImplementationData CreateOrReuseImplementation(string[] ImplementationParams, bool CreateNewInstance)
        {
            if (!CreateNewInstance)
                return (ImplementationData)m_CommonImplementationList[ImplementationParams[1]];

            IZenImplementation oImplementation = Utils.LoadImplementation(Path.Combine(m_gadgeteerBoard.TemplateRootDirectory, "Implementations", ImplementationParams[0]));
            oImplementation.ID = ImplementationParams[1];
            oImplementation.ParentBoard = m_gadgeteerBoard;

            return new ImplementationData(oImplementation, ImplementationParams);
        }
        #endregion

        #region Initial fills
        #region FillImplementationList
        //Create implementations. Later, new implementation for node will be created, or this one will be reused
        private void FillImplementationList()
        {
            List<string> tmpImplementations = new List<string>();
            foreach (string sImplementation in ImplementationClassFileContent.Split(';'))
            {
                if (sImplementation.Trim() != "")
                {
                    ImplementationData implementation = CreateOrReuseImplementation(sImplementation.Split(','), true);
                    m_CommonImplementationList.Add(implementation.Implementation.ID, implementation);
                    if (!tmpImplementations.Contains(sImplementation.Split(',')[0]))
                    {
                        tmpImplementations.Add(sImplementation.Split(',')[0]);
                        IZenImplementationInit oImplementationInitialisableModule = implementation.Implementation as IZenImplementationInit;
                        if (oImplementationInitialisableModule != null)
                            oImplementationInitialisableModule.OnImplementationInit(sImplementation.Split(',')[3]);
                    }
                }
            }
        }
        #endregion

        #region FillElementList
        private void FillElementList()
        {
            foreach (DictionaryEntry element in ModulesFileContent)
            {
                IGenericElement myElement = new GenericElement();
                myElement.InstanceLock = new object();
                Hashtable currentElement = JsonConvert.DeserializeObject<Hashtable>(element.Value.ToString());
                myElement.Conditions = JsonConvert.DeserializeObject<Hashtable>(currentElement["ELEMENT_PROPERTIES"].ToString());
                myElement.ParentBoard = this.m_gadgeteerBoard;
                myElement.ID = element.Key.ToString();

                //Shared  class
                ImplementationData implementationData = m_CommonImplementationList[currentElement["IMPLEMENTATION"].ToString()] as ImplementationData;
                myElement.ImplementationModule = CreateOrReuseImplementation(implementationData.ImplementationParams, currentElement["IS_SHARED"].ToString() != "1").Implementation;

                //Debug
                myElement.DoDebug = currentElement["DO_DEBUG"].ToString() == "1";

                //Operator
                myElement.Operator = currentElement["OPERATOR"].ToString();

                if (currentElement["START_TRIGGERS"] != null && currentElement["START_TRIGGERS"].ToString().Trim() != "")
                {
                    myElement.ManuallyTriggeredStartElements = new ArrayList();
                    foreach (string s in currentElement["START_TRIGGERS"].ToString().Split('-'))
                    {
                        if (s.Trim() != "")
                            myElement.ManuallyTriggeredStartElements.Add(s.Trim());
                    }
                }

                //Unregister Event
                myElement.UnregisterEvent = currentElement["UNREGISTER_EVENT"].ToString() == "1";
                myElement.ElementEvent += new ElementEventHandler(IElement_ElementEvent);
                myElement.Status = ElementStatus.STOPPED;
                m_AllElements.Add(myElement.ID, myElement);
            }

            foreach (DictionaryEntry p in m_AllElements)
            {
                //m_AllElements must be filled before ManuallyTriggeredStartElement are set
                if ((p.Value as IGenericElement).ManuallyTriggeredStartElements != null)
                {
                    ArrayList al = new ArrayList();
                    foreach (string s in (p.Value as IGenericElement).ManuallyTriggeredStartElements)
                        al.Add(m_AllElements[s]);

                    (p.Value as IGenericElement).ManuallyTriggeredStartElements = null;
                    (p.Value as IGenericElement).ManuallyTriggeredStartElements = al;
                }

                //Fire PreInit event on the main thread
                IZenElementPreInit oNodePreInit = (p.Value as IElement).ImplementationModule as IZenElementPreInit;
                if (oNodePreInit != null)
                    oNodePreInit.OnElementPreInit((p.Value as IElement), m_AllElements);
            }
        }

        #endregion

        #region FillRelationsList
        private void FillRelationsList()
        {
            ZenDebugPrint("", "------------DEFINING RELATIONS-------------");
            foreach (string sElements in RelationFileContent.Split(';'))
            {
                if (sElements.Trim() != "")
                {
                    string[] aSettings = sElements.Split(',');
                    string sID = aSettings[0];
                    string sTrueChilds = aSettings[1];
                    string sFalseChilds = aSettings[2];

                    IGenericElement oCurrentElement = m_AllElements[sID] as IGenericElement;
                    ArrayList al = new ArrayList();
                    foreach (string sChild in sTrueChilds.Split('|'))
                    {
                        if (sChild.Trim() != "")
                            al.Add(m_AllElements[sChild]);
                    }

                    oCurrentElement.FirstLevelTrueChilds = al;
                    //Dodaj še kontra relacijo -> vsem childom tega elementa
                    foreach (IGenericElement oTrueChild in oCurrentElement.FirstLevelTrueChilds)
                    {
                        if (oTrueChild.FirstLevelTrueElements == null)
                            oTrueChild.FirstLevelTrueElements = new ArrayList();

                        oTrueChild.FirstLevelTrueElements.Add(oCurrentElement);
                        ZenDebugPrint(oCurrentElement.ID + " -> " + oTrueChild.ID);
                    }

                    al = new ArrayList();
                    foreach (string sChild in sFalseChilds.Split('|'))
                    {
                        if (sChild.Trim() != "")
                        {
                            al.Add(m_AllElements[sChild]);
                            ZenDebugPrint(sID + " -> " + sChild);
                        }
                    }

                    oCurrentElement.FirstLevelFalseChilds = al;
                    foreach (IGenericElement oFalseChild in oCurrentElement.FirstLevelFalseChilds)
                    {
                        if (oFalseChild.FirstLevelFalseElements == null)
                            oFalseChild.FirstLevelFalseElements = new ArrayList();

                        oFalseChild.FirstLevelFalseElements.Add(oCurrentElement);
                        ZenDebugPrint(oCurrentElement.ID + " -> " + oFalseChild.ID);
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Queries
        #region GetStartConditionString
        private void GetStartConditionString(IGenericElement p)
        {
            Hashtable conditions = p.Conditions as Hashtable;
            //Is this a start element -> check with properties
            if (conditions["DEPENDENCIES"] == null && conditions["REPEATABLE"] == null && conditions["ACTIVE"] == null)
                return;

            //Is start element already handled?
            if (conditions.Contains("IS_START_ELEMENT"))
                return;

            if (conditions["DEPENDENCIES"] != null)
            {
                string[] aDependency = conditions["DEPENDENCIES"].ToString().Split(',');
                for (int i = 0; i < aDependency.Length; i++)
                {
                    if (aDependency[i].Trim() != "")
                    {
                        IGenericElement oPlu = m_AllElements[aDependency[i]] as IGenericElement;
                        if ((oPlu.ImplementationModule as IZenStart).Dependencies == null)
                            (oPlu.ImplementationModule as IZenStart).Dependencies = new ArrayList();
                        (oPlu.ImplementationModule as IZenStart).Dependencies.Add(p);
                    }
                }
            }

            if (conditions["REPEATABLE"].ToString() == "1")
                (p.ImplementationModule as IZenStart).IsRepeatable = true;
            else
                (p.ImplementationModule as IZenStart).IsRepeatable = false;

            if (conditions["ACTIVE"].ToString() == "1")
                (p.ImplementationModule as IZenStart).IsActive = true;
            else
                (p.ImplementationModule as IZenStart).IsActive = false;

            conditions.Add("IS_START_ELEMENT", true);
        }
        #endregion

        #region FindImplementationClass
        private string FindImplementationClass(string ID)
        {
            foreach (string sImplementation in ImplementationClassFileContent.Split(';'))
            {
                if (sImplementation != "")
                {
                    string[] a = sImplementation.Split(',');
                    if (a[1] == ID)
                        return a[0];
                }
            }
            return "";
        }
        #endregion
        #endregion
        #endregion

        #region Properties
        #region IsDebugInProgress
        public bool IsDebugInProgress
        {
            get { return DebugDataReceived != null; }
        }
        #endregion

        #region StartElementsCollection
        private ArrayList StartElementsCollection
        {
            get
            {
                ArrayList alReturn = new ArrayList();
                foreach (DictionaryEntry p in m_AllElements)
                {
                    if ((p.Value as IGenericElement).ImplementationModule as IZenStart != null)
                    {
                        GetStartConditionString((p.Value as IGenericElement));
                        alReturn.Add(p.Value);
                    }
                }

                foreach (DictionaryEntry p in m_AllElements)
                {
                    if ((p.Value as IGenericElement).ImplementationModule as IZenStart != null && ((p.Value as IGenericElement).ImplementationModule as IZenStart).Dependencies != null)
                    {
                        foreach (IGenericElement p1 in ((p.Value as IGenericElement).ImplementationModule as IZenStart).Dependencies)
                            alReturn.Remove(p1);
                    }
                }
                return alReturn;
            }
        }
        #endregion

        #region File Contents
        #region RelationFileContent
        string m_RelationFileContent;
        string RelationFileContent
        {
            get
            {
                if (m_RelationFileContent == null)
                    m_RelationFileContent = m_gadgeteerBoard.Relations;

                return m_RelationFileContent;
            }
        }
        #endregion

        #region ModulesFileContent
        Hashtable m_ModulesFileContent;
        Hashtable ModulesFileContent
        {
            get
            {
                if (m_ModulesFileContent == null)
                    m_ModulesFileContent = m_gadgeteerBoard.Modules;

                return m_ModulesFileContent;
            }
        }
        #endregion

        #region ImplementationClassFileContent
        string m_ImplementationFileContent;
        public string ImplementationClassFileContent
        {
            get
            {
                if (m_ImplementationFileContent == null)
                    m_ImplementationFileContent = m_gadgeteerBoard.Implementations;

                return m_ImplementationFileContent;
            }
        }
        #endregion

        #region DependenciesFileContent
        string m_DependenciesFileContent;
        string DependenciesFileContent
        {
            get
            {
                if (m_DependenciesFileContent == null)
                    m_DependenciesFileContent = m_gadgeteerBoard.Dependencies;

                return m_DependenciesFileContent;
            }
        }
        #endregion
        #endregion
        #endregion

        #region Public functions
        #region StartElements
        public void StartElements(IGadgeteerBoard ParentBoard)
        {
            m_gadgeteerBoard = ParentBoard;
            FillImplementationList();
            FillElementList();
            FillRelationsList();
            MakeVirtualConnections();

            //Sync lock of elements inside loops
            ZenDebugPrint("", "------------DEFINING LOCKS-----------");
            foreach (IGenericElement e in StartElementsCollection)
            {
                if (!(e.ImplementationModule as IZenStart).IsActive)
                    continue;

                object syncObject = new object();
                e.LoopLock = syncObject;

                foreach (IGenericElement childElement in e.FirstLevelTrueChilds)
                    SetLoopLockable(e, childElement, syncObject);

                foreach (IGenericElement childElement in e.FirstLevelFalseChilds)
                    SetLoopLockable(e, childElement, syncObject);

                if (e.FirstLevelTrueElements != null && e.FirstLevelTrueElements.Count > 0)
                    foreach (IGenericElement childElement in e.FirstLevelTrueElements)
                        SetLoopLockableElements(e, childElement, syncObject);

                if (e.FirstLevelFalseElements != null && e.FirstLevelFalseElements.Count > 0)
                    foreach (IGenericElement childElement in e.FirstLevelFalseElements)
                        SetLoopLockableElements(e, childElement, syncObject);
            }

            ClearRam();

            ZenDebugPrint("");
            ZenDebugPrint("#### LOCKS STATE ####");
            foreach (DictionaryEntry p in m_AllElements)
                ZenDebugPrint((p.Value as IGenericElement).ID + " : " + ((p.Value as IGenericElement).LoopLock == null ? "" : (p.Value as IGenericElement).LoopLock.GetHashCode().ToString()));

            new Task(() =>
            {
                foreach (IGenericElement p in StartElementsCollection)
                {
                    if (!(p.ImplementationModule as IZenStart).IsActive)
                        continue;

                    p.StartElement(m_AllElements, true);
                }
            }).Start();
        }
        #endregion
        #endregion
    }
}
