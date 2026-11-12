#nullable enable
using System;
using System.Windows.Input;

namespace System.Windows.Markup
{
    /// <summary>
    /// Marks a partial class as an observable ViewModel/Model.
    /// The CSUI Roslyn Incremental Source Generator automatically implements <see cref="System.ComponentModel.INotifyPropertyChanged"/>,
    /// generates <c>OnPropertyChanged</c> and <c>SetProperty</c> helpers, and registers all public &amp; generated properties
    /// into <c>PropertyAccessorRegistry</c> via an assembly <c>[ModuleInitializer]</c> for zero-reflection NativeAOT binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ObservableObjectAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a backing field (e.g. <c>_userName</c> or <c>m_userName</c>) inside an <see cref="ObservableObjectAttribute"/> partial class
    /// so the CSUI Source Generator emits a public property with change notifications, <c>OnXxxChanging</c>/<c>OnXxxChanged</c> partial hooks,
    /// and automatic <c>PropertyAccessorRegistry</c> registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ObservablePropertyAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies additional dependent property names to raise <c>PropertyChanged</c> for whenever an <see cref="ObservablePropertyAttribute"/> field changes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class AlsoNotifyChangeForAttribute : Attribute
    {
        public string[] PropertyNames { get; }

        public AlsoNotifyChangeForAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName ?? string.Empty };
        }

        public AlsoNotifyChangeForAttribute(string propertyName, params string[] otherPropertyNames)
        {
            if (otherPropertyNames == null || otherPropertyNames.Length == 0)
            {
                PropertyNames = new[] { propertyName ?? string.Empty };
            }
            else
            {
                string[] all = new string[1 + otherPropertyNames.Length];
                all[0] = propertyName ?? string.Empty;
                Array.Copy(otherPropertyNames, 0, all, 1, otherPropertyNames.Length);
                PropertyNames = all;
            }
        }
    }

    /// <summary>
    /// Marks a method inside an <see cref="ObservableObjectAttribute"/> partial class to generate an <see cref="ICommand"/> property
    /// (<c>{MethodName}Command</c>) and automatically register it in <c>PropertyAccessorRegistry</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RelayCommandAttribute : Attribute
    {
        /// <summary>
        /// Optional name of a boolean method or property that determines whether the command can execute.
        /// </summary>
        public string? CanExecute { get; set; }
    }

    /// <summary>
    /// Marks any POCO/Model/ViewModel class or struct so the CSUI Source Generator automatically registers all of its public
    /// instance properties and indexers into <c>PropertyAccessorRegistry</c> inside the assembly's <c>[ModuleInitializer]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class BindableAttribute : Attribute
    {
    }

    /// <summary>
    /// Unambiguous alias for <see cref="BindableAttribute"/> when <c>System.ComponentModel</c> is also imported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class AotBindableAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a custom UI element, control, or converter class with a parameterless constructor so the CSUI Source Generator
    /// automatically registers <c>XamlMarkup.RegisterType&lt;T&gt;()</c> and its CLR properties in the assembly's <c>[ModuleInitializer]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class XamlTypeAttribute : Attribute
    {
    }

    /// <summary>
    /// High-performance, NativeAOT-safe <see cref="ICommand"/> implementation used by <see cref="RelayCommandAttribute"/> and Fluent/MVVM bindings.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute();
            }
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Strongly-typed parameterized <see cref="ICommand"/> implementation used by <see cref="RelayCommandAttribute"/>.
    /// </summary>
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            if (parameter is T typed)
            {
                return _canExecute(typed);
            }
            if (parameter == null && default(T) == null)
            {
                return _canExecute(default);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }
            if (parameter is T typed)
            {
                _execute(typed);
            }
            else
            {
                _execute(default);
            }
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
