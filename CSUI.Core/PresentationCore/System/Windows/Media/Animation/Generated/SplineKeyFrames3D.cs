#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;

using System.Collections;
using System.ComponentModel;
using System.Windows.Media;
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
    /// This Point3DKeyFrame interpolates between the Point3D Value of
    /// the previous key frame and its own Value to produce its output value.
    /// </summary>
    public partial class SplinePoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SplinePoint3DKeyFrame.
        /// </summary>
        public SplinePoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SplinePoint3DKeyFrame.
        /// </summary>
        public SplinePoint3DKeyFrame(Point3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new SplinePoint3DKeyFrame.
        /// </summary>
        public SplinePoint3DKeyFrame(Point3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new SplinePoint3DKeyFrame.
        /// </summary>
        public SplinePoint3DKeyFrame(Point3D value, KeyTime keyTime, KeySpline keySpline)
            : this()
        {
            ArgumentNullException.ThrowIfNull(keySpline);

            Value = value;
            KeyTime = keyTime;
            KeySpline = keySpline;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new  SplinePoint3DKeyFrame();
        }

        #endregion

        #region Point3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point3D InterpolateValueCore(Point3D baseValue, double keyFrameProgress)
        {
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
                double splineProgress = KeySpline.GetSplineProgress(keyFrameProgress);

                return AnimatedTypeHelpers.InterpolatePoint3D(baseValue, Value, splineProgress);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// KeySpline Property
        /// </summary>
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplinePoint3DKeyFrame),
                new PropertyMetadata(new KeySpline()));

        /// <summary>
        /// The KeySpline defines the way that progress will be altered for this
        /// key frame.
        /// </summary>
        public KeySpline KeySpline
        {
            get
            {
                return (KeySpline)GetValue(KeySplineProperty);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                SetValue(KeySplineProperty, value);
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame interpolates between the Quaternion Value of
    /// the previous key frame and its own Value to produce its output value.
    /// </summary>
    public partial class SplineQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SplineQuaternionKeyFrame.
        /// </summary>
        public SplineQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SplineQuaternionKeyFrame.
        /// </summary>
        public SplineQuaternionKeyFrame(Quaternion value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new SplineQuaternionKeyFrame.
        /// </summary>
        public SplineQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new SplineQuaternionKeyFrame.
        /// </summary>
        public SplineQuaternionKeyFrame(Quaternion value, KeyTime keyTime, KeySpline keySpline)
            : this()
        {
            ArgumentNullException.ThrowIfNull(keySpline);

            Value = value;
            KeyTime = keyTime;
            KeySpline = keySpline;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new  SplineQuaternionKeyFrame();
        }

        #endregion

        #region QuaternionKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Quaternion InterpolateValueCore(Quaternion baseValue, double keyFrameProgress)
        {
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
                double splineProgress = KeySpline.GetSplineProgress(keyFrameProgress);

                return AnimatedTypeHelpers.InterpolateQuaternion(baseValue, Value, splineProgress, UseShortestPath);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// KeySpline Property
        /// </summary>
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplineQuaternionKeyFrame),
                new PropertyMetadata(new KeySpline()));

        /// <summary>
        /// The KeySpline defines the way that progress will be altered for this
        /// key frame.
        /// </summary>
        public KeySpline KeySpline
        {
            get
            {
                return (KeySpline)GetValue(KeySplineProperty);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                SetValue(KeySplineProperty, value);
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    ///
    /// This Rotation3DKeyFrame interpolates between the Rotation3D Value of
    /// the previous key frame and its own Value to produce its output value.
    /// </summary>
    public partial class SplineRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SplineRotation3DKeyFrame.
        /// </summary>
        public SplineRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SplineRotation3DKeyFrame.
        /// </summary>
        public SplineRotation3DKeyFrame(Rotation3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new SplineRotation3DKeyFrame.
        /// </summary>
        public SplineRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new SplineRotation3DKeyFrame.
        /// </summary>
        public SplineRotation3DKeyFrame(Rotation3D value, KeyTime keyTime, KeySpline keySpline)
            : this()
        {
            ArgumentNullException.ThrowIfNull(keySpline);

            Value = value;
            KeyTime = keyTime;
            KeySpline = keySpline;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new  SplineRotation3DKeyFrame();
        }

        #endregion

        #region Rotation3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rotation3D InterpolateValueCore(Rotation3D baseValue, double keyFrameProgress)
        {
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
                double splineProgress = KeySpline.GetSplineProgress(keyFrameProgress);

                return AnimatedTypeHelpers.InterpolateRotation3D(baseValue, Value, splineProgress);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// KeySpline Property
        /// </summary>
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplineRotation3DKeyFrame),
                new PropertyMetadata(new KeySpline()));

        /// <summary>
        /// The KeySpline defines the way that progress will be altered for this
        /// key frame.
        /// </summary>
        public KeySpline KeySpline
        {
            get
            {
                return (KeySpline)GetValue(KeySplineProperty);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                SetValue(KeySplineProperty, value);
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    ///
    /// This Vector3DKeyFrame interpolates between the Vector3D Value of
    /// the previous key frame and its own Value to produce its output value.
    /// </summary>
    public partial class SplineVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SplineVector3DKeyFrame.
        /// </summary>
        public SplineVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SplineVector3DKeyFrame.
        /// </summary>
        public SplineVector3DKeyFrame(Vector3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new SplineVector3DKeyFrame.
        /// </summary>
        public SplineVector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new SplineVector3DKeyFrame.
        /// </summary>
        public SplineVector3DKeyFrame(Vector3D value, KeyTime keyTime, KeySpline keySpline)
            : this()
        {
            ArgumentNullException.ThrowIfNull(keySpline);

            Value = value;
            KeyTime = keyTime;
            KeySpline = keySpline;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new  SplineVector3DKeyFrame();
        }

        #endregion

        #region Vector3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector3D InterpolateValueCore(Vector3D baseValue, double keyFrameProgress)
        {
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
                double splineProgress = KeySpline.GetSplineProgress(keyFrameProgress);

                return AnimatedTypeHelpers.InterpolateVector3D(baseValue, Value, splineProgress);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// KeySpline Property
        /// </summary>
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplineVector3DKeyFrame),
                new PropertyMetadata(new KeySpline()));

        /// <summary>
        /// The KeySpline defines the way that progress will be altered for this
        /// key frame.
        /// </summary>
        public KeySpline KeySpline
        {
            get
            {
                return (KeySpline)GetValue(KeySplineProperty);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                SetValue(KeySplineProperty, value);
            }
        }

        #endregion
    }
}
