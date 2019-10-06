/*************************************************************************
 * Copyright (c) 2015, 2019 Zenodys BV
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Contributors:
 *    Tomaž Vinko
 *   
 **************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using ZenCommon;

namespace ZenGwSystemInfo
{
#if NETCOREAPP2_0
    public class ZenGwSystemInfo
#else
    public class ZenGwSystemInfo : IZenAction, IZenElementInit
#endif

    {
        #region Fields
        #region _htMeassurements
        Hashtable _htMeassurements = new Hashtable();
        #endregion

#if NETCOREAPP2_0
        #region _process
        Process _process;
        #endregion
#else
        #region _cpuProcessorTime
        PerformanceCounter _cpuProcessorTime;
        #endregion

        #region _cpuUserTime
        PerformanceCounter _cpuUserTime;
        #endregion

        #region _cpuPrivilegedTime
        PerformanceCounter _cpuPrivilegedTime;
        #endregion

        #region _ramCounter
        PerformanceCounter _ramCounter;
        #endregion
#endif
        #endregion

#if NETCOREAPP2_0
        #region Fields
        #region _implementations
        static Dictionary<string, ZenGwSystemInfo> _implementations = new Dictionary<string, ZenGwSystemInfo>();
        #endregion

        #region _userProcessor
        CpuUsage _userProcessor;
        #endregion

        #region _privilegedProcessor
        CpuUsage _privilegedProcessor;
        #endregion

        #region _totalProcessor
        CpuUsage _totalProcessor;
        #endregion
        #endregion

        #region InitUnmanagedElements
        unsafe public static void InitUnmanagedElements(string currentElementId, void** elements, int elementsCount, int isManaged, string projectRoot, string projectId, ZenNativeHelpers.GetElementProperty getElementPropertyCallback, ZenNativeHelpers.GetElementResultInfo getElementResultInfoCallback, ZenNativeHelpers.GetElementResult getElementResultCallback, ZenNativeHelpers.ExecuteElement executeElementCallback, ZenNativeHelpers.SetElementProperty setElementProperty, ZenNativeHelpers.AddEventToBuffer addEventToBuffer)
        {
            if (!_implementations.ContainsKey(currentElementId))
                _implementations.Add(currentElementId, new ZenGwSystemInfo());

            ZenNativeHelpers.InitUnmanagedElements(currentElementId, elements, elementsCount, isManaged, projectRoot, projectId, getElementPropertyCallback, getElementResultInfoCallback, getElementResultCallback, executeElementCallback, setElementProperty, addEventToBuffer);
            _implementations[currentElementId].ElementInit(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement);
        }
        #endregion

        #region ExecuteAction
        unsafe public static void ExecuteAction(string currentElementId, void** elements, int elementsCount, IntPtr result)
        {
            _implementations[currentElementId].Meassure(ZenNativeHelpers.Elements, ZenNativeHelpers.Elements[currentElementId] as IElement);
            ZenNativeHelpers.CopyManagedStringToUnmanagedMemory(string.Empty, result);
        }
        #endregion
#else
        #region IZenElementInit implementations
        #region OnElementInit
        public void OnElementInit(Hashtable elements, IElement element)
        {
            ElementInit(elements, element);
        }
        #endregion
        #endregion

        #region IZenAction Implementations
        #region Properties
        #region ID
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

        #region Functions
        #region ExecuteAction
        public void ExecuteAction(Hashtable elements, IElement element, IElement iAmStartedYou)
        {
            Meassure(elements, element);
        }
        #endregion
        #endregion
        #endregion
#endif

        #region Functions

        #region Meassure
        public void Meassure(Hashtable elements, IElement element)
        {
#if NETCOREAPP2_0
            if (element.GetElementProperty("USER_TIME_ACTIVE") == "1")
            {
                _userProcessor.CallCPU(_process.UserProcessorTime);
                _htMeassurements["UserTime"] = _userProcessor.GetInfoTotal();
            }

            if (element.GetElementProperty("PRIVILEGED_TIME_ACTIVE") == "1")
            {
                _privilegedProcessor.CallCPU(_process.PrivilegedProcessorTime);
                _htMeassurements["PrivilegedTime"] = _privilegedProcessor.GetInfoTotal();
            }

            if (element.GetElementProperty("PROCESSOR_TIME_ACTIVE") == "1")
            {
                _totalProcessor.CallCPU(_process.TotalProcessorTime);
                _htMeassurements["ProcessorTime"] = _totalProcessor.GetInfoTotal();
            }

            if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT_ACTIVE") == "1")
                _htMeassurements["RamCounter"] = (element.GetElementProperty("AVAILABLE_MEMORY_UNIT") == "KB")
                ? _process.WorkingSet64 / 1024.0 :
                KilobytesToMegabytes(_process.WorkingSet64 / 1024);
#else
                MeassureCommon(element);
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        MeassureWinSpecific(element);
                        break;

                    default:
                        MeassureUnixSpecific(element);
                        break;
                }
#endif
            element.IsConditionMet = true;
            element.LastResultBoxed = JsonConvert.SerializeObject(_htMeassurements);
        }
        #endregion

        #region ElementInit
        public void ElementInit(Hashtable elements, IElement element)
        {
#if NETCOREAPP2_0
            _process = Process.GetCurrentProcess();

            _userProcessor = new CpuUsage(_process.UserProcessorTime);
            _privilegedProcessor = new CpuUsage(_process.PrivilegedProcessorTime);
            _totalProcessor = new CpuUsage(_process.TotalProcessorTime);
#else
            if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT_ACTIVE") == "1")
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT") == "KB")
                        _ramCounter = new PerformanceCounter("Memory", "Available KBytes");
                    else
                        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
            }

            if (element.GetElementProperty("USER_TIME_ACTIVE") == "1")
            {
                _cpuUserTime = new PerformanceCounter();
                _cpuUserTime.CategoryName = "Processor";
                _cpuUserTime.CounterName = "% User Time";
                _cpuUserTime.InstanceName = "_Total";

            }

            if (element.GetElementProperty("PRIVILEGED_TIME_ACTIVE") == "1")
            {
                _cpuPrivilegedTime = new PerformanceCounter();
                _cpuPrivilegedTime.CategoryName = "Processor";
                _cpuPrivilegedTime.CounterName = "% Privileged Time";
                _cpuPrivilegedTime.InstanceName = "_Total";
            }

            if (element.GetElementProperty("PROCESSOR_TIME_ACTIVE") == "1")
            {
                _cpuProcessorTime = new PerformanceCounter();
                _cpuProcessorTime.CategoryName = "Processor";
                _cpuProcessorTime.CounterName = "% Processor Time";
                _cpuProcessorTime.InstanceName = "_Total";
            }
#endif
        }
        #endregion

        #region Common
        #region KilobytesToMegabytes
        double KilobytesToMegabytes(long kilobytes)
        {
            return kilobytes / 1024f;
        }
        #endregion
        #endregion

#if !NETCOREAPP2_0
        #region MeassureCommon
        void MeassureCommon(IElement element)
        {
            if (element.GetElementProperty("USER_TIME_ACTIVE") == "1")
                _htMeassurements["UserTime"] = _cpuUserTime.NextValue();

            if (element.GetElementProperty("PRIVILEGED_TIME_ACTIVE") == "1")
                _htMeassurements["PrivilegedTime"] = _cpuPrivilegedTime.NextValue();

            if (element.GetElementProperty("PROCESSOR_TIME_ACTIVE") == "1")
                _htMeassurements["ProcessorTime"] = _cpuProcessorTime.NextValue();
        }
        #endregion

        #region Win

        void MeassureWinSpecific(IElement element)
        {
            if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT_ACTIVE") == "1")
                _htMeassurements["RamCounter"] = _ramCounter.NextValue();

        }
        #endregion

        #region Unix
        void MeassureUnixSpecific(IElement element)
        {
            if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT_ACTIVE") == "1")
            {
                FreeCSharp free = new FreeCSharp();
                free.GetValues();

                long buffersPlusCached = free.Buffers + free.Cached;
                long mainUsed = free.MemTotal - free.MemFree;
                long ramCounter = mainUsed - buffersPlusCached;
                //Console.WriteLine("Used physical memory: {0} kB", mainUsed - buffersPlusCached);
                //Console.WriteLine("Available physical memory: {0} kB", free.MemFree + buffersPlusCached);

                if (element.GetElementProperty("AVAILABLE_MEMORY_UNIT") == "MB")
                    _htMeassurements["RamCounter"] = (long)KilobytesToMegabytes(ramCounter);
                else
                    _htMeassurements["RamCounter"] = ramCounter;
            }
        }
        
        #endregion
#endif
        #endregion

    }

}
