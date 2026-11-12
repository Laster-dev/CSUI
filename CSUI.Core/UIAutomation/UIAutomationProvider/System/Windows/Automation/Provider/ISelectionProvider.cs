#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Selection pattern provider interface


namespace System.Windows.Automation.Provider
{

    /// <summary>
    /// public interface representing containers that manage selection.
    /// </summary>
    /// <remarks>
    /// Client code uses this public interface; server implementers implent the
    /// ISelectionProvider public interface instead.
    /// </remarks>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ISelectionProvider
#else
    public interface ISelectionProvider
#endif
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Get the currently selected elements
        /// </summary>
        /// <returns>An AutomationElement array containing the currently selected elements</returns>
        IRawElementProviderSimple [] GetSelection();

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Indicates whether the control allows more than one element to be selected
        /// </summary>
        /// <returns>Boolean indicating whether the control allows more than one element to be selected</returns>
        /// <remarks>If this is false, then the control is a single-select ccntrol</remarks>
        bool CanSelectMultiple
        {
            get;
        }

        /// <summary>
        /// Indicates whether the control requires at least one element to be selected
        /// </summary>
        /// <returns>Boolean indicating whether the control requires at least one element to be selected</returns>
        /// <remarks>If this is false, then the control allows all elements to be unselected</remarks>
        bool IsSelectionRequired 
        {
            get;
        }

        #endregion Public Properties
    }
}
