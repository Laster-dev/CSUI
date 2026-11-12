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
    public sealed partial class DashStyle : Animatable
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
        public new DashStyle Clone()
        {
            return (DashStyle)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DashStyle CloneCurrentValue()
        {
            return (DashStyle)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void OffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DashStyle target = ((DashStyle) d);


            target.PropertyChanged(OffsetProperty);
        }
        private static void DashesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            DashStyle target = ((DashStyle) d);


            target.PropertyChanged(DashesProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Offset - double.  Default value is 0.0.
        /// </summary>
        public double Offset
        {
            get
            {
                return (double)GetValue(OffsetProperty);
            }
            set
            {
                SetValueInternal(OffsetProperty, value);
            }
        }

        /// <summary>
        ///     Dashes - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection Dashes
        {
            get
            {
                return (DoubleCollection)GetValue(DashesProperty);
            }
            set
            {
                SetValueInternal(DashesProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DashStyle();
        }



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

        //
        //  This property finds the correct initial size for the _effectiveValues store on the
        //  current DependencyObject as a performance optimization
        //
        //  This includes:
        //    Dashes
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 1;
            }
        }



        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the DashStyle.Offset property.
        /// </summary>
        public static readonly DependencyProperty OffsetProperty;
        /// <summary>
        ///     The DependencyProperty for the DashStyle.Dashes property.
        /// </summary>
        public static readonly DependencyProperty DashesProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_Offset = 0.0;
        internal static DoubleCollection s_Dashes = DoubleCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DashStyle()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Dashes == null || s_Dashes.IsFrozen,
                "Detected context bound default value DashStyle.s_Dashes (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(DashStyle);
            OffsetProperty =
                  RegisterProperty("Offset",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            DashesProperty =
                  RegisterProperty("Dashes",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(DashesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
