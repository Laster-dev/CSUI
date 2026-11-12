// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Utility;
using System.Collections;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Effects
{
    public sealed partial class BlurEffect : Effect
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
        public new BlurEffect Clone()
        {
            return (BlurEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BlurEffect CloneCurrentValue()
        {
            return (BlurEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BlurEffect target = ((BlurEffect) d);


            target.PropertyChanged(RadiusProperty);
        }
        private static void KernelTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BlurEffect target = ((BlurEffect) d);


            target.PropertyChanged(KernelTypeProperty);
        }
        private static void RenderingBiasPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BlurEffect target = ((BlurEffect) d);


            target.PropertyChanged(RenderingBiasProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Radius - double.  Default value is 5.0.
        /// </summary>
        public double Radius
        {
            get
            {
                return (double)GetValue(RadiusProperty);
            }
            set
            {
                SetValueInternal(RadiusProperty, value);
            }
        }

        /// <summary>
        ///     KernelType - KernelType.  Default value is KernelType.Gaussian.
        /// </summary>
        public KernelType KernelType
        {
            get
            {
                return (KernelType)GetValue(KernelTypeProperty);
            }
            set
            {
                SetValueInternal(KernelTypeProperty, value);
            }
        }

        /// <summary>
        ///     RenderingBias - RenderingBias.  Default value is RenderingBias.Performance.
        /// </summary>
        public RenderingBias RenderingBias
        {
            get
            {
                return (RenderingBias)GetValue(RenderingBiasProperty);
            }
            set
            {
                SetValueInternal(RenderingBiasProperty, value);
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
            return new BlurEffect();
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
        ///     The DependencyProperty for the BlurEffect.Radius property.
        /// </summary>
        public static readonly DependencyProperty RadiusProperty;
        /// <summary>
        ///     The DependencyProperty for the BlurEffect.KernelType property.
        /// </summary>
        public static readonly DependencyProperty KernelTypeProperty;
        /// <summary>
        ///     The DependencyProperty for the BlurEffect.RenderingBias property.
        /// </summary>
        public static readonly DependencyProperty RenderingBiasProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_Radius = 5.0;
        internal const KernelType c_KernelType = KernelType.Gaussian;
        internal const RenderingBias c_RenderingBias = RenderingBias.Performance;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BlurEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(BlurEffect);
            RadiusProperty =
                  RegisterProperty("Radius",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(RadiusPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            KernelTypeProperty =
                  RegisterProperty("KernelType",
                                   typeof(KernelType),
                                   typeofThis,
                                   KernelType.Gaussian,
                                   new PropertyChangedCallback(KernelTypePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsKernelTypeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            RenderingBiasProperty =
                  RegisterProperty("RenderingBias",
                                   typeof(RenderingBias),
                                   typeofThis,
                                   RenderingBias.Performance,
                                   new PropertyChangedCallback(RenderingBiasPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsRenderingBiasValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
