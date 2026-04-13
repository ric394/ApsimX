// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Paddock and amount of ration
    /// </summary>
    [Serializable]
    public class SuppToStockType : SuppInfo
    {
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        [Units("-")]
        public string Paddock { get; set; }

        /// <summary>
        /// Gets or sets the amount of ration (kg).
        /// </summary>
        /// <value>
        /// The amount of ration (kg).
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the flag to feed supplement before pasture. Bail feeding.
        /// </summary>
        [Units("-")]
        public bool FeedSuppFirst { get; set; }
    }
}
