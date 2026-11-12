// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    /// <summary>
    /// The GuidelineSet object represents a set of guide lines
    /// used in rendering for better adjusting rendered figures to device pixel grid.
    /// </summary>
    public partial class GuidelineSet : Animatable
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        public GuidelineSet()
        {
        }

        /// <summary>
        /// Constructs a new GuidelineSet object.
        /// </summary>
        /// <param name="guidelinesX">Array of X coordinates that defines a set of vertical guidelines.</param>
        /// <param name="guidelinesY">Array of Y coordinates that defines a set of horizontal guidelines.</param>
        /// <param name="isDynamic">Usage flag: when true then rendering machine will detect animation state and apply subpixel animation behavior.</param>
        internal GuidelineSet(double[] guidelinesX, double[] guidelinesY, bool isDynamic)
        {
            if (guidelinesX != null)
            {
                // Dynamic guideline is defined by a pair of numbers: (coordinate, shift),
                // so the length of array should be even.
                Debug.Assert(!isDynamic || guidelinesX.Length % 2 == 0);

                GuidelinesX = new DoubleCollection(guidelinesX);
            }

            if (guidelinesY != null)
            {
                // Dynamic guideline is defined by a pair of numbers: (coordinate, shift),
                // so the length of array should be even.
                Debug.Assert(!isDynamic || guidelinesY.Length % 2 == 0);

                GuidelinesY = new DoubleCollection(guidelinesY);
            }

            IsDynamic = isDynamic;
        }

        /// <summary>
        /// Constructs a new GuidelineSet object.
        /// </summary>
        /// <param name="guidelinesX">Array of X coordinates that defines a set of vertical guidelines.</param>
        /// <param name="guidelinesY">Array of Y coordinates that defines a set of horizontal guidelines.</param>
        public GuidelineSet(double[] guidelinesX, double[] guidelinesY)
        {
            if (guidelinesX != null)
            {
                GuidelinesX = new DoubleCollection(guidelinesX);
            }

            if (guidelinesY != null)
            {
                GuidelinesY = new DoubleCollection(guidelinesY);
            }
        }

        #endregion Constructors
    }
}
