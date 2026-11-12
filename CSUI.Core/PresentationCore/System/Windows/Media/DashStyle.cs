// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: Implementation of the class DashStyle
//
//

using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    #region DashStyle
    /// <summary>
    /// This class captures the array of dashe and gap lengths and the dash offset.
    /// </summary>
    public partial class DashStyle : Animatable
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        public DashStyle()
        {
        }

        /// <summary>
        ///     Constructor from an array and offset
        /// <param name="dashes">
        ///     The array of lengths of dashes and gaps, measured in Thickness units.
        ///     If the value of dashes is null then the style will be solid
        /// </param>
        /// <param name="offset">
        ///     Determines where in the dash sequence the stroke will start
        /// </param>
        /// </summary>
        public DashStyle(IEnumerable<double> dashes, Double offset)
        {
            Offset = offset;

            if (dashes != null)
            {
                Dashes = new DoubleCollection(dashes);
            }
        }


        #endregion Constructors

 
        #region Internal Methods
        /// <summary>
        /// Returns the dashes information.
        /// </summary>
        internal float[] GetDashArray(out float dashOffset)
        {
            dashOffset = (float)Offset;
            DoubleCollection dashes = Dashes;
            int count = dashes != null ? dashes.Count : 0;
            if (count == 0)
            {
                return null;
            }

            float[] result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = (float)dashes[i];
            }

            return result;
        }
        #endregion Internal Methods
    }
    #endregion
}
