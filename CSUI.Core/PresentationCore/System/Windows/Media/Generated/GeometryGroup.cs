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
    public sealed partial class GeometryGroup : Geometry
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
        public new GeometryGroup Clone()
        {
            return (GeometryGroup)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GeometryGroup CloneCurrentValue()
        {
            return (GeometryGroup)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void FillRulePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GeometryGroup target = ((GeometryGroup) d);


            target.PropertyChanged(FillRuleProperty);
        }
        private static void ChildrenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            GeometryGroup target = ((GeometryGroup)d);
            target.PropertyChanged(ChildrenProperty);
        }


        #region Public Properties

        /// <summary>
        ///     FillRule - FillRule.  Default value is FillRule.EvenOdd.
        /// </summary>
        public FillRule FillRule
        {
            get
            {
                return (FillRule)GetValue(FillRuleProperty);
            }
            set
            {
                SetValueInternal(FillRuleProperty, FillRuleBoxes.Box(value));
            }
        }

        /// <summary>
        ///     Children - GeometryCollection.  Default value is new FreezableDefaultValueFactory(GeometryCollection.Empty).
        /// </summary>
        public GeometryCollection Children
        {
            get
            {
                return (GeometryCollection)GetValue(ChildrenProperty);
            }
            set
            {
                SetValueInternal(ChildrenProperty, value);
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
            return new GeometryGroup();
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
        //    Children
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
        ///     The DependencyProperty for the GeometryGroup.FillRule property.
        /// </summary>
        public static readonly DependencyProperty FillRuleProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryGroup.Children property.
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const FillRule c_FillRule = FillRule.EvenOdd;
        internal static GeometryCollection s_Children = GeometryCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static GeometryGroup()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Children == null || s_Children.IsFrozen,
                "Detected context bound default value GeometryGroup.s_Children (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(GeometryGroup);
            FillRuleProperty =
                  RegisterProperty("FillRule",
                                   typeof(FillRule),
                                   typeofThis,
                                   FillRule.EvenOdd,
                                   new PropertyChangedCallback(FillRulePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ChildrenProperty =
                  RegisterProperty("Children",
                                   typeof(GeometryCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(GeometryCollection.Empty),
                                   new PropertyChangedCallback(ChildrenPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
