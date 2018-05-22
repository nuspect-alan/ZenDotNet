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
