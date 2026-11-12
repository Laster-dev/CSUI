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
    /// the previous key frame and its own Value linearly to produce its output value.
    /// </summary>
    public partial class LinearPoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new LinearPoint3DKeyFrame.
        /// </summary>
        public LinearPoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new LinearPoint3DKeyFrame.
        /// </summary>
        public LinearPoint3DKeyFrame(Point3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new LinearPoint3DKeyFrame.
        /// </summary>
        public LinearPoint3DKeyFrame(Point3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new LinearPoint3DKeyFrame();
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
                return AnimatedTypeHelpers.InterpolatePoint3D(baseValue, Value, keyFrameProgress);
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
    /// the previous key frame and its own Value linearly to produce its output value.
    /// </summary>
    public partial class LinearQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new LinearQuaternionKeyFrame.
        /// </summary>
        public LinearQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new LinearQuaternionKeyFrame.
        /// </summary>
        public LinearQuaternionKeyFrame(Quaternion value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new LinearQuaternionKeyFrame.
        /// </summary>
        public LinearQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new LinearQuaternionKeyFrame();
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
                return AnimatedTypeHelpers.InterpolateQuaternion(baseValue, Value, keyFrameProgress, UseShortestPath);
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
    /// the previous key frame and its own Value linearly to produce its output value.
    /// </summary>
    public partial class LinearRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new LinearRotation3DKeyFrame.
        /// </summary>
        public LinearRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new LinearRotation3DKeyFrame.
        /// </summary>
        public LinearRotation3DKeyFrame(Rotation3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new LinearRotation3DKeyFrame.
        /// </summary>
        public LinearRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new LinearRotation3DKeyFrame();
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
                return AnimatedTypeHelpers.InterpolateRotation3D(baseValue, Value, keyFrameProgress);
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
    /// the previous key frame and its own Value linearly to produce its output value.
    /// </summary>
    public partial class LinearVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new LinearVector3DKeyFrame.
        /// </summary>
        public LinearVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new LinearVector3DKeyFrame.
        /// </summary>
        public LinearVector3DKeyFrame(Vector3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new LinearVector3DKeyFrame.
        /// </summary>
        public LinearVector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new LinearVector3DKeyFrame();
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
                return AnimatedTypeHelpers.InterpolateVector3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion
    }
}
