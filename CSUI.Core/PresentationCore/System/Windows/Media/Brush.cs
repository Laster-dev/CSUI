// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: This file contains the implementation of Brush.
//              Brush is the abstract base class which describes how to fill 
//              a geometric area.
//
//

using MS.Internal;
using System.Windows.Media.Animation;

using CSUI.Core.Interop.DirectX;

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// Brush - 
    /// A brush is an object that represents a method to fill a plane.
    /// In addition to being able to fill a plane in an absolute way,
    /// Brushes are also able to adapt how they fill the plane to the
    /// size of the object that they are used to fill.
    /// </summary>
    public abstract partial class Brush : Animatable, IFormattable
    {       
        #region Constructors
        
        /// <summary>
        /// Protected constructor for Brush.  
        /// Sets all values to their defaults.  
        /// To set property values, use the constructor which accepts paramters
        /// </summary>
        protected Brush()
        {
        }

        #endregion Constructors

        #region ToString

        /// <summary>
        /// Parse - parse a Brush from a string (reproduces WPF SolidColorBrush string syntax without reflection).
        /// </summary>
        /// <param name="value"> String representation of a Brush. Cannot be null/empty. </param>
        internal static Brush Parse(string value)
        {
            return Parsers.ParseBrush(value, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS);
        }

        /// <summary>
        /// Can serialze "this" to a string
        /// </summary>
        internal virtual bool CanSerializeToString()
        {
            return false;
        }

        #endregion
    }
}
