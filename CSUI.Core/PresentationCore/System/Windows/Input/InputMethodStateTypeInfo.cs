// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Input
{
    /// <summary>
    /// This enum identifies the type of input method event.
    /// </summary>
    internal enum InputMethodStateType
    {
        Invalid,
        ImeState,
        MicrophoneState,
        HandwritingState,
        SpeechMode,
        ImeConversionModeValues,
        ImeSentenceModeValues,
    }
}
