#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Media.Media3D;

using MS.Internal.PresentationCore;


using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{

    /// <summary>
    /// This class is used as part of a Point3DKeyFrameCollection in
    /// conjunction with a KeyFramePoint3DAnimation to animate a
    /// Point3D property value along a set of key frames.
    ///
    /// This Point3DKeyFrame interpolates the between the Point3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingPoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingPoint3DKeyFrame();
        }

        #endregion

        #region Point3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point3D InterpolateValueCore(Point3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolatePoint3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingPoint3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame interpolates the between the Quaternion Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingQuaternionKeyFrame();
        }

        #endregion

        #region QuaternionKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Quaternion InterpolateValueCore(Quaternion baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateQuaternion(baseValue, Value, keyFrameProgress, UseShortestPath);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingQuaternionKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    ///
    /// This Rotation3DKeyFrame interpolates the between the Rotation3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingRotation3DKeyFrame();
        }

        #endregion

        #region Rotation3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rotation3D InterpolateValueCore(Rotation3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateRotation3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingRotation3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    ///
    /// This Vector3DKeyFrame interpolates the between the Vector3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingVector3DKeyFrame();
        }

        #endregion

        #region Vector3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector3D InterpolateValueCore(Vector3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateVector3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingVector3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }
}
