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
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public sealed partial class VisualBrush : TileBrush
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
        public new VisualBrush Clone()
        {
            return (VisualBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new VisualBrush CloneCurrentValue()
        {
            return (VisualBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void VisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VisualBrush target = ((VisualBrush)d);
            Visual oldV = (Visual)e.OldValue;
            if (target._pendingLayout && oldV != null)
            {
                Visual.UnsubscribeLayoutUpdatedHook?.Invoke(oldV, target.OnLayoutUpdated);
                target._DispatcherLayoutResult?.Abort();
                target._pendingLayout = false;
            }
            target._isCacheDirty = true;
            target.PropertyChanged(VisualProperty);
        }
        private static void AutoLayoutContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VisualBrush target = ((VisualBrush) d);


            target.PropertyChanged(AutoLayoutContentProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Visual - Visual.  Default value is null.
        /// </summary>
        public Visual Visual
        {
            get
            {
                return (Visual)GetValue(VisualProperty);
            }
            set
            {
                SetValueInternal(VisualProperty, value);
            }
        }

        /// <summary>
        ///     AutoLayoutContent - bool.  Default value is true.
        ///     If this property is true, this Brush will run layout on the contents of this Brush 
        ///     if the Visual is a non-parented UIElement.
        /// </summary>
        public bool AutoLayoutContent
        {
            get
            {
                return (bool)GetValue(AutoLayoutContentProperty);
            }
            set
            {
                SetValueInternal(AutoLayoutContentProperty, BooleanBoxes.Box(value));
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
            return new VisualBrush();
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
        //    Visual
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
        ///     The DependencyProperty for the VisualBrush.Visual property.
        /// </summary>
        public static readonly DependencyProperty VisualProperty;
        /// <summary>
        ///     The DependencyProperty for the VisualBrush.AutoLayoutContent property.
        /// </summary>
        public static readonly DependencyProperty AutoLayoutContentProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const bool c_AutoLayoutContent = true;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static VisualBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(VisualBrush);
            VisualProperty =
                  RegisterProperty("Visual",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(VisualPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AutoLayoutContentProperty =
                  RegisterProperty("AutoLayoutContent",
                                   typeof(bool),
                                   typeofThis,
                                   true,
                                   new PropertyChangedCallback(AutoLayoutContentPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
