// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Defines DisplayMemberTemplateSelector class.
//

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal.Controls;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace MS.Internal.Data
{
    // Selects template appropriate for CLR item in order to
    // display string property at DisplayMemberPath on the item.
    internal sealed class DisplayMemberTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayMemberPath">path to the member to display</param>
        public DisplayMemberTemplateSelector(string displayMemberPath, string stringFormat)
        {
            Debug.Assert(!(String.IsNullOrEmpty(displayMemberPath) && String.IsNullOrEmpty(stringFormat)));
            _displayMemberPath = displayMemberPath;
            _stringFormat = stringFormat;
        }

        /// <summary>
        /// Override this method to return an app specific <seealso cref="DataTemplate"/>.
        /// </summary>
        /// <param name="item">The data content</param>
        /// <param name="container">The container in which the content is to be displayed</param>
        /// <returns>a app specific template to apply.</returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (_clrNodeContentTemplate == null &&
                ControlFoundationHooks.TextDisplayProperty != null &&
                ControlFoundationHooks.CreateTextDisplayFactoryHook != null)
            {
                _clrNodeContentTemplate = new DataTemplate();
                FrameworkElementFactory text = ControlFoundationHooks.CreateTextDisplayFactoryHook();
                Binding binding = new Binding
                {
                    Path = new PropertyPath(_displayMemberPath),
                    StringFormat = _stringFormat
                };
                text.SetBinding(ControlFoundationHooks.TextDisplayProperty, binding);
                _clrNodeContentTemplate.VisualTree = text;
                _clrNodeContentTemplate.Seal();
            }
            return _clrNodeContentTemplate;
        }

        private string _displayMemberPath;
        private string _stringFormat;
        private DataTemplate _clrNodeContentTemplate;
    }
}
