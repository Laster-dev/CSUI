// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// An enumerator for collections of weak references.
    /// </summary>
    internal struct WeakRefEnumerator<T>
    {
        internal WeakRefEnumerator(List<WeakReference> list)
        {
            _list = list;
            _readIndex = 0;
            _writeIndex = 0;
            _current = default(T);

#if DEBUG
            _valid = false;
#endif
        }

        internal T Current
        {
            get
            {
#if DEBUG
                Debug.Assert(_valid);
#endif
                return _current;
            }
        }

        internal int CurrentIndex
        {
            get
            {
#if DEBUG
                Debug.Assert(_valid);
#endif
                return _writeIndex - 1;
            }
        }

        internal void Dispose()
        {
            if (_readIndex != _writeIndex)
            {
                _list.RemoveRange(_writeIndex, _readIndex - _writeIndex);
                _readIndex = _writeIndex = _list.Count;
            }

            _current = default(T);

#if DEBUG
            _valid = false;
#endif
        }

        internal bool MoveNext()
        {
            while (_readIndex < _list.Count)
            {
                WeakReference currentRef = _list[_readIndex];
                _current = (T)currentRef.Target;
                if ((object)_current != null)
                {
                    if (_writeIndex != _readIndex)
                    {
                        _list[_writeIndex] = currentRef;
                    }

                    _readIndex++;
                    _writeIndex++;

#if DEBUG
                    _valid = true;
#endif

                    return true;
                }
                else
                {
                    _readIndex++;
                }
            }

            Dispose();
            return false;
        }

        private List<WeakReference> _list;
        private T _current;
        private int _readIndex;
        private int _writeIndex;

#if DEBUG
        private bool _valid;
#endif
    }
}
