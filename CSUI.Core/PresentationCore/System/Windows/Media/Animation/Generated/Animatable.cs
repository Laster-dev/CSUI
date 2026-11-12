// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    public abstract partial class Animatable : IAnimatable
    {
        public void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock)
        {
            ApplyAnimationClock(dp, clock, HandoffBehavior.SnapshotAndReplace);
        }

        public void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock, HandoffBehavior handoffBehavior)
        {
            ArgumentNullException.ThrowIfNull(dp);

            if (!AnimationStorage.IsPropertyAnimatable(this, dp))
            {
                throw new ArgumentException(SR.Format(SR.Animation_DependencyPropertyIsNotAnimatable, dp.Name, GetType()), nameof(dp));
            }

            if (clock != null && !AnimationStorage.IsAnimationValid(dp, clock.Timeline))
            {
                throw new ArgumentException(SR.Format(SR.Animation_AnimationTimelineTypeMismatch, clock.Timeline.GetType(), dp.Name, dp.PropertyType), nameof(clock));
            }

            if (!HandoffBehaviorEnum.IsDefined(handoffBehavior))
            {
                throw new ArgumentException(SR.Animation_UnrecognizedHandoffBehavior);
            }

            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Format(SR.IAnimatable_CantAnimateSealedDO, dp, GetType()));
            }

            AnimationStorage.ApplyAnimationClock(this, dp, clock, handoffBehavior);
        }

        public void BeginAnimation(DependencyProperty dp, AnimationTimeline animation)
        {
            BeginAnimation(dp, animation, HandoffBehavior.SnapshotAndReplace);
        }

        public void BeginAnimation(DependencyProperty dp, AnimationTimeline animation, HandoffBehavior handoffBehavior)
        {
            ArgumentNullException.ThrowIfNull(dp);

            if (!AnimationStorage.IsPropertyAnimatable(this, dp))
            {
                throw new ArgumentException(SR.Format(SR.Animation_DependencyPropertyIsNotAnimatable, dp.Name, GetType()), nameof(dp));
            }

            if (animation != null && !AnimationStorage.IsAnimationValid(dp, animation))
            {
                throw new ArgumentException(SR.Format(SR.Animation_AnimationTimelineTypeMismatch, animation.GetType(), dp.Name, dp.PropertyType), nameof(animation));
            }

            if (!HandoffBehaviorEnum.IsDefined(handoffBehavior))
            {
                throw new ArgumentException(SR.Animation_UnrecognizedHandoffBehavior);
            }

            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Format(SR.IAnimatable_CantAnimateSealedDO, dp, GetType()));
            }

            AnimationStorage.BeginAnimation(this, dp, animation, handoffBehavior);
        }

        public bool HasAnimatedProperties
        {
            get
            {
                VerifyAccess();
                return IAnimatable_HasAnimatedProperties;
            }
        }

        public object GetAnimationBaseValue(DependencyProperty dp)
        {
            ArgumentNullException.ThrowIfNull(dp);

            return GetValueEntry(
                LookupEntry(dp.GlobalIndex),
                dp,
                null,
                RequestFlags.AnimationBaseValue).Value;
        }

        internal sealed override void EvaluateAnimatedValueCore(
            DependencyProperty dp,
            PropertyMetadata metadata,
            ref EffectiveValueEntry entry)
        {
            if (IAnimatable_HasAnimatedProperties)
            {
                AnimationStorage storage = AnimationStorage.GetStorage(this, dp);
                storage?.EvaluateAnimatedValue(metadata, ref entry);
            }
        }
    }
}
