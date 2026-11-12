// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows.Threading;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// AnimationClockResource class.
    /// AnimationClockResource classes refer to an AnimationClock and a base value
    /// for animated RenderData records without requiring a DUCE resource handle.
    /// </summary>
    internal abstract class AnimationClockResource
    {
        protected AnimationClockResource(AnimationClock animationClock)
        {
            _animationClock = animationClock;
            if (_animationClock != null)
            {
                _animationClock.CurrentTimeInvalidated += new EventHandler(OnChanged);
            }
        }

        public AnimationClock AnimationClock => _animationClock;

        protected void OnChanged(object sender, EventArgs args)
        {
            if (sender is DispatcherObject dispatcherObject && dispatcherObject.Dispatcher != null)
            {
                MediaContext mediaContext = MediaContext.From(dispatcherObject.Dispatcher);
                mediaContext?.PostRender();
            }
        }

        internal virtual void PropagateChangedHandlersCore(EventHandler handler, bool adding)
        {
            if (_animationClock != null)
            {
                if (adding)
                {
                    _animationClock.CurrentTimeInvalidated += handler;
                }
                else
                {
                    _animationClock.CurrentTimeInvalidated -= handler;
                }
            }
        }

        protected AnimationClock _animationClock;
    }
}
