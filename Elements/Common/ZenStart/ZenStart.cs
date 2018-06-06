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

using ZenCommon;
using System;
using System.Collections;

namespace ZenStart
{
    public class ZenStart : IZenStart, IZenAction
    {
        #region Fields
        #region IsStarted
        bool IsStarted;
        #endregion
        #endregion

        #region IZenStartable implementations
        #region Properties
        #region Dependencies
        public ArrayList Dependencies { get; set; }
        #endregion

        #region State
        public string State { get; set; }
        #endregion

        #region IsRepeatable
        public bool IsRepeatable { get; set; }
        #endregion

        #region IsActive
        public bool IsActive { get; set; }
        #endregion
        #endregion
        #endregion

        #region IZenAction implementations
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
            element.IsConditionMet = !IsStarted || (IsStarted && (this as IZenStart).IsRepeatable);
            IsStarted = true;
            if (element.IsConditionMet)
                element.LastResultBoxed = DateTime.Now;
        }
        #endregion
        #endregion
        #endregion
    }
}
