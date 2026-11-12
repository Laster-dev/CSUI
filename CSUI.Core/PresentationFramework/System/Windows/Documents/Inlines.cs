// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Documents
{
    public abstract class Inline : TextElement
    {
        static Inline()
        {
            MS.Internal.Controls.ControlFoundationHooks.InlineTextDecorationsProperty = TextDecorationsProperty;
            MS.Internal.Controls.ControlFoundationHooks.InlineBaselineAlignmentProperty = BaselineAlignmentProperty;
            MS.Internal.Controls.ControlFoundationHooks.TryAddInlineHook ??= static (target, inlineObj) =>
            {
                if (target is TextBlock tb)
                {
                    if (inlineObj is Inline inl)
                    {
                        tb.Inlines.Add(inl);
                        return true;
                    }
                    if (inlineObj is UIElement uie)
                    {
                        tb.Inlines.Add(uie);
                        return true;
                    }
                }
                return false;
            };
        }

        protected Inline()
        {
        }

        public static readonly DependencyProperty BaselineAlignmentProperty =
            DependencyProperty.Register(
                nameof(BaselineAlignment),
                typeof(BaselineAlignment),
                typeof(Inline),
                new FrameworkPropertyMetadata(BaselineAlignment.Baseline));

        public BaselineAlignment BaselineAlignment
        {
            get => (BaselineAlignment)GetValue(BaselineAlignmentProperty);
            set => SetValue(BaselineAlignmentProperty, value);
        }

        public static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(
                nameof(TextDecorations),
                typeof(TextDecorationCollection),
                typeof(Inline),
                new FrameworkPropertyMetadata(null));

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        internal virtual void AppendPlainText(System.Text.StringBuilder sb)
        {
        }

        internal virtual void CollectChildElements(List<UIElement> children)
        {
        }
    }

    public class Run : Inline
    {
        public Run()
        {
        }

        public Run(string text)
        {
            Text = text;
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(Run),
                new FrameworkPropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        internal override void AppendPlainText(System.Text.StringBuilder sb)
        {
            string t = Text;
            if (!string.IsNullOrEmpty(t))
            {
                sb.Append(t);
            }
        }
    }

    public class LineBreak : Inline
    {
        internal override void AppendPlainText(System.Text.StringBuilder sb)
        {
            sb.Append('\n');
        }
    }

    [ContentProperty(nameof(Inlines))]
    public class Span : Inline
    {
        private InlineCollection _inlines;

        public Span()
        {
        }

        public Span(Inline childInline)
        {
            if (childInline != null)
            {
                Inlines.Add(childInline);
            }
        }

        public InlineCollection Inlines => _inlines ??= new InlineCollection(this);

        internal override void AppendPlainText(System.Text.StringBuilder sb)
        {
            if (_inlines == null) return;
            foreach (Inline inline in _inlines)
            {
                inline?.AppendPlainText(sb);
            }
        }

        internal override void CollectChildElements(List<UIElement> children)
        {
            if (_inlines == null) return;
            foreach (Inline inline in _inlines)
            {
                inline?.CollectChildElements(children);
            }
        }
    }

    public partial class Bold : Span
    {
        static partial void RegisterDefaultTheme();
        static Bold()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Bold), new FrameworkPropertyMetadata(typeof(Bold)));
            FontWeightProperty.OverrideMetadata(typeof(Bold), new FrameworkPropertyMetadata(FontWeights.Bold));
            RegisterDefaultTheme();
        }
        public Bold() { }
        public Bold(Inline childInline) : base(childInline) { }
    }

    public partial class Italic : Span
    {
        static partial void RegisterDefaultTheme();
        static Italic()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Italic), new FrameworkPropertyMetadata(typeof(Italic)));
            FontStyleProperty.OverrideMetadata(typeof(Italic), new FrameworkPropertyMetadata(FontStyles.Italic));
            RegisterDefaultTheme();
        }
        public Italic() { }
        public Italic(Inline childInline) : base(childInline) { }
    }

    public partial class Underline : Span
    {
        static partial void RegisterDefaultTheme();
        static Underline()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Underline), new FrameworkPropertyMetadata(typeof(Underline)));
            TextDecorationsProperty.OverrideMetadata(typeof(Underline), new FrameworkPropertyMetadata(System.Windows.TextDecorations.Underline));
            RegisterDefaultTheme();
        }
        public Underline() { }
        public Underline(Inline childInline) : base(childInline) { }
    }

    public partial class Hyperlink : Span, ICommandSource
    {
        static partial void RegisterDefaultTheme();

        static Hyperlink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
            ForegroundProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(56, 189, 248))));
            CursorProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(Cursors.Hand));
            RegisterDefaultTheme();
        }

        public Hyperlink()
        {
        }

        public Hyperlink(Inline childInline) : base(childInline)
        {
        }

        public static readonly DependencyProperty NavigateUriProperty =
            DependencyProperty.Register(
                nameof(NavigateUri),
                typeof(Uri),
                typeof(Hyperlink),
                new FrameworkPropertyMetadata(null));

        public Uri NavigateUri
        {
            get => (Uri)GetValue(NavigateUriProperty);
            set => SetValue(NavigateUriProperty, value);
        }

        public static readonly DependencyProperty TargetNameProperty =
            DependencyProperty.Register(
                nameof(TargetName),
                typeof(string),
                typeof(Hyperlink),
                new FrameworkPropertyMetadata(string.Empty));

        public string TargetName
        {
            get => (string)GetValue(TargetNameProperty);
            set => SetValue(TargetNameProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(Hyperlink),
                new FrameworkPropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(Hyperlink),
                new FrameworkPropertyMetadata(null));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(
                nameof(CommandTarget),
                typeof(IInputElement),
                typeof(Hyperlink),
                new FrameworkPropertyMetadata(null));

        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                "Click",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(Hyperlink));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        protected virtual void OnClick()
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            if (Command != null && Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
            }
        }

        public void DoClick() => OnClick();
    }

    [ContentProperty(nameof(Child))]
    public class InlineUIContainer : Inline
    {
        private UIElement _child;

        public InlineUIContainer()
        {
        }

        public InlineUIContainer(UIElement childUIElement)
        {
            Child = childUIElement;
        }

        public UIElement Child
        {
            get => _child;
            set
            {
                if (_child != value)
                {
                    if (_child != null)
                    {
                        RemoveLogicalChild(_child);
                    }
                    _child = value;
                    if (_child != null)
                    {
                        AddLogicalChild(_child);
                    }
                }
            }
        }

        internal override void CollectChildElements(List<UIElement> children)
        {
            if (_child != null)
            {
                children.Add(_child);
            }
        }
    }

    public sealed class InlineCollection : IList<Inline>, IList
    {
        private readonly DependencyObject _owner;
        private readonly List<Inline> _items = new List<Inline>();

        internal InlineCollection(DependencyObject owner)
        {
            _owner = owner;
            if (owner is TextBlock tb)
            {
                tb.ConfigureInlineStorage(
                    static storage => ((InlineCollection)storage).ExtractPlainText(),
                    static (storage, collected) => ((InlineCollection)storage).CollectChildElements(collected));
            }
        }

        internal string ExtractPlainText()
        {
            if (_items.Count == 0)
            {
                return string.Empty;
            }
            var sb = new System.Text.StringBuilder();
            foreach (Inline inline in _items)
            {
                inline?.AppendPlainText(sb);
            }
            return sb.ToString();
        }

        internal void CollectChildElements(List<UIElement> collected)
        {
            foreach (Inline inline in _items)
            {
                inline?.CollectChildElements(collected);
            }
        }

        public Inline FirstInline => _items.Count > 0 ? _items[0] : null;
        public Inline LastInline => _items.Count > 0 ? _items[_items.Count - 1] : null;

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

        public Inline this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                InvalidateOwner();
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (Inline)value;
        }

        public void Add(string text) => Add(new Run(text));

        public void Add(UIElement uiElement) => Add(new InlineUIContainer(uiElement));

        public void Add(Inline item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _items.Add(item);
            InvalidateOwner();
        }

        public void Clear()
        {
            _items.Clear();
            InvalidateOwner();
        }

        public bool Contains(Inline item) => _items.Contains(item);
        public void CopyTo(Inline[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<Inline> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(Inline item) => _items.IndexOf(item);
        public void Insert(int index, Inline item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _items.Insert(index, item);
            InvalidateOwner();
        }
        public bool Remove(Inline item)
        {
            bool removed = _items.Remove(item);
            if (removed) InvalidateOwner();
            return removed;
        }
        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            InvalidateOwner();
        }

        int IList.Add(object value)
        {
            if (value is string s) { Add(s); return _items.Count - 1; }
            if (value is UIElement uie) { Add(uie); return _items.Count - 1; }
            Add((Inline)value);
            return _items.Count - 1;
        }
        bool IList.Contains(object value) => value is Inline inline && Contains(inline);
        int IList.IndexOf(object value) => value is Inline inline ? IndexOf(inline) : -1;
        void IList.Insert(int index, object value) => Insert(index, (Inline)value);
        void IList.Remove(object value) { if (value is Inline inline) Remove(inline); }
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

        private void InvalidateOwner()
        {
            if (_owner is TextBlock tb)
            {
                tb.OnInlinesChanged();
            }
            else if (_owner is FrameworkContentElement fce && fce.Parent is TextBlock parentTb)
            {
                parentTb.OnInlinesChanged();
            }
        }
    }
}
