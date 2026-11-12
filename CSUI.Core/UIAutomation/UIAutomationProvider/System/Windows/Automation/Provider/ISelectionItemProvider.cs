#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Selection Item pattern provider interface


namespace System.Windows.Automation.Provider
{

    /// <summary>
    /// Define a Selectable Item (only supported on logical elements that are a 
    /// child of an Element that supports SelectionPattern and is itself selectable).  
    /// This allows for manipulation of Selection from the element itself.
    /// </summary>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ISelectionItemProvider
#else
    public interface ISelectionItemProvider
#endif
    {
        /// <summary>
        /// Sets the current element as the selection
        /// This clears the selection from other elements in the container 
        /// </summary>
        void Select();

        /// <summary>
        /// Adds current element to selection
        /// </summary>
        void AddToSelection();
        
        /// <summary>
        /// Removes current element from selection
        /// </summary>
        void RemoveFromSelection();

        /// <summary>
        /// Check whether an element is selected
        /// </summary>
        /// <returns>returns true if the element is selected</returns>
        bool IsSelected
        {
            get;
        }

        /// <summary>
        /// The logical element that supports the SelectionPattern for this Item
        /// </summary>
        /// <returns>returns a IRawElementProviderSimple</returns>
        IRawElementProviderSimple SelectionContainer
        {
            get;
        }
    }
}
