// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    /// TextDecorations class contains a set of commonly used text decorations such as underline,
    /// strikethrough, baseline and over-line.
    /// </summary>
    public static class TextDecorations
    {
        static TextDecorations()
        {
            TextDecoration td = new TextDecoration
            {
                Location = TextDecorationLocation.Underline
            };
            underline = new TextDecorationCollection();
            underline.Add(td);
            underline.Freeze();

            td = new TextDecoration
            {
                Location = TextDecorationLocation.Strikethrough
            };
            strikethrough = new TextDecorationCollection();
            strikethrough.Add(td);
            strikethrough.Freeze();

            td = new TextDecoration
            {
                Location = TextDecorationLocation.OverLine
            };
            overLine = new TextDecorationCollection();
            overLine.Add(td);
            overLine.Freeze();

            td = new TextDecoration
            {
                Location = TextDecorationLocation.Baseline
            };
            baseline = new TextDecorationCollection();
            baseline.Add(td);
            baseline.Freeze();
        }

        public static TextDecorationCollection Underline     { get { return underline; } }
        public static TextDecorationCollection Strikethrough { get { return strikethrough; } }
        public static TextDecorationCollection OverLine      { get { return overLine; } }
        public static TextDecorationCollection Baseline      { get { return baseline; } }

        private static readonly TextDecorationCollection underline;
        private static readonly TextDecorationCollection strikethrough;
        private static readonly TextDecorationCollection overLine;
        private static readonly TextDecorationCollection baseline;
    }
}
