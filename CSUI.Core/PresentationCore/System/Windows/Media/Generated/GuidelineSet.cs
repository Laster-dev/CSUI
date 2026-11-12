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
    public sealed partial class GuidelineSet : Animatable
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
        public new GuidelineSet Clone()
        {
            return (GuidelineSet)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GuidelineSet CloneCurrentValue()
        {
            return (GuidelineSet)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GuidelinesXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(GuidelinesXProperty);
        }
        private static void GuidelinesYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(GuidelinesYProperty);
        }
        private static void IsDynamicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(IsDynamicProperty);
        }


        #region Public Properties

        /// <summary>
        ///     GuidelinesX - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection GuidelinesX
        {
            get
            {
                return (DoubleCollection)GetValue(GuidelinesXProperty);
            }
            set
            {
                SetValueInternal(GuidelinesXProperty, value);
            }
        }

        /// <summary>
        ///     GuidelinesY - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection GuidelinesY
        {
            get
            {
                return (DoubleCollection)GetValue(GuidelinesYProperty);
            }
            set
            {
                SetValueInternal(GuidelinesYProperty, value);
            }
        }

        /// <summary>
        ///     IsDynamic - bool.  Default value is false.
        /// </summary>
        internal bool IsDynamic
        {
            get
            {
                return (bool)GetValue(IsDynamicProperty);
            }
            set
            {
                SetValueInternal(IsDynamicProperty, BooleanBoxes.Box(value));
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
            return new GuidelineSet();
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





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.GuidelinesX property.
        /// </summary>
        public static readonly DependencyProperty GuidelinesXProperty;
        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.GuidelinesY property.
        /// </summary>
        public static readonly DependencyProperty GuidelinesYProperty;
        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.IsDynamic property.
        /// </summary>
        internal static readonly DependencyProperty IsDynamicProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal static DoubleCollection s_GuidelinesX = DoubleCollection.Empty;
        internal static DoubleCollection s_GuidelinesY = DoubleCollection.Empty;
        internal const bool c_IsDynamic = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static GuidelineSet()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_GuidelinesX == null || s_GuidelinesX.IsFrozen,
                "Detected context bound default value GuidelineSet.s_GuidelinesX (See OS Bug #947272).");


            Debug.Assert(s_GuidelinesY == null || s_GuidelinesY.IsFrozen,
                "Detected context bound default value GuidelineSet.s_GuidelinesY (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(GuidelineSet);
            GuidelinesXProperty =
                  RegisterProperty("GuidelinesX",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(GuidelinesXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GuidelinesYProperty =
                  RegisterProperty("GuidelinesY",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(GuidelinesYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            IsDynamicProperty =
                  RegisterProperty("IsDynamic",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(IsDynamicPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
