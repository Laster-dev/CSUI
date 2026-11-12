// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    internal class AnimationLayer
    {
        private object _snapshotValue = DependencyProperty.UnsetValue;
        private IList<AnimationClock> _animationClocks;
        private AnimationStorage _ownerStorage;
        private EventHandler _removeRequestedHandler;
        private bool _hasStickySnapshotValue;

        internal AnimationLayer(AnimationStorage ownerStorage)
        {
            Debug.Assert(ownerStorage != null);

            _ownerStorage = ownerStorage;
            _removeRequestedHandler = new EventHandler(OnRemoveRequested);
        }

        internal void ApplyAnimationClocks(
            IList<AnimationClock> newAnimationClocks,
            HandoffBehavior handoffBehavior,
            object defaultDestinationValue)
        {
            Debug.Assert(
                newAnimationClocks == null
                || (newAnimationClocks.Count > 0
                    && !newAnimationClocks.Contains(null)));

            if (handoffBehavior == HandoffBehavior.SnapshotAndReplace)
            {
                Debug.Assert(defaultDestinationValue != DependencyProperty.UnsetValue,
                    "We need a valid default destination value when peforming a snapshot and replace.");

                EventHandler handler = new EventHandler(OnCurrentStateInvalidated);

                if (_hasStickySnapshotValue)
                {
                    _animationClocks[0].CurrentStateInvalidated -= handler;

                    DetachAnimationClocks();
                }
                else if (_animationClocks != null)
                {
                    _snapshotValue = GetCurrentValue(defaultDestinationValue);

                    DetachAnimationClocks();
                }
                else
                {
                    _snapshotValue = defaultDestinationValue;
                }

                if (newAnimationClocks != null
                    && newAnimationClocks[0].CurrentState == ClockState.Stopped)
                {
                    _hasStickySnapshotValue = true;
                    newAnimationClocks[0].CurrentStateInvalidated += handler;
                }
                else
                {
                    _hasStickySnapshotValue = false;
                }

                SetAnimationClocks(newAnimationClocks);
            }
            else
            {
                Debug.Assert(handoffBehavior == HandoffBehavior.Compose,
                    "Unhandled handoffBehavior value.");
                Debug.Assert(defaultDestinationValue == DependencyProperty.UnsetValue,
                    "We shouldn't take the time to calculate a default destination value when it isn't needed.");

                if (newAnimationClocks == null)
                {
                    return;
                }
                else if (_animationClocks == null)
                {
                    SetAnimationClocks(newAnimationClocks);
                }
                else
                {
                    AppendAnimationClocks(newAnimationClocks);
                }
            }
        }

        private void DetachAnimationClocks()
        {
            Debug.Assert(_animationClocks != null);

            int count = _animationClocks.Count;

            for (int i = 0; i < count; i++)
            {
                _ownerStorage.DetachAnimationClock(_animationClocks[i], _removeRequestedHandler);
            }

            _animationClocks = null;
        }

        private void SetAnimationClocks(
            IList<AnimationClock> animationClocks)
        {
            Debug.Assert(animationClocks != null);
            Debug.Assert(animationClocks.Count > 0);
            Debug.Assert(!animationClocks.Contains(null));
            Debug.Assert(_animationClocks == null);

            _animationClocks = animationClocks;

            int count = animationClocks.Count;

            for (int i = 0; i < count; i++)
            {
                _ownerStorage.AttachAnimationClock(animationClocks[i], _removeRequestedHandler);
            }
        }

        private void OnCurrentStateInvalidated(object sender, EventArgs args)
        {
            Debug.Assert(_hasStickySnapshotValue,
                "_hasStickySnapshotValue should be set to true if OnCurrentStateInvalidated has been called.");

            _hasStickySnapshotValue = false;

            ((AnimationClock)sender).CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
        }

        private void OnRemoveRequested(object sender, EventArgs args)
        {
            Debug.Assert(_animationClocks != null
                         && _animationClocks.Count > 0,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            AnimationClock animationClock = (AnimationClock)sender;

            int index = _animationClocks.IndexOf(animationClock);

            Debug.Assert(index >= 0,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            if (_hasStickySnapshotValue
                && index == 0)
            {
                _animationClocks[0].CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
                _hasStickySnapshotValue = false;
            }

            _animationClocks.RemoveAt(index);

            _ownerStorage.DetachAnimationClock(animationClock, _removeRequestedHandler);

            AnimationStorage tmpOwnerStorage = _ownerStorage;

            if (_animationClocks.Count == 0)
            {
                _animationClocks = null;
                _snapshotValue = DependencyProperty.UnsetValue;
                _ownerStorage.RemoveLayer(this);
                _ownerStorage = null;
            }

            tmpOwnerStorage.WritePostscript();
        }

        private void AppendAnimationClocks(
            IList<AnimationClock> newAnimationClocks)
        {
            Debug.Assert(newAnimationClocks != null);
            Debug.Assert(newAnimationClocks.Count > 0);
            Debug.Assert(!newAnimationClocks.Contains(null));

            int newClocksCount = newAnimationClocks.Count;
            List<AnimationClock> animationClockList = _animationClocks as List<AnimationClock>;

            if (animationClockList == null)
            {
                int oldClocksCount = (_animationClocks == null) ? 0 : _animationClocks.Count;

                animationClockList = new List<AnimationClock>(oldClocksCount + newClocksCount);

                for (int i = 0; i < oldClocksCount; i++)
                {
                    animationClockList.Add(_animationClocks[i]);
                }

                _animationClocks = animationClockList;
            }

            for (int i = 0; i < newClocksCount; i++)
            {
                AnimationClock clock = newAnimationClocks[i];

                animationClockList.Add(clock);
                _ownerStorage.AttachAnimationClock(clock, _removeRequestedHandler);
            }
        }

        internal object GetCurrentValue(
            object defaultDestinationValue)
        {
            Debug.Assert(defaultDestinationValue != DependencyProperty.UnsetValue);

            if (_hasStickySnapshotValue
                && _animationClocks[0].CurrentState == ClockState.Stopped)
            {
                return _snapshotValue;
            }

            if (_animationClocks == null)
            {
                Debug.Assert(_snapshotValue != DependencyProperty.UnsetValue);

                return _snapshotValue;
            }

            object currentLayerValue = _snapshotValue;
            bool hasUnstoppedClocks = false;

            if (currentLayerValue == DependencyProperty.UnsetValue)
            {
                currentLayerValue = defaultDestinationValue;
            }

            int count = _animationClocks.Count;

            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                AnimationClock clock = _animationClocks[i];

                if (clock.CurrentState != ClockState.Stopped)
                {
                    hasUnstoppedClocks = true;

                    currentLayerValue = clock.GetCurrentValue(
                        currentLayerValue,
                        defaultDestinationValue);
                }
            }

            if (hasUnstoppedClocks)
            {
                return currentLayerValue;
            }
            else
            {
                return defaultDestinationValue;
            }
        }
    }
}
