// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Windows.Media.Animation;
using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// A collection of TextEffect objects.
    /// </summary>
    public sealed partial class TextEffectCollection : Animatable, IList, IList<TextEffect>
    {
        #region Public Methods

        public new TextEffectCollection Clone()
        {
            return (TextEffectCollection)base.Clone();
        }

        public new TextEffectCollection CloneCurrentValue()
        {
            return (TextEffectCollection)base.CloneCurrentValue();
        }

        #endregion Public Methods

        #region IList<T>

        public void Add(TextEffect value)
        {
            AddHelper(value);
        }

        public void Clear()
        {
            WritePreamble();

            for (int i = _collection.Count - 1; i >= 0; i--)
            {
                OnFreezablePropertyChanged(/* oldValue = */ _collection[i], /* newValue = */ null);
            }

            _collection.Clear();

            Debug.Assert(_collection.Count == 0);

            ++_version;
            WritePostscript();
        }

        public bool Contains(TextEffect value)
        {
            ReadPreamble();

            return _collection.Contains(value);
        }

        public int IndexOf(TextEffect value)
        {
            ReadPreamble();

            return _collection.IndexOf(value);
        }

        public void Insert(int index, TextEffect value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Collection_NoNull);
            }

            WritePreamble();

            OnFreezablePropertyChanged(/* oldValue = */ null, /* newValue = */ value);

            _collection.Insert(index, value);

            ++_version;
            WritePostscript();
        }

        public bool Remove(TextEffect value)
        {
            WritePreamble();

            int index = IndexOf(value);

            if (index >= 0)
            {
                TextEffect oldValue = _collection[index];

                OnFreezablePropertyChanged(oldValue, null);

                _collection.RemoveAt(index);

                ++_version;
                WritePostscript();

                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            RemoveAtWithoutFiringPublicEvents(index);
            WritePostscript();
        }

        internal void RemoveAtWithoutFiringPublicEvents(int index)
        {
            WritePreamble();

            TextEffect oldValue = _collection[index];

            OnFreezablePropertyChanged(oldValue, null);

            _collection.RemoveAt(index);

            ++_version;
        }

        public TextEffect this[int index]
        {
            get
            {
                ReadPreamble();

                return _collection[index];
            }
            set
            {
                if (value == null)
                {
                    throw new System.ArgumentException(SR.Collection_NoNull);
                }

                WritePreamble();

                if (!Object.ReferenceEquals(_collection[index], value))
                {
                    TextEffect oldValue = _collection[index];
                    OnFreezablePropertyChanged(oldValue, value);

                    _collection[index] = value;
                }

                ++_version;
                WritePostscript();
            }
        }

        #endregion

        #region ICollection<T>

        public int Count
        {
            get
            {
                ReadPreamble();

                return _collection.Count;
            }
        }

        public void CopyTo(TextEffect[] array, int index)
        {
            ReadPreamble();

            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length - _collection.Count);

            _collection.CopyTo(array, index);
        }

        bool ICollection<TextEffect>.IsReadOnly
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        #endregion

        #region IEnumerable<T>

        public Enumerator GetEnumerator()
        {
            ReadPreamble();

            return new Enumerator(this);
        }

        IEnumerator<TextEffect> IEnumerable<TextEffect>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList

        bool IList.IsReadOnly => ((ICollection<TextEffect>)this).IsReadOnly;

        bool IList.IsFixedSize
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = Cast(value);
        }

        int IList.Add(object value) => AddHelper(Cast(value));
        bool IList.Contains(object value) => Contains(value as TextEffect);
        int IList.IndexOf(object value) => IndexOf(value as TextEffect);
        void IList.Insert(int index, object value) => Insert(index, Cast(value));
        void IList.Remove(object value) => Remove(value as TextEffect);

        #endregion

        #region ICollection

        void ICollection.CopyTo(Array array, int index)
        {
            ReadPreamble();

            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length - _collection.Count);

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Collection_BadRank);
            }

            try
            {
                int count = _collection.Count;
                for (int i = 0; i < count; i++)
                {
                    array.SetValue(_collection[i], index + i);
                }
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(SR.Format(SR.Collection_BadDestArray, this.GetType().Name), e);
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                ReadPreamble();

                return IsFrozen || Dispatcher != null;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                ReadPreamble();
                return this;
            }
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion

        #region Internal Helpers

        internal static TextEffectCollection Empty
        {
            get
            {
                if (s_empty == null)
                {
                    TextEffectCollection collection = new TextEffectCollection();
                    collection.Freeze();
                    s_empty = collection;
                }

                return s_empty;
            }
        }

        internal TextEffect Internal_GetItem(int i) => _collection[i];

        internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);

            for (int i = 0; i < this.Count; i++)
            {
                DependencyObject inheritanceChild = _collection[i];
                if (inheritanceChild != null && inheritanceChild.InheritanceContext == this)
                {
                    inheritanceChild.OnInheritanceContextChanged(args);
                }
            }
        }

        #endregion

        #region Private Helpers

        private TextEffect Cast(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!(value is TextEffect))
            {
                throw new System.ArgumentException(SR.Format(SR.Collection_BadType, this.GetType().Name, value.GetType().Name, "TextEffect"));
            }

            return (TextEffect)value;
        }

        private int AddHelper(TextEffect value)
        {
            int index = AddWithoutFiringPublicEvents(value);
            WritePostscript();
            return index;
        }

        internal int AddWithoutFiringPublicEvents(TextEffect value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Collection_NoNull);
            }
            WritePreamble();
            TextEffect newValue = value;
            OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
            int index = _collection.Add(newValue);

            ++_version;
            return index;
        }

        #endregion Private Helpers

        private static TextEffectCollection s_empty;

        #region Protected Methods

        protected override Freezable CreateInstanceCore() => new TextEffectCollection();

        protected override void CloneCore(Freezable source)
        {
            TextEffectCollection sourceCollection = (TextEffectCollection)source;

            base.CloneCore(source);

            int count = sourceCollection._collection.Count;
            _collection = new FrugalStructList<TextEffect>(count);

            for (int i = 0; i < count; i++)
            {
                TextEffect newValue = (TextEffect)sourceCollection._collection[i].Clone();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);
            }
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            TextEffectCollection sourceCollection = (TextEffectCollection)source;

            base.CloneCurrentValueCore(source);

            int count = sourceCollection._collection.Count;
            _collection = new FrugalStructList<TextEffect>(count);

            for (int i = 0; i < count; i++)
            {
                TextEffect newValue = (TextEffect)sourceCollection._collection[i].CloneCurrentValue();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);
            }
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            TextEffectCollection sourceCollection = (TextEffectCollection)source;

            base.GetAsFrozenCore(source);

            int count = sourceCollection._collection.Count;
            _collection = new FrugalStructList<TextEffect>(count);

            for (int i = 0; i < count; i++)
            {
                TextEffect newValue = (TextEffect)sourceCollection._collection[i].GetAsFrozen();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);
            }
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            TextEffectCollection sourceCollection = (TextEffectCollection)source;

            base.GetCurrentValueAsFrozenCore(source);

            int count = sourceCollection._collection.Count;
            _collection = new FrugalStructList<TextEffect>(count);

            for (int i = 0; i < count; i++)
            {
                TextEffect newValue = (TextEffect)sourceCollection._collection[i].GetCurrentValueAsFrozen();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);
            }
        }

        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = base.FreezeCore(isChecking);

            int count = _collection.Count;
            for (int i = 0; i < count && canFreeze; i++)
            {
                canFreeze &= Freezable.Freeze(_collection[i], isChecking);
            }

            return canFreeze;
        }

        #endregion Protected Methods

        #region Internal Fields

        internal FrugalStructList<TextEffect> _collection;
        internal uint _version = 0;

        #endregion Internal Fields

        #region Enumerator

        public struct Enumerator : IEnumerator, IEnumerator<TextEffect>
        {
            internal Enumerator(TextEffectCollection list)
            {
                Debug.Assert(list != null, "list may not be null.");

                _list = list;
                _version = list._version;
                _index = -1;
                _current = default(TextEffect);
            }

            void IDisposable.Dispose()
            {
            }

            public bool MoveNext()
            {
                _list.ReadPreamble();

                if (_version == _list._version)
                {
                    if (_index > -2 && _index < _list._collection.Count - 1)
                    {
                        _current = _list._collection[++_index];
                        return true;
                    }
                    else
                    {
                        _index = -2;
                        return false;
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Enumerator_CollectionChanged);
                }
            }

            public void Reset()
            {
                _list.ReadPreamble();

                if (_version == _list._version)
                {
                    _index = -1;
                }
                else
                {
                    throw new InvalidOperationException(SR.Enumerator_CollectionChanged);
                }
            }

            object IEnumerator.Current => this.Current;

            public TextEffect Current
            {
                get
                {
                    if (_index > -1)
                    {
                        return _current;
                    }
                    else if (_index == -1)
                    {
                        throw new InvalidOperationException(SR.Enumerator_NotStarted);
                    }
                    else
                    {
                        Debug.Assert(_index == -2, "expected -2, got " + _index + "\n");
                        throw new InvalidOperationException(SR.Enumerator_ReachedEnd);
                    }
                }
            }

            private TextEffect _current;
            private TextEffectCollection _list;
            private uint _version;
            private int _index;
        }

        #endregion

        #region Constructors

        public TextEffectCollection()
        {
            _collection = new FrugalStructList<TextEffect>();
        }

        public TextEffectCollection(int capacity)
        {
            _collection = new FrugalStructList<TextEffect>(capacity);
        }

        public TextEffectCollection(IEnumerable<TextEffect> collection)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(collection);

            bool needsItemValidation = true;
            ICollection<TextEffect> icollectionOfT = collection as ICollection<TextEffect>;

            if (icollectionOfT != null)
            {
                _collection = new FrugalStructList<TextEffect>(icollectionOfT);
            }
            else
            {
                ICollection icollection = collection as ICollection;

                if (icollection != null)
                {
                    _collection = new FrugalStructList<TextEffect>(icollection);
                }
                else
                {
                    _collection = new FrugalStructList<TextEffect>();

                    foreach (TextEffect item in collection)
                    {
                        if (item == null)
                        {
                            throw new System.ArgumentException(SR.Collection_NoNull);
                        }
                        TextEffect newValue = item;
                        OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                        _collection.Add(newValue);
                    }

                    needsItemValidation = false;
                }
            }

            if (needsItemValidation)
            {
                foreach (TextEffect item in collection)
                {
                    if (item == null)
                    {
                        throw new System.ArgumentException(SR.Collection_NoNull);
                    }
                    OnFreezablePropertyChanged(/* oldValue = */ null, item);
                }
            }

            WritePostscript();
        }

        #endregion Constructors
    }
}
