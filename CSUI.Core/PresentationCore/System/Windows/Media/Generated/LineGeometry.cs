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
    public sealed partial class LineGeometry : Geometry
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
        public new LineGeometry Clone()
        {
            return (LineGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new LineGeometry CloneCurrentValue()
        {
            return (LineGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void StartPointPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGeometry target = ((LineGeometry) d);


            target.PropertyChanged(StartPointProperty);
        }
        private static void EndPointPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGeometry target = ((LineGeometry) d);


            target.PropertyChanged(EndPointProperty);
        }


        #region Public Properties

        /// <summary>
        ///     StartPoint - Point.  Default value is new Point().
        /// </summary>
        public Point StartPoint
        {
            get
            {
                return (Point)GetValue(StartPointProperty);
            }
            set
            {
                SetValueInternal(StartPointProperty, value);
            }
        }

        /// <summary>
        ///     EndPoint - Point.  Default value is new Point().
        /// </summary>
        public Point EndPoint
        {
            get
            {
                return (Point)GetValue(EndPointProperty);
            }
            set
            {
                SetValueInternal(EndPointProperty, value);
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
            return new LineGeometry();
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
        ///     The DependencyProperty for the LineGeometry.StartPoint property.
        /// </summary>
        public static readonly DependencyProperty StartPointProperty;
        /// <summary>
        ///     The DependencyProperty for the LineGeometry.EndPoint property.
        /// </summary>
        public static readonly DependencyProperty EndPointProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal static Point s_StartPoint = new Point();
        internal static Point s_EndPoint = new Point();

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static LineGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(LineGeometry);
            StartPointProperty =
                  RegisterProperty("StartPoint",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   new PropertyChangedCallback(StartPointPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            EndPointProperty =
                  RegisterProperty("EndPoint",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   new PropertyChangedCallback(EndPointPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
