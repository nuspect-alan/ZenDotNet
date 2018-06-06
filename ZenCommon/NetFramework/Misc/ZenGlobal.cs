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

using System.Collections.Generic;

namespace ZenCommonNetFramework
{
    public class ZenGlobal
    {
        #region Fields
        #region GeneralSync
        //Global multipurpose sync objects collection
        public static readonly object GeneralSync = new object();
        #endregion

        #region StorageBag
        //Global storage bag for sharing objects between elements.
        public static Dictionary<string, object> StorageBag = new Dictionary<string, object>();
        #endregion
        #endregion

        #region Functions
        #region GetSync
        //Thread safe helper for getting locking object
        public static object GetSync(string SyncIdentifier)
        {
            lock (GeneralSync)
            {
                if (!StorageBag.ContainsKey(SyncIdentifier))
                {
                    object syncObject = new object();
                    StorageBag.Add(SyncIdentifier, syncObject);
                }
                return StorageBag[SyncIdentifier];
            }
        }
        #endregion
        #endregion
    }
}
