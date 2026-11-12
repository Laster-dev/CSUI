#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Grid pattern provider interface


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Exposes basic grid functionality: size and moving to specified cells.
    /// </summary>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IGridProvider
#else
    public interface IGridProvider
#endif
    {
        ///<summary>
        /// Obtain the IRawElementProviderSimple at an absolute position
        /// </summary>
        /// <param name="row">Row of cell to get</param>
        /// <param name="column">Column of cell to get</param>
        IRawElementProviderSimple GetItem(int row, int column); 

        /// <summary>
        /// number of rows in the grid
        /// </summary>
        int RowCount
        {
            get;
        }

        /// <summary>
        /// number of columns in the grid
        /// </summary>
        int ColumnCount
        {
            get;
        }
    }
}
