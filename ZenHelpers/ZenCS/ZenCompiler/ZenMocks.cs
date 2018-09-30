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
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZenCommon;

namespace ZenCS
{
    internal class ZenMocks
    {
        #region Fields
        #region elements
        static Hashtable _elements = new Hashtable();
        #endregion

        #region _board
        static IGadgeteerBoard _board = new GadgeteerBoard(string.Empty, "compile");
        #endregion
        #endregion

        #region Properties
        #region Board
        internal static IGadgeteerBoard Board
        {
            get
            {
                return _board;
            }
        }
        #endregion

        #region Elements
        internal static Hashtable Elements
        {
            get
            {
                return _elements;
            }
        }
        #endregion

        #region Element
        internal static IElement Element
        {
            get
            {
                if (_elements.Count == 0)
                    throw new Exception("At least one element must be defined in res/mocks.csv file!");

                return _elements[0] as IElement;
            }
        }
        #endregion
        #endregion

        #region Functions
        #region FillMocks
        internal static void FillMocks()
        {
            IGadgeteerBoard mockBoard = new GadgeteerBoard(string.Empty, "compile");
            foreach (string mockElement in File.ReadAllText("mocks.csv").Split(Environment.NewLine))
            {
                string[] element = mockElement.Split(',');
                IElement e = new Element(element[0], IntPtr.Zero);
                e.IsManaged = true;
                e.LastResultBoxed = element[1];

                _elements.Add(e.ID, e);
            }
        }
        #endregion
        #endregion
    }
}
