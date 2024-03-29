﻿/*************************************************************************
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

using ZenCommon;
using System.Collections;

public interface IZenCsScript
{
    string RawHtml { get; }
    object RunCustomCode(string functionName);
#if NETCOREAPP2_0
    void Init(Hashtable elements, IElement element);
#else
    void Init(Hashtable elements, IElement element, IZenCallable callable);
#endif
    int Order { get; }
}
