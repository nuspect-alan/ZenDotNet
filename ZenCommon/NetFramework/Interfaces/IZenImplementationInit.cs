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

namespace ZenCommon
{
    //This is pre-element state. Method is fired just once, on initial implementation creation.
    //Method is useful for actions that are common to all elements of current implementation (dll copying, etc...) and do not require nodes data
    //It runs on main thread, and it's thread safe
    //Warning : boot time is affected
    public interface IZenImplementationInit
    {
        void OnImplementationInit(string args);
    }
}
