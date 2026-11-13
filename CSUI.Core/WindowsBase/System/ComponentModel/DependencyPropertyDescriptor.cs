using System;
using System.Windows;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a <see cref="PropertyDescriptor"/> wrapper around a WPF <see cref="DependencyProperty"/>
    /// with support for value-changed notifications.
    /// </summary>
    public sealed class DependencyPropertyDescriptor : PropertyDescriptor
    {
        private readonly DependencyProperty _dp;
        private readonly Type _componentType;
        private readonly bool _isAttached;

        private DependencyPropertyDescriptor(DependencyProperty dp, Type componentType, bool isAttached)
            : base(dp.Name, null)
        {
            _dp = dp;
            _componentType = componentType;
            _isAttached = isAttached;
        }

        public static DependencyPropertyDescriptor FromProperty(DependencyProperty dependencyProperty, Type targetType)
        {
            ArgumentNullException.ThrowIfNull(dependencyProperty);
            ArgumentNullException.ThrowIfNull(targetType);

            bool isAttached = !dependencyProperty.OwnerType.IsAssignableFrom(targetType);
            return new DependencyPropertyDescriptor(dependencyProperty, targetType, isAttached);
        }

        public static DependencyPropertyDescriptor? FromName(string name, Type ownerType, Type targetType)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(targetType);

            DependencyProperty? dp = DependencyProperty.FromName(name, ownerType);
            return dp != null ? FromProperty(dp, targetType) : null;
        }

        public DependencyProperty DependencyProperty => _dp;
        public PropertyMetadata Metadata => _dp.GetMetadata(_componentType);
        public override Type ComponentType => _componentType;
        public override Type PropertyType => _dp.PropertyType;
        public override bool IsReadOnly => _dp.ReadOnly;
        public bool IsAttached => _isAttached;

        public override bool CanResetValue(object component) =>
            component is DependencyObject d && d.ReadLocalValue(_dp) != DependencyProperty.UnsetValue;

        public override object? GetValue(object? component) =>
            component is DependencyObject d ? d.GetValue(_dp) : null;

        public override void SetValue(object? component, object? value)
        {
            if (component is DependencyObject d)
            {
                d.SetValue(_dp, value);
            }
        }

        public override void ResetValue(object component)
        {
            if (component is DependencyObject d)
            {
                d.ClearValue(_dp);
            }
        }

        public override bool ShouldSerializeValue(object component) => CanResetValue(component);

        public override void AddValueChanged(object component, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(component);
            ArgumentNullException.ThrowIfNull(handler);

            if (component is DependencyObject d)
            {
                d.AddDpValueChanged(_dp, handler);
            }
        }

        public override void RemoveValueChanged(object component, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(component);
            ArgumentNullException.ThrowIfNull(handler);

            if (component is DependencyObject d)
            {
                d.RemoveDpValueChanged(_dp, handler);
            }
        }

        public override bool Equals(object? obj) =>
            obj is DependencyPropertyDescriptor other &&
            _dp == other._dp &&
            _componentType == other._componentType;

        public override int GetHashCode() => HashCode.Combine(_dp, _componentType);

        public override string ToString() => $"{_componentType.Name}.{_dp.Name}";
    }
}
