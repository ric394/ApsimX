// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Paddock information about the supplement fed
    /// </summary>
    [Serializable]
    private class PaddockInfo
    {
        /// <summary>
        /// The name
        /// </summary>
        public string Name;

        /// <summary>
        /// The padd identifier
        /// </summary>
        public int PaddId;

        /// <summary>
        /// The supplement fed
        /// </summary>
        public SupplementRation SupptFed;   // Entry N is the supplement fed out N days ago

        /// <summary>
        /// For bail feeding
        /// </summary>
        public bool FeedSuppFirst = false;
    }
    
}
