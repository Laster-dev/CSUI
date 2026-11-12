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
    public sealed partial class Pen : Animatable
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
        public new Pen Clone()
        {
            return (Pen)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new Pen CloneCurrentValue()
        {
            return (Pen)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void BrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            Pen target = ((Pen)d);
            target.PropertyChanged(BrushProperty);
        }
        private static void ThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(ThicknessProperty);
        }
        private static void StartLineCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(StartLineCapProperty);
        }
        private static void EndLineCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(EndLineCapProperty);
        }
        private static void DashCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(DashCapProperty);
        }
        private static void LineJoinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(LineJoinProperty);
        }
        private static void MiterLimitPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(MiterLimitProperty);
        }
        private static void DashStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            Pen target = ((Pen)d);
            target.PropertyChanged(DashStyleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Brush - Brush.  Default value is null.
        /// </summary>
        public Brush Brush
        {
            get
            {
                return (Brush)GetValue(BrushProperty);
            }
            set
            {
                SetValueInternal(BrushProperty, value);
            }
        }

        /// <summary>
        ///     Thickness - double.  Default value is 1.0.
        /// </summary>
        public double Thickness
        {
            get
            {
                return (double)GetValue(ThicknessProperty);
            }
            set
            {
                SetValueInternal(ThicknessProperty, value);
            }
        }

        /// <summary>
        ///     StartLineCap - PenLineCap.  Default value is PenLineCap.Flat.
        /// </summary>
        public PenLineCap StartLineCap
        {
            get
            {
                return (PenLineCap)GetValue(StartLineCapProperty);
            }
            set
            {
                SetValueInternal(StartLineCapProperty, value);
            }
        }

        /// <summary>
        ///     EndLineCap - PenLineCap.  Default value is PenLineCap.Flat.
        /// </summary>
        public PenLineCap EndLineCap
        {
            get
            {
                return (PenLineCap)GetValue(EndLineCapProperty);
            }
            set
            {
                SetValueInternal(EndLineCapProperty, value);
            }
        }

        /// <summary>
        ///     DashCap - PenLineCap.  Default value is PenLineCap.Square.
        /// </summary>
        public PenLineCap DashCap
        {
            get
            {
                return (PenLineCap)GetValue(DashCapProperty);
            }
            set
            {
                SetValueInternal(DashCapProperty, value);
            }
        }

        /// <summary>
        ///     LineJoin - PenLineJoin.  Default value is PenLineJoin.Miter.
        /// </summary>
        public PenLineJoin LineJoin
        {
            get
            {
                return (PenLineJoin)GetValue(LineJoinProperty);
            }
            set
            {
                SetValueInternal(LineJoinProperty, value);
            }
        }

        /// <summary>
        ///     MiterLimit - double.  Default value is 10.0.
        /// </summary>
        public double MiterLimit
        {
            get
            {
                return (double)GetValue(MiterLimitProperty);
            }
            set
            {
                SetValueInternal(MiterLimitProperty, value);
            }
        }

        /// <summary>
        ///     DashStyle - DashStyle.  Default value is DashStyles.Solid.
        /// </summary>
        public DashStyle DashStyle
        {
            get
            {
                return (DashStyle)GetValue(DashStyleProperty);
            }
            set
            {
                SetValueInternal(DashStyleProperty, value);
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
            return new Pen();
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
        ///     The DependencyProperty for the Pen.Brush property.
        /// </summary>
        public static readonly DependencyProperty BrushProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.Thickness property.
        /// </summary>
        public static readonly DependencyProperty ThicknessProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.StartLineCap property.
        /// </summary>
        public static readonly DependencyProperty StartLineCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.EndLineCap property.
        /// </summary>
        public static readonly DependencyProperty EndLineCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.DashCap property.
        /// </summary>
        public static readonly DependencyProperty DashCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.LineJoin property.
        /// </summary>
        public static readonly DependencyProperty LineJoinProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.MiterLimit property.
        /// </summary>
        public static readonly DependencyProperty MiterLimitProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.DashStyle property.
        /// </summary>
        public static readonly DependencyProperty DashStyleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_Thickness = 1.0;
        internal const PenLineCap c_StartLineCap = PenLineCap.Flat;
        internal const PenLineCap c_EndLineCap = PenLineCap.Flat;
        internal const PenLineCap c_DashCap = PenLineCap.Square;
        internal const PenLineJoin c_LineJoin = PenLineJoin.Miter;
        internal const double c_MiterLimit = 10.0;
        internal static DashStyle s_DashStyle = DashStyles.Solid;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static Pen()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_DashStyle == null || s_DashStyle.IsFrozen,
                "Detected context bound default value Pen.s_DashStyle (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(Pen);
            BrushProperty =
                  RegisterProperty("Brush",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BrushPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ThicknessProperty =
                  RegisterProperty("Thickness",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ThicknessPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            StartLineCapProperty =
                  RegisterProperty("StartLineCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Flat,
                                   new PropertyChangedCallback(StartLineCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            EndLineCapProperty =
                  RegisterProperty("EndLineCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Flat,
                                   new PropertyChangedCallback(EndLineCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            DashCapProperty =
                  RegisterProperty("DashCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Square,
                                   new PropertyChangedCallback(DashCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            LineJoinProperty =
                  RegisterProperty("LineJoin",
                                   typeof(PenLineJoin),
                                   typeofThis,
                                   PenLineJoin.Miter,
                                   new PropertyChangedCallback(LineJoinPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineJoinValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            MiterLimitProperty =
                  RegisterProperty("MiterLimit",
                                   typeof(double),
                                   typeofThis,
                                   10.0,
                                   new PropertyChangedCallback(MiterLimitPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            DashStyleProperty =
                  RegisterProperty("DashStyle",
                                   typeof(DashStyle),
                                   typeofThis,
                                   DashStyles.Solid,
                                   new PropertyChangedCallback(DashStylePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
