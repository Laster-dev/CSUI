// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A collection of Clock objects.
    /// </summary>
    public class ClockCollection : ICollection<Clock>
    {
        public int Count
        {
            get
            {
                ClockGroup clockGroup = _owner as ClockGroup;
                if (clockGroup != null)
                {
                    List<Clock> childList = clockGroup.InternalChildren;
                    if (childList != null)
                    {
                        return childList.Count;
                    }
                }
                return 0;
            }
        }

        public bool IsReadOnly => true;

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public void Add(Clock item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(Clock item)
        {
            throw new NotSupportedException();
        }

        public bool Contains(Clock item)
        {
            ArgumentNullException.ThrowIfNull(item);

            foreach (Clock t in this)
            {
                if (t.Equals(item))
                    return true;
            }

            return false;
        }

        public void CopyTo(Clock[] array, int index)
        {
            ClockGroup clockGroup = _owner as ClockGroup;

            if (clockGroup != null)
            {
                List<Clock> list = clockGroup.InternalChildren;

                list?.CopyTo(array, index);
            }
        }

        IEnumerator<Clock> IEnumerable<Clock>.GetEnumerator()
        {
            List<Clock> list = null;
            ClockGroup clockGroup = _owner as ClockGroup;

            if (clockGroup != null)
            {
                list = clockGroup.InternalChildren;
            }

            if (list != null)
            {
                return list.GetEnumerator();
            }
            else
            {
                return new ClockEnumerator(_owner);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ClockEnumerator(_owner);
        }

        public override bool Equals(object obj)
        {
            if (obj is ClockCollection)
            {
                return (this == (ClockCollection)obj);
            }
            else
            {
                return false;
            }
        }

        public static bool Equals(ClockCollection objA, ClockCollection objB)
        {
            return (objA == objB);
        }

        public static bool operator ==(ClockCollection objA, ClockCollection objB)
        {
            if (object.ReferenceEquals(objA, objB))
            {
                return true;
            }
            else if (object.ReferenceEquals(objA, null)
                     || object.ReferenceEquals(objB, null))
            {
                return false;
            }
            else
            {
                return objA._owner == objB._owner;
            }
        }

        public static bool operator !=(ClockCollection objA, ClockCollection objB)
        {
            return !(objA == objB);
        }

        public override int GetHashCode()
        {
            return _owner.GetHashCode();
        }

        public Clock this[int index]
        {
            get
            {
                List<Clock> list = null;
                ClockGroup clockGroup = _owner as ClockGroup;

                if (clockGroup != null)
                {
                    list = clockGroup.InternalChildren;
                }

                if (list == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return list[index];
            }
        }

        internal struct ClockEnumerator : IEnumerator<Clock>
        {
            internal ClockEnumerator(Clock owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
            }

            Clock IEnumerator<Clock>.Current =>
                throw new InvalidOperationException(SR.Timing_EnumeratorOutOfRange);

            object IEnumerator.Current => ((IEnumerator<Clock>)this).Current;

            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                ClockGroup clockGroup = _owner as ClockGroup;

                if (clockGroup != null && clockGroup.InternalChildren != null)
                {
                    throw new InvalidOperationException(SR.Timing_EnumeratorInvalidated);
                }

                return false;
            }

            private Clock _owner;
        }

        internal ClockCollection(Clock owner)
        {
            Debug.Assert(owner != null, "ClockCollection must have a non-null owner.");
            _owner = owner;
        }

        private ClockCollection()
        {
            Debug.Fail("Parameterless constructor is illegal for ClockCollection.");
        }

        private Clock _owner;
    }
}
