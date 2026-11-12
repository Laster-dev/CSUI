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


using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{

    /// <summary>
    /// This class is used as part of a Point3DKeyFrameCollection in
    /// conjunction with a KeyFramePoint3DAnimation to animate a
    /// Point3D property value along a set of key frames.
    /// </summary>
    public abstract class Point3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Point3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Point3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame(Point3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame(Point3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Point3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Point3D),
                    typeof(Point3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Point3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Point3D Value
        {
            get
            {
                return (Point3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Point3D InterpolateValue(
            Point3D baseValue, 
            double keyFrameProgress)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(keyFrameProgress);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(keyFrameProgress, 1.0);

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Point3D InterpolateValueCore(
            Point3D baseValue,
            double keyFrameProgress);

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    /// </summary>
    public abstract class QuaternionKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new QuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new QuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame(Quaternion value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(QuaternionKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Quaternion),
                    typeof(QuaternionKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Quaternion)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Quaternion Value
        {
            get
            {
                return (Quaternion)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Quaternion InterpolateValue(
            Quaternion baseValue, 
            double keyFrameProgress)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(keyFrameProgress);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(keyFrameProgress, 1.0);

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Quaternion InterpolateValueCore(
            Quaternion baseValue,
            double keyFrameProgress);

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    /// </summary>
    public abstract class Rotation3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Rotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Rotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame(Rotation3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Rotation3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Rotation3D),
                    typeof(Rotation3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Rotation3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Rotation3D Value
        {
            get
            {
                return (Rotation3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Rotation3D InterpolateValue(
            Rotation3D baseValue, 
            double keyFrameProgress)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(keyFrameProgress);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(keyFrameProgress, 1.0);

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Rotation3D InterpolateValueCore(
            Rotation3D baseValue,
            double keyFrameProgress);

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    /// </summary>
    public abstract class Vector3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Vector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Vector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame(Vector3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Vector3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Vector3D),
                    typeof(Vector3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Vector3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Vector3D Value
        {
            get
            {
                return (Vector3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Vector3D InterpolateValue(
            Vector3D baseValue, 
            double keyFrameProgress)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(keyFrameProgress);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(keyFrameProgress, 1.0);

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Vector3D InterpolateValueCore(
            Vector3D baseValue,
            double keyFrameProgress);

        #endregion
    }
}
