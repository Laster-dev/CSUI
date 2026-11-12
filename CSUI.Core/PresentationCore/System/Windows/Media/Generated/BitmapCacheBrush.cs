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
    public sealed partial class BitmapCacheBrush : Brush
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
        public new BitmapCacheBrush Clone()
        {
            return (BitmapCacheBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapCacheBrush CloneCurrentValue()
        {
            return (BitmapCacheBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void TargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(TargetProperty);
        }
        private static void BitmapCachePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            BitmapCacheBrush target = ((BitmapCacheBrush)d);
            target.PropertyChanged(BitmapCacheProperty);
        }
        private static void AutoLayoutContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(AutoLayoutContentProperty);
        }
        private static void InternalTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush)d);
            Visual oldV = (Visual)e.OldValue;
            if (target._pendingLayout && oldV != null)
            {
                Visual.UnsubscribeLayoutUpdatedHook?.Invoke(oldV, target.OnLayoutUpdated);
                target._DispatcherLayoutResult?.Abort();
                target._pendingLayout = false;
            }
            target.PropertyChanged(InternalTargetProperty);
        }
        private static void AutoWrapTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(AutoWrapTargetProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Target - Visual.  Default value is null.
        /// </summary>
        public Visual Target
        {
            get
            {
                return (Visual)GetValue(TargetProperty);
            }
            set
            {
                SetValueInternal(TargetProperty, value);
            }
        }

        /// <summary>
        ///     BitmapCache - BitmapCache.  Default value is null.
        /// </summary>
        public BitmapCache BitmapCache
        {
            get
            {
                return (BitmapCache)GetValue(BitmapCacheProperty);
            }
            set
            {
                SetValueInternal(BitmapCacheProperty, value);
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

        /// <summary>
        ///     InternalTarget - Visual.  Default value is null.
        /// </summary>
        internal Visual InternalTarget
        {
            get
            {
                return (Visual)GetValue(InternalTargetProperty);
            }
            set
            {
                SetValueInternal(InternalTargetProperty, value);
            }
        }

        /// <summary>
        ///     AutoWrapTarget - bool.  Default value is false.
        ///     If true, this Brush wrap its Target visual in a ContainerVisual, which will allow 
        ///     the brush to support rendering all properties on the visual above the cache to 
        ///     match VisualBrush's behavior.
        /// </summary>
        internal bool AutoWrapTarget
        {
            get
            {
                return (bool)GetValue(AutoWrapTargetProperty);
            }
            set
            {
                SetValueInternal(AutoWrapTargetProperty, BooleanBoxes.Box(value));
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
            return new BitmapCacheBrush();
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
        //    Target
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
        ///     The DependencyProperty for the BitmapCacheBrush.Target property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.BitmapCache property.
        /// </summary>
        public static readonly DependencyProperty BitmapCacheProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.AutoLayoutContent property.
        /// </summary>
        public static readonly DependencyProperty AutoLayoutContentProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.InternalTarget property.
        /// </summary>
        internal static readonly DependencyProperty InternalTargetProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.AutoWrapTarget property.
        /// </summary>
        internal static readonly DependencyProperty AutoWrapTargetProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const bool c_AutoLayoutContent = true;
        internal const bool c_AutoWrapTarget = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapCacheBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(BitmapCacheBrush);
            StaticInitialize(typeofThis);
            TargetProperty =
                  RegisterProperty("Target",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(TargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BitmapCacheProperty =
                  RegisterProperty("BitmapCache",
                                   typeof(BitmapCache),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BitmapCachePropertyChanged),
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
            InternalTargetProperty =
                  RegisterProperty("InternalTarget",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(InternalTargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AutoWrapTargetProperty =
                  RegisterProperty("AutoWrapTarget",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(AutoWrapTargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
