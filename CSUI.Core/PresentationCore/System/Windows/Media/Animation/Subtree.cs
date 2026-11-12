// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    [Flags]
    internal enum SubtreeFlag
    {
        Reset = 1,
        ProcessRoot = 2,
        SkipSubtree = 4,
    }

    /// <summary>
    /// An object that enumerates the clocks of a subtree of Clock
    /// objects.
    /// </summary>
    internal struct PrefixSubtreeEnumerator
    {
        internal PrefixSubtreeEnumerator(Clock root, bool processRoot)
        {
            _rootClock = root;
            _currentClock = null;
            _flags = processRoot ? (SubtreeFlag.Reset | SubtreeFlag.ProcessRoot) : SubtreeFlag.Reset;
        }

        internal void SkipSubtree()
        {
            _flags |= SubtreeFlag.SkipSubtree;
        }

        public bool MoveNext()
        {
            if ((_flags & SubtreeFlag.Reset) != 0)
            {
                if ((_flags & SubtreeFlag.ProcessRoot) != 0)
                {
                    _currentClock = _rootClock;
                }
                else
                {
                    if (_rootClock != null)
                    {
                        ClockGroup rootClockGroup = _rootClock as ClockGroup;
                        if (rootClockGroup != null)
                        {
                            _currentClock = rootClockGroup.FirstChild;
                        }
                        else
                        {
                            _currentClock = null;
                        }
                    }
                }

                _flags &= ~SubtreeFlag.Reset;
            }
            else if (_currentClock != null)
            {
                ClockGroup currentClockGroup = _currentClock as ClockGroup;
                Clock nextClock = currentClockGroup?.FirstChild;

                if (((_flags & SubtreeFlag.SkipSubtree) != 0) || (nextClock == null))
                {
                    while ((_currentClock != _rootClock) && ((nextClock = _currentClock.NextSibling) == null))
                    {
                        _currentClock = _currentClock.InternalParent;
                    }

                    if (_currentClock == _rootClock)
                    {
                        nextClock = null;
                    }

                    _flags &= ~SubtreeFlag.SkipSubtree;
                }

                _currentClock = nextClock;
            }

            return _currentClock != null;
        }

        public void Reset()
        {
            _currentClock = null;
            _flags = (_flags & SubtreeFlag.ProcessRoot) | SubtreeFlag.Reset;
        }

        internal Clock Current => _currentClock;

        private Clock _rootClock;
        private Clock _currentClock;
        private SubtreeFlag _flags;
    }

    /// <summary>
    /// An object that enumerates the clocks of a subtree of Clock
    /// objects in postfix order.
    /// </summary>
    internal struct PostfixSubtreeEnumerator
    {
        internal PostfixSubtreeEnumerator(Clock root, bool processRoot)
        {
            _rootClock = root;
            _currentClock = null;
            _flags = processRoot ? (SubtreeFlag.Reset | SubtreeFlag.ProcessRoot) : SubtreeFlag.Reset;
        }

        public bool MoveNext()
        {
            if ((_flags & SubtreeFlag.Reset) != 0)
            {
                _currentClock = _rootClock;
                _flags &= ~SubtreeFlag.Reset;
            }
            else if (_currentClock == _rootClock)
            {
                _currentClock = null;
            }

            if (_currentClock != null)
            {
                Clock nextClock = _currentClock;

                if ((_currentClock != _rootClock) && (nextClock = _currentClock.NextSibling) == null)
                {
                    _currentClock = _currentClock.InternalParent;
                }
                else
                {
                    do
                    {
                        _currentClock = nextClock;
                    }
                    while ((nextClock = _currentClock.FirstChild) != null);
                }

                if ((_currentClock == _rootClock) && ((_flags & SubtreeFlag.ProcessRoot) == 0))
                {
                    _currentClock = null;
                }
            }

            return _currentClock != null;
        }

        internal Clock Current => _currentClock;

        private Clock _rootClock;
        private Clock _currentClock;
        private SubtreeFlag _flags;
    }
}
