// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace System.Windows.Controls
{
    public enum SpellingReform
    {
        PreAndPostreform = 0,
        Prereform = 1,
        Postreform = 2
    }

    public enum UndoAction
    {
        None = 0,
        Clear = 1,
        Create = 2,
        Undo = 3,
        Redo = 4,
        Merge = 5
    }

    public class TextChange
    {
        internal TextChange(int offset, int addedLength, int removedLength)
        {
            Offset = offset;
            AddedLength = addedLength;
            RemovedLength = removedLength;
        }

        public int Offset { get; }
        public int AddedLength { get; }
        public int RemovedLength { get; }
    }

    /// <summary>
    /// Managed SpellCheck configuration without legacy COM ISpellChecker/msftedit coupling.
    /// </summary>
    public sealed class SpellCheck
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SpellCheck),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty SpellingReformProperty =
            DependencyProperty.RegisterAttached(
                "SpellingReform",
                typeof(SpellingReform),
                typeof(SpellCheck),
                new FrameworkPropertyMetadata(SpellingReform.Postreform));

        public static readonly DependencyProperty CustomDictionariesProperty =
            DependencyProperty.RegisterAttached(
                "CustomDictionaries",
                typeof(IList),
                typeof(SpellCheck),
                new FrameworkPropertyMetadata(null));

        private readonly TextBoxBase _owner;

        internal SpellCheck(TextBoxBase owner)
        {
            _owner = owner;
        }

        public static bool GetIsEnabled(TextBoxBase textBoxBase)
        {
            ArgumentNullException.ThrowIfNull(textBoxBase);
            return (bool)textBoxBase.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(TextBoxBase textBoxBase, bool value)
        {
            ArgumentNullException.ThrowIfNull(textBoxBase);
            textBoxBase.SetValue(IsEnabledProperty, value);
        }

        public static SpellingReform GetSpellingReform(TextBoxBase textBoxBase)
        {
            ArgumentNullException.ThrowIfNull(textBoxBase);
            return (SpellingReform)textBoxBase.GetValue(SpellingReformProperty);
        }

        public static void SetSpellingReform(TextBoxBase textBoxBase, SpellingReform value)
        {
            ArgumentNullException.ThrowIfNull(textBoxBase);
            textBoxBase.SetValue(SpellingReformProperty, value);
        }

        public static IList GetCustomDictionaries(TextBoxBase textBoxBase)
        {
            ArgumentNullException.ThrowIfNull(textBoxBase);
            if (textBoxBase.GetValue(CustomDictionariesProperty) is not IList list)
            {
                list = new List<Uri>();
                textBoxBase.SetValue(CustomDictionariesProperty, list);
            }
            return list;
        }

        public bool IsEnabled
        {
            get => GetIsEnabled(_owner);
            set => SetIsEnabled(_owner, value);
        }

        public SpellingReform SpellingReform
        {
            get => GetSpellingReform(_owner);
            set => SetSpellingReform(_owner, value);
        }

        public IList CustomDictionaries => GetCustomDictionaries(_owner);
    }
}
