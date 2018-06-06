/*************************************************************************
 * Copyright (c) 2015, 2018 Zenodys BV
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
using System.Threading;
using ZenCommon;

namespace ComputingEngine
{
    public delegate void ElementEventHandler(object sender, params Object[] e);
    public delegate void ElementExceptionHandler(object sender, params Object[] e);

    public interface IGenericElement : IElement
    {
        #region events
        event ElementEventHandler ElementEvent;
        #endregion

        #region InstanceLock
        object InstanceLock { get; set; }
        #endregion

        DateTime LastCacheFill { get; set; }
        IGadgeteerBoard ParentBoard { get; set; }
        bool IsStarted { get; set; }
        bool UnregisterEvent { get; set; }
        AutoResetEvent PauseElement { get; }
        ArrayList ManuallyTriggeredStartElements { get; set; }
        ArrayList FirstLevelTrueChilds { get; set; }
        ArrayList FirstLevelFalseChilds { get; set; }
        ArrayList FirstLevelTrueElements { get; set; }
        ArrayList FirstLevelFalseElements { get; set; }
        string Operator { get; set; }

        void StopElementExecuting();
    }
}
