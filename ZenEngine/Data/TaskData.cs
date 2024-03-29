﻿/*************************************************************************
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
