// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Navigation
{
    public enum JournalOwnership
    {
        Automatic = 0,
        OwnsJournal = 1,
        UsesParentJournal = 2
    }

    public enum NavigationUIVisibility
    {
        Automatic = 0,
        Visible = 1,
        Hidden = 2
    }

    public class JournalEntry : DependencyObject
    {
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached(
                "Name",
                typeof(string),
                typeof(JournalEntry),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty KeepAliveProperty =
            DependencyProperty.RegisterAttached(
                "KeepAlive",
                typeof(bool),
                typeof(JournalEntry),
                new FrameworkPropertyMetadata(false));

        private object _content;

        internal JournalEntry(Uri source, string name, object content, CustomContentState customContentState)
        {
            Source = source;
            Name = name ?? string.Empty;
            _content = content;
            CustomContentState = customContentState;
        }

        public static string GetName(DependencyObject dependencyObject)
        {
            ArgumentNullException.ThrowIfNull(dependencyObject);
            return (string)dependencyObject.GetValue(NameProperty);
        }

        public static void SetName(DependencyObject dependencyObject, string name)
        {
            ArgumentNullException.ThrowIfNull(dependencyObject);
            dependencyObject.SetValue(NameProperty, name);
        }

        public static bool GetKeepAlive(DependencyObject dependencyObject)
        {
            ArgumentNullException.ThrowIfNull(dependencyObject);
            return (bool)dependencyObject.GetValue(KeepAliveProperty);
        }

        public static void SetKeepAlive(DependencyObject dependencyObject, bool keepAlive)
        {
            ArgumentNullException.ThrowIfNull(dependencyObject);
            dependencyObject.SetValue(KeepAliveProperty, keepAlive);
        }

        public Uri Source { get; set; }

        public CustomContentState CustomContentState { get; internal set; }

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        internal object Content
        {
            get => _content;
            set => _content = value;
        }
    }

    internal sealed class JournalEntryStack : IEnumerable<JournalEntry>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly List<JournalEntry> _items = new List<JournalEntry>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => _items.Count;

        internal void Push(JournalEntry entry)
        {
            _items.Insert(0, entry);
            RaiseChanged();
        }

        internal JournalEntry Pop()
        {
            if (_items.Count == 0)
            {
                return null;
            }

            JournalEntry entry = _items[0];
            _items.RemoveAt(0);
            RaiseChanged();
            return entry;
        }

        internal void Clear()
        {
            if (_items.Count > 0)
            {
                _items.Clear();
                RaiseChanged();
            }
        }

        private void RaiseChanged()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        public IEnumerator<JournalEntry> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class JournalEntryUnifiedViewConverter : IMultiValueConverter
    {
        public static readonly DependencyProperty JournalEntryPositionProperty =
            DependencyProperty.RegisterAttached(
                "JournalEntryPosition",
                typeof(JournalEntryPosition),
                typeof(JournalEntryUnifiedViewConverter),
                new FrameworkPropertyMetadata(JournalEntryPosition.Current));

        public static JournalEntryPosition GetJournalEntryPosition(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (JournalEntryPosition)element.GetValue(JournalEntryPositionProperty);
        }

        public static void SetJournalEntryPosition(DependencyObject element, JournalEntryPosition position)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(JournalEntryPositionProperty, position);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                return null;
            }

            var unified = new List<object>();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        unified.Add(item);
                    }
                }
            }

            return unified;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => new[] { Binding.DoNothing };
    }

    public interface IProvideCustomContentState
    {
        CustomContentState GetContentState();
    }

    public sealed class JournalEntryListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    public enum JournalEntryPosition
    {
        Back,
        Current,
        Forward
    }
}
