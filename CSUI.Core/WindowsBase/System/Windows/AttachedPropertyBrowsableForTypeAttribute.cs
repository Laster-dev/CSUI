// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    ///     This class declares that an attached property is browsable only
    ///     for dependency objects that derive from the given type. If more
    ///     than one type is specified, the property is browsable if any type
    ///     matches (logical or). The type may also be an interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AttachedPropertyBrowsableForTypeAttribute : AttachedPropertyBrowsableAttribute
    {
        public AttachedPropertyBrowsableForTypeAttribute(Type targetType)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            _targetType = targetType;
        }

        public Type TargetType
        {
            get
            {
                return _targetType;
            }
        }

        public override object TypeId
        {
            get
            {
                return this;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not AttachedPropertyBrowsableForTypeAttribute other)
                return false;
            return _targetType == other._targetType;
        }

        public override int GetHashCode()
        {
            return _targetType.GetHashCode();
        }

        internal override bool IsBrowsable(DependencyObject d, DependencyProperty dp)
        {
            ArgumentNullException.ThrowIfNull(d);
            ArgumentNullException.ThrowIfNull(dp);

            if (!_dTargetTypeChecked)
            {
                try
                {
                    _dTargetType = DependencyObjectType.FromSystemType(_targetType);
                }
                catch (ArgumentException)
                {
                }

                _dTargetTypeChecked = true;
            }

            if (_dTargetType != null && _dTargetType.IsInstanceOfType(d))
            {
                return true;
            }

            return false;
        }

        internal override bool UnionResults
        {
            get
            {
                return true;
            }
        }

        private Type _targetType;
        private DependencyObjectType _dTargetType;
        private bool _dTargetTypeChecked;
    }
}
