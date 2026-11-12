// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Utility;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows.Markup;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{

    public abstract partial class Geometry : Animatable, IFormattable
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new Geometry Clone()
        {
            return (Geometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new Geometry CloneCurrentValue()
        {
            return (Geometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Geometry target = ((Geometry)d);
            target.TransformPropertyChangedHook(e);

            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(TransformProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Transform - Transform.  Default value is Transform.Identity.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return (Transform)GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods





        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties


        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public string ToString(IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal virtual string ConvertToString(string format, IFormatProvider provider)
        {
            return base.ToString();
        }
        /// <summary>
        /// Parse - returns an instance converted from the provided string
        /// using the current culture
        /// <param name="source"> string with Geometry data </param>
        /// </summary>
        public static Geometry Parse(string source)
        {
            IFormatProvider formatProvider = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            return MS.Internal.Parsers.ParseGeometry(source, formatProvider);
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the Geometry.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static Transform s_Transform = Transform.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static Geometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Transform == null || s_Transform.IsFrozen,
                "Detected context bound default value Geometry.s_Transform (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(Geometry);
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   Transform.Identity,
                                   new PropertyChangedCallback(TransformPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
