// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// A stored supplement name and quantity
    /// </summary>
    [Serializable]
    public class StoreType : SuppInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [Units("-")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the amount of supplement.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [Units("kg")]
        public double Stored { get; set; }
    }
}
