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
    ///
    /// This Point3DKeyFrame changes from the Point3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscretePoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame(Point3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame(Point3D value, KeyTime keyTime)
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
            return new DiscretePoint3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Point3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point3D InterpolateValueCore(Point3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame changes from the Quaternion Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame(Quaternion value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
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
            return new DiscreteQuaternionKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region QuaternionKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Quaternion InterpolateValueCore(Quaternion baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    ///
    /// This Rotation3DKeyFrame changes from the Rotation3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame(Rotation3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
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
            return new DiscreteRotation3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Rotation3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rotation3D InterpolateValueCore(Rotation3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    ///
    /// This Vector3DKeyFrame changes from the Vector3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame(Vector3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame(Vector3D value, KeyTime keyTime)
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
            return new DiscreteVector3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Vector3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector3D InterpolateValueCore(Vector3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }
}
