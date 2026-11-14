// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Windows.Markup;
using System.Windows.Media.Animation;
using MS.Internal;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{
    /// <summary>
    ///     A visual state that can be transitioned into.
    /// </summary>
    [ContentProperty("Storyboard")]
    [RuntimeNameProperty("Name")]
    public class VisualState : DependencyObject
    {
        /// <summary>
        ///     The name of the VisualState.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        private static readonly DependencyProperty StoryboardProperty = 
            DependencyProperty.Register(
            "Storyboard", 
            typeof(Storyboard), 
            typeof(VisualState));

        /// <summary>
        ///     Storyboard defining the values of properties in this visual state.
        /// </summary>        
        public Storyboard Storyboard
        {
            get { return (Storyboard)GetValue(StoryboardProperty); }
            set { SetValue(StoryboardProperty, value); }
        }

        private DependencyObject _inheritanceContext;
        private bool _hasMultipleInheritanceContexts;

        internal override DependencyObject InheritanceContext => _inheritanceContext;

        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.AddInheritanceContext(context, this, ref _hasMultipleInheritanceContexts, ref _inheritanceContext);
        }

        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.RemoveInheritanceContext(context, this, ref _hasMultipleInheritanceContexts, ref _inheritanceContext);
        }

        internal override bool HasMultipleInheritanceContexts => _hasMultipleInheritanceContexts;

        internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);
            Storyboard?.OnInheritanceContextChanged(args);
        }
    }
}
