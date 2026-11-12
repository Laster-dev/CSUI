#nullable disable
using SR = MS.Internal.PresentationCore.SR;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Manage Input Methods (EA-IME, TextServicesFramework).

using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Interop;
using MS.Win32;
using MS.Internal;

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  InputMethodState enum
    //
    //------------------------------------------------------

    /// <summary>
    /// State of Ime
    /// </summary>
    public enum InputMethodState
    {
        /// <summary>
        /// InputMethod state is on.
        /// </summary>
        Off = 0,

        /// <summary>
        /// InputMethod state is on.
        /// </summary>
        On  = 1,

        /// <summary>
        /// InputMethod state is not set. It does not care.
        /// </summary>
        DoNotCare  = 2,
    }

    //------------------------------------------------------
    //
    //  SpeechMode enum
    //
    //------------------------------------------------------

    /// <summary>
    /// Mode of speech
    /// </summary>
    public enum SpeechMode
    {
        /// <summary>
        /// Speech is in dictation mode.
        /// </summary>
        Dictation,

        /// <summary>
        /// Speech is in command mode.
        /// </summary>
        Command,

        /// <summary>
        /// Speech mode is indeterminate.
        /// </summary>
        Indeterminate,
}

    //------------------------------------------------------
    //
    //  ImeConversionModeValues enum
    //
    //------------------------------------------------------

    /// <summary>
    /// ImeConversionModeValues
    /// </summary>
    [Flags]
    public enum ImeConversionModeValues
    {
        /// <summary>
        /// Native Mode (Hiragana, Hangul, Chinese)
        /// </summary>
        Native            = 0x00000001,
        /// <summary>
        /// Japanese Katakana Mode
        /// </summary>
        Katakana          = 0x00000002,
        /// <summary>
        /// Full Shape mode
        /// </summary>
        FullShape         = 0x00000004,
        /// <summary>
        /// Roman Input Mode
        /// </summary>
        Roman             = 0x00000008,
        /// <summary>
        /// Roman Input Mode
        /// </summary>
        CharCode          = 0x00000010,
        /// <summary>
        /// No conversion
        /// </summary>
        NoConversion      = 0x00000020,
        /// <summary>
        /// EUDC symbol(bopomofo) Mode
        /// </summary>
        Eudc              = 0x00000040,
        /// <summary>
        /// Symbol Input Mode
        /// </summary>
        Symbol            = 0x00000080, 
        /// <summary>
        /// Fixed Input Mode
        /// </summary>
        Fixed             = 0x00000100,
        /// <summary>
        /// Alphanumeric mode (Alphanumeric mode was 0x0 in Win32 IMM/Cicero).
        /// </summary>
        Alphanumeric      = 0x00000200,

        /// <summary>
        /// Mode is not set. It does not care.
        /// </summary>
        DoNotCare         = unchecked((int)0x80000000),
    }

    //------------------------------------------------------
    //
    //  ImeSentenceModeValues enum
    //
    //------------------------------------------------------

    /// <summary>
    /// ImeSentenceModeValues
    /// </summary>
    [Flags]
    public enum ImeSentenceModeValues
    {
        /// <summary>
        /// Non Sentence conversion
        /// </summary>
        None               = 0x00000000,   
        /// <summary>
        /// PluralClause conversion
        /// </summary>
        PluralClause       = 0x00000001,      
        /// <summary>
        /// Single Kanji/Hanja conversion
        /// </summary>
        SingleConversion   = 0x00000002,   
        /// <summary>
        /// automatic conversion mode
        /// </summary>
        Automatic          = 0x00000004,    
        /// <summary>
        /// phrase prediction mode
        /// </summary>
        PhrasePrediction   = 0x00000008,   
        /// <summary>
        /// conversation style conversion mode
        /// </summary>
        Conversation       = 0x00000010, 

        /// <summary>
        /// Mode is not set. It does not care.
        /// </summary>
        DoNotCare          = unchecked((int)0x80000000),
    }
 
   
    //------------------------------------------------------
    //
    //  InputMethod class
    //
    //------------------------------------------------------

    /// <summary>
    /// The InputMethod class is a place holder for Cicero API, which are
    /// communicating or accessing TIP's properties.
    /// </summary>
    public class InputMethod : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal InputMethod()
        {
        }

        //------------------------------------------------------
        //
        //  Static Initialization 
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Static Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     A dependency property that enables alternative text inputs.
        /// </summary>
        public static readonly DependencyProperty IsInputMethodEnabledProperty =
                DependencyProperty.RegisterAttached(
                        "IsInputMethodEnabled",
                        typeof(bool),
                        typeof(InputMethod),
                        new PropertyMetadata(
                                true, 
                                new PropertyChangedCallback(IsInputMethodEnabled_Changed)));

        /// <summary>
        /// Setter for IsInputMethodEnabled DependencyProperty
        /// </summary>
        public static void SetIsInputMethodEnabled(DependencyObject target, bool value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(IsInputMethodEnabledProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodEnabled DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsInputMethodEnabled(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (bool)(target.GetValue(IsInputMethodEnabledProperty));
        }

        /// <summary>
        ///     A dependency property that suspends alternative text inputs.
        ///     If this property is true, the document focus remains in the previous focus element
        ///     and the key events won't be dispatched into Cicero/IMEs.
        /// </summary>
        public static readonly DependencyProperty IsInputMethodSuspendedProperty =
                DependencyProperty.RegisterAttached(
                        "IsInputMethodSuspended",
                        typeof(bool),
                        typeof(InputMethod),
                        new PropertyMetadata(false));

        /// <summary>
        /// Setter for IsInputMethodSuspended DependencyProperty
        /// </summary>
        public static void SetIsInputMethodSuspended(DependencyObject target, bool value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(IsInputMethodSuspendedProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodSuspended DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsInputMethodSuspended(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (bool)(target.GetValue(IsInputMethodSuspendedProperty));
        }


        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME status is changed to
        /// the preferred state (open or close)
        /// </summary>
        public static readonly DependencyProperty PreferredImeStateProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeState",
                        typeof(InputMethodState),
                        typeof(InputMethod),
                        new PropertyMetadata(InputMethodState.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeState DependencyProperty
        /// </summary>
        public static void SetPreferredImeState(DependencyObject target, InputMethodState value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(PreferredImeStateProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeState DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static InputMethodState GetPreferredImeState(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (InputMethodState)(target.GetValue(PreferredImeStateProperty));
        }
         
        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME conversion mode is changed to
        /// the preferred mode
        /// </summary>
        public static readonly DependencyProperty PreferredImeConversionModeProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeConversionMode",
                        typeof(ImeConversionModeValues),
                        typeof(InputMethod),
                        new PropertyMetadata(ImeConversionModeValues.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeConversionMode DependencyProperty
        /// </summary>
        public static void SetPreferredImeConversionMode(DependencyObject target, ImeConversionModeValues value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(PreferredImeConversionModeProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeConversionMode DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static ImeConversionModeValues GetPreferredImeConversionMode(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (ImeConversionModeValues)(target.GetValue(PreferredImeConversionModeProperty));
        }
         
        /// <summary>
        /// This is a property for UIElements such as TextBox. 
        /// When the element gets the focus, the IME sentence mode is changed to
        /// the preferred mode
        /// </summary>
        public static readonly DependencyProperty PreferredImeSentenceModeProperty =
                DependencyProperty.RegisterAttached(
                        "PreferredImeSentenceMode",
                        typeof(ImeSentenceModeValues),
                        typeof(InputMethod),
                        new PropertyMetadata(ImeSentenceModeValues.DoNotCare));

        /// <summary>
        /// Setter for PreferredImeSentenceMode DependencyProperty
        /// </summary>
        public static void SetPreferredImeSentenceMode(DependencyObject target, ImeSentenceModeValues value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(PreferredImeSentenceModeProperty, value);
        }

        /// <summary>
        /// Getter for PreferredImeSentenceMode DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static ImeSentenceModeValues GetPreferredImeSentenceMode(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (ImeSentenceModeValues)(target.GetValue(PreferredImeSentenceModeProperty));
        }

        /// <summary>
        /// InputScope is the specified document context for UIElement.
        /// This is a property for UIElements such as TextBox. 
        /// </summary>
        public static readonly DependencyProperty InputScopeProperty =
                DependencyProperty.RegisterAttached(
                        "InputScope",
                        typeof(InputScope),
                        typeof(InputMethod),
                        new PropertyMetadata((InputScope) null));

        /// <summary>
        /// Setter for InputScope DependencyProperty
        /// </summary>
        public static void SetInputScope(DependencyObject target, InputScope value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(InputScopeProperty, value);
        }

        /// <summary>
        /// Getter for InputScope DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static InputScope GetInputScope(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (InputScope)(target.GetValue(InputScopeProperty));
        }

        /// <summary>
        ///     Return the input language manager associated 
        ///     with the current context.
        /// </summary>
        public static InputMethod Current
        {
            get
            {
                InputMethod inputMethod = null;
            
                // Do not auto-create the dispatcher.
                Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                if(dispatcher != null)
                {
                    inputMethod = dispatcher.InputMethod as InputMethod;
                
                    if (inputMethod == null)
                    {
                        inputMethod = new InputMethod();
                        dispatcher.InputMethod = inputMethod;
                    }
                }
                return inputMethod;
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        ///    Show the configure UI of the current active keyboard text service.
        /// </summary> 
        public void ShowConfigureUI()
        {
            ShowConfigureUI(null);
        }

        /// <summary>
        ///    Show the configure UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="element">
        ///     Specify UIElement which frame window becomes the parent of the configure UI.
        ///     This param can be null.
        /// </param>
        public void ShowConfigureUI(UIElement element)
        {
            _ShowConfigureUI(element, true);
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        public void ShowRegisterWordUI()
        {
            ShowRegisterWordUI("");
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="registeredText">
        ///     Specify default string to be registered. This is usually shown in the 
        ///     text field of the register word UI.
        /// </param>
        public void ShowRegisterWordUI(string registeredText)
        {
            ShowRegisterWordUI(null, registeredText);
        }

        /// <summary>
        ///    Show the register word UI of the current active keyboard text service.
        /// </summary> 
        /// <param name="element">
        ///     Specify UIElement which frame window becomes the parent of the configure UI.
        ///     This param can be null.
        /// </param>
        /// <param name="registeredText">
        ///     Specify default string to be registered. This is usually shown in the 
        ///     text field of the register word UI.
        /// </param>
        public void ShowRegisterWordUI(UIElement element, string registeredText)
        {
            _ShowRegisterWordUI(element, true, registeredText);
        }

        #endregion Public Methods        
 
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Access the current keyboard on/off (open/close) status.
        /// </summary> 
        public InputMethodState ImeState
        {
            get
            {
                if (IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the open status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        bool fOpen = UnsafeNativeMethods.ImmGetOpenStatus(new HandleRef(this, himc));
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));

                        return fOpen ? InputMethodState.On : InputMethodState.Off;
                    }
                }
                return InputMethodState.Off;
            }

            set
            {
                Debug.Assert(value != InputMethodState.DoNotCare);

                //
                // Under IMM32 enabled system, we call IMM32 API to update open status.
                //
                if (_immEnabled)
                {
                    IntPtr hwnd = IntPtr.Zero;
                    hwnd = HwndFromInputElement(Keyboard.FocusedElement);

                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        bool fOpen = UnsafeNativeMethods.ImmGetOpenStatus(new HandleRef(this, himc));

                        // we don't have to call IMM unless the value is changed.
                        if (fOpen != (value == InputMethodState.On))
                        {
                           UnsafeNativeMethods.ImmSetOpenStatus(new HandleRef(this, himc), (value == InputMethodState.On));
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current microphone on/off status.
        /// </summary>
        public InputMethodState MicrophoneState
        {
            get
            {
                return InputMethodState.Off;
            }

            set
            {
                Debug.Assert(value != InputMethodState.DoNotCare);
            }
        }

        /// <summary> 
        /// Access the current handwriting on/off status.
        /// </summary> 
        public InputMethodState HandwritingState
        {
            get
            {
                return InputMethodState.Off;
            }

            set
            {
                Debug.Assert(value != InputMethodState.DoNotCare);
            }
        }


        /// <summary> 
        /// Access the current speech mode
        /// </summary> 
        public SpeechMode SpeechMode
        {
            get
            {
                return SpeechMode.Indeterminate;
            }

            set
            {
                if (value != SpeechMode.Dictation && value != SpeechMode.Command)
                {
                    Debug.Fail("Unknown Speech Mode");
                }
            }
        }

        /// <summary> 
        /// Access the current ime conversion mode
        /// </summary> 
        public ImeConversionModeValues ImeConversionMode
        {
            get
            {
                if (IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the conversion status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));


                        ImeConversionModeValues ret = 0;
                        if ((convmode & (NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_KATAKANA)) == 0)
                            ret |= ImeConversionModeValues.Alphanumeric;
                        if ((convmode & NativeMethods.IME_CMODE_NATIVE) != 0)
                            ret |= ImeConversionModeValues.Native;
                        if ((convmode & NativeMethods.IME_CMODE_KATAKANA) != 0)
                            ret |= ImeConversionModeValues.Katakana;
                        if ((convmode & NativeMethods.IME_CMODE_FULLSHAPE) != 0)
                            ret |= ImeConversionModeValues.FullShape;
                        if ((convmode & NativeMethods.IME_CMODE_ROMAN) != 0)
                            ret |= ImeConversionModeValues.Roman;
                        if ((convmode & NativeMethods.IME_CMODE_CHARCODE) != 0)
                            ret |= ImeConversionModeValues.CharCode;
                        if ((convmode & NativeMethods.IME_CMODE_NOCONVERSION) != 0)
                            ret |= ImeConversionModeValues.NoConversion;
                        if ((convmode & NativeMethods.IME_CMODE_EUDC) != 0)
                            ret |= ImeConversionModeValues.Eudc;
                        if ((convmode & NativeMethods.IME_CMODE_SYMBOL) != 0)
                            ret |= ImeConversionModeValues.Symbol;
                        if ((convmode & NativeMethods.IME_CMODE_FIXED) != 0)
                            ret |= ImeConversionModeValues.Fixed;

                        return ret;
                    }
                }

                return ImeConversionModeValues.Alphanumeric;
            }

            set
            {
                if (!IsValidConversionMode(value))
                {
                    throw new ArgumentException(SR.Format(SR.InputMethod_InvalidConversionMode, value));
                }

                Debug.Assert((value & ImeConversionModeValues.DoNotCare) == 0);

                IntPtr hwnd = IntPtr.Zero;
                if (_immEnabled)
                {
                    hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                }

                //
                // Under IMM32 enabled system, we call IMM32 API to update conversion status.
                //
                if (_immEnabled)
                {
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);

                        int convmodeNew = 0;
                        // IME_CMODE_ALPHANUMERIC is 0.
                        // if ((value & ImeConversionModeValues.Alphanumeric) != 0)
                        //     convmodeNew |= NativeMethods.IME_CMODE_ALPHANUMERIC;
                        if ((value & ImeConversionModeValues.Native) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_NATIVE;
                        if ((value & ImeConversionModeValues.Katakana) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_KATAKANA;
                        if ((value & ImeConversionModeValues.FullShape) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_FULLSHAPE;
                        if ((value & ImeConversionModeValues.Roman) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_ROMAN;
                        if ((value & ImeConversionModeValues.CharCode) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_CHARCODE;
                        if ((value & ImeConversionModeValues.NoConversion) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_NOCONVERSION;
                        if ((value & ImeConversionModeValues.Eudc) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_EUDC;
                        if ((value & ImeConversionModeValues.Symbol) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_SYMBOL;
                        if ((value & ImeConversionModeValues.Fixed) != 0)
                            convmodeNew |= NativeMethods.IME_CMODE_FIXED;

                        // We don't have to call IMM unless the value is changed.
                        if (convmode != convmodeNew)
                        {
                            int conversionModeClearBit = 0;

                            if (convmodeNew == (NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_FULLSHAPE))
                            {
                                // Chinese, Hiragana or Korean so clear Katakana
                                conversionModeClearBit = NativeMethods.IME_CMODE_KATAKANA;
                            }
                            else if (convmodeNew == (NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE))
                            {
                                // Katakana Half
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_FULLSHAPE)
                            {
                                // Alpha Full
                                conversionModeClearBit = NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_ALPHANUMERIC)
                            {
                                // Alpha Half
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE | NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_NATIVE;
                            }
                            else if (convmodeNew == NativeMethods.IME_CMODE_NATIVE)
                            {
                                // Hangul
                                conversionModeClearBit = NativeMethods.IME_CMODE_FULLSHAPE;
                            }

                            // Set the new conversion mode bit and apply the clear bit
                            convmodeNew |= convmode;
                            convmodeNew &= ~conversionModeClearBit;

                            UnsafeNativeMethods.ImmSetConversionStatus(new HandleRef(this, himc), convmodeNew, sentence);
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
            }
        }

        /// <summary> 
        /// Access the current ime sentence mode
        /// </summary> 
        public ImeSentenceModeValues ImeSentenceMode
        {
            get
            {
                if (IsImm32ImeCurrent())
                {
                    //
                    // If the current hkl is the real IMM32-IME, we call IMM32 API to get the sentence status.
                    //
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        ImeSentenceModeValues ret = 0;
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));


                        // TF_SENTENCEMODE_ALPHANUMERIC is 0. 
                        if (sentence == NativeMethods.IME_SMODE_NONE)
                            return ImeSentenceModeValues.None;
    
                        if ((sentence & NativeMethods.IME_SMODE_PLAURALCLAUSE) != 0)
                            ret |= ImeSentenceModeValues.PluralClause;
                        if ((sentence & NativeMethods.IME_SMODE_SINGLECONVERT) != 0)
                            ret |= ImeSentenceModeValues.SingleConversion;
                        if ((sentence & NativeMethods.IME_SMODE_AUTOMATIC) != 0)
                            ret |= ImeSentenceModeValues.Automatic;
                        if ((sentence & NativeMethods.IME_SMODE_PHRASEPREDICT) != 0)
                            ret |= ImeSentenceModeValues.PhrasePrediction;
                        if ((sentence & NativeMethods.IME_SMODE_CONVERSATION) != 0)
                            ret |= ImeSentenceModeValues.Conversation;
    
                        return ret;
                    }
                }
                return ImeSentenceModeValues.None;
            }

            set
            {
                if (!IsValidSentenceMode(value))
                {
                    throw new ArgumentException(SR.Format(SR.InputMethod_InvalidSentenceMode, value));
                }

                Debug.Assert((value & ImeSentenceModeValues.DoNotCare) == 0);

                //
                // Under IMM32 enabled system, we call IMM32 API to update sentence status.
                //
                if (_immEnabled)
                {
                    IntPtr hwnd = HwndFromInputElement(Keyboard.FocusedElement);
                    if (hwnd != IntPtr.Zero)
                    {
                        int convmode = 0;
                        int sentence = 0;
                        IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                        UnsafeNativeMethods.ImmGetConversionStatus(new HandleRef(this, himc), ref convmode, ref sentence);
                        int sentenceNew = 0;

                        if ((value & ImeSentenceModeValues.PluralClause) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_PLAURALCLAUSE;
                        if ((value & ImeSentenceModeValues.SingleConversion) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_SINGLECONVERT;
                        if ((value & ImeSentenceModeValues.Automatic) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_AUTOMATIC;
                        if ((value & ImeSentenceModeValues.PhrasePrediction) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_PHRASEPREDICT;
                        if ((value & ImeSentenceModeValues.Conversation) != 0)
                            sentenceNew |= NativeMethods.IME_SMODE_CONVERSATION;

                        // We don't have to call IMM unless the value is changed.
                        if (sentence != sentenceNew)
                        {
                            UnsafeNativeMethods.ImmSetConversionStatus(new HandleRef(this, himc), convmode, sentenceNew);
                        }

                        UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                    }
                }
}
        }

        /// <summary> 
        ///    This is a property that indicates if the current keybaord text services
        ///    can show the configure UI.
        /// </summary> 
        public bool CanShowConfigurationUI
        {
            get
            {
                return _ShowConfigureUI(null, false);
            }
        }

        /// <summary> 
        ///    This is a property that indicates if the current keybaord text services
        ///    can show the register word UI.
        /// </summary> 
        public bool CanShowRegisterWordUI
        {
            get
            {
                return _ShowRegisterWordUI(null, false, "");
            }
        }

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        ///     An event for input method state changed.
        /// </summary>
        public event InputMethodStateChangedEventHandler StateChanged
        {
            add
            {
                ArgumentNullException.ThrowIfNull(value);

                _StateChanged += value;
            }
            remove
            {
                ArgumentNullException.ThrowIfNull(value);

                _StateChanged -= value;
            }
        }



        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        /// <summary>
        ///     When keyboard device gets focus, this is called.
        ///     We will check the preferred input statues of focus element.
        ///     The preferred input methods should be applied after Cicero TIP gots SetFocus callback.
        /// </summary> 
        internal void GotKeyboardFocus(DependencyObject focus)
        {
            object value;

            if (focus == null)
                return;

            //
            // Check the InputLanguageProperty of the focus element.
            //
            value = focus.GetValue(PreferredImeStateProperty);
            if ((value != null) && ((InputMethodState)value != InputMethodState.DoNotCare))
            {
                ImeState = (InputMethodState)value;
            }

            value = focus.GetValue(PreferredImeConversionModeProperty);
            if ((value != null) && (((ImeConversionModeValues)value & ImeConversionModeValues.DoNotCare) == 0))
            {
                ImeConversionMode = (ImeConversionModeValues)value;
            }

            value = focus.GetValue(PreferredImeSentenceModeProperty);
            if ((value != null) && (((ImeSentenceModeValues)value & ImeSentenceModeValues.DoNotCare) == 0))
            {
                ImeSentenceMode = (ImeSentenceModeValues)value;
            }
        }

        /// <summary>
        /// return true if the current keyboard layout is a real IMM32-IME.
        /// </summary>
        internal static bool IsImm32ImeCurrent()
        {
            if (!_immEnabled)
            {
                return false;
            }

            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            return IsImm32Ime(hkl);
        }

        /// <summary>
        /// return true if the keyboard layout is a real IMM32-IME.
        /// </summary>
        internal static bool IsImm32Ime(IntPtr hkl)
        {
            if (hkl == IntPtr.Zero)
            {
                return false;
            }

            return ((NativeMethods.IntPtrToInt32(hkl) & 0xf0000000) == 0xe0000000);
        }

        /// <summary>
        ///     This is call back for the IsInputMethodEnable property.
        /// </summary> 
        private static void IsInputMethodEnabled_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IInputElement inputElement = (IInputElement)d;
            if (inputElement == Keyboard.FocusedElement)
            {
                InputMethod.Current.EnableOrDisableInputMethod((bool) e.NewValue);
            }
        }

        /// <summary>
        ///     InputMethod enabling/disabling function.
        ///     This takes care of both Cicero and IMM32.
        /// </summary> 
        internal void EnableOrDisableInputMethod(bool bEnabled)
        {
            //
            // Under IMM32 enabled system, we associate default hIMC or null hIMC.
            //
            if (_immEnabled)
            {
                IntPtr hwnd;
                hwnd = HwndFromInputElement(Keyboard.FocusedElement);
 
                if (bEnabled)
                {
                    //
                    // Enabled. Use the default hIMC.
                    //
                    if (DefaultImc != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.ImmAssociateContext(new HandleRef(this, hwnd), new HandleRef(this, _defaultImc));
                    }
                }
                else 
                {
                    //
                    // Disable. Use null hIMC.
                    //
                    UnsafeNativeMethods.ImmAssociateContext(new HandleRef(this, hwnd), new HandleRef(this, IntPtr.Zero));
                }
            }
        }
 
        #endregion Internal methods
        
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        #region Internal Properties

        // Set and get the per Dispatcher cache of InputLanguageManager
        internal InputLanguageManager InputLanguageManager
        {
            get {return _inputlanguagemanager;}
            set {_inputlanguagemanager = value;}
        }

        #endregion Internal Properties
 
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private bool _ShowConfigureUI(UIElement element, bool fShow)
        {
            bool bCanShown = false;
            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            if (IsImm32Ime(hkl))
            {
                bCanShown = true;
                if (fShow)
                {
                    UnsafeNativeMethods.ImmConfigureIME(new HandleRef(this, hkl), new HandleRef(this, HwndFromInputElement(element)), NativeMethods.IME_CONFIG_GENERAL, IntPtr.Zero);
                }
            }
            return bCanShown;
        }

        private bool _ShowRegisterWordUI(UIElement element, bool fShow, string strRegister)
        {
            bool bCanShown = false;
            IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);

            if (IsImm32Ime(hkl))
            {
                bCanShown = true;
                if (fShow)
                {
                    NativeMethods.REGISTERWORD regWord = new NativeMethods.REGISTERWORD
                    {
                        lpReading = null,
                        lpWord = strRegister
                    };
                    UnsafeNativeMethods.ImmConfigureIME(new HandleRef(this, hkl), new HandleRef(this, HwndFromInputElement(element)), NativeMethods.IME_CONFIG_REGISTERWORD, ref regWord);
                }
            }
            return bCanShown;
        }

        /// <summary>
        ///    Get hwnd handle value as IntPtr from UIElement.
        /// </summary> 
        private static IntPtr HwndFromInputElement(IInputElement element)
        {
            IntPtr hwnd = (IntPtr)0;
            // We allow null element.
            if (element != null)
            {
                DependencyObject o = element as DependencyObject;
                if (o != null)
                {
                    DependencyObject containingVisual = InputElement.GetContainingVisual(o);
                    if(containingVisual != null)
                    {
                        IWin32Window win32Window = null;
                        PresentationSource source = PresentationSource.CriticalFromVisual(containingVisual);
                        if (source != null)
                        {
                            win32Window = source as IWin32Window;
                            if (win32Window != null)
                            {
                                hwnd = win32Window.Handle;
                            }
                        }
                    }
                }
            }

            return hwnd;
        }

        // This validates the ImeConversionMode value.
        private bool IsValidConversionMode(ImeConversionModeValues mode)
        {
            int mask = (int)(ImeConversionModeValues.Alphanumeric |
                             ImeConversionModeValues.Native       |
                             ImeConversionModeValues.Katakana     |
                             ImeConversionModeValues.FullShape    |
                             ImeConversionModeValues.Roman        |
                             ImeConversionModeValues.CharCode     |
                             ImeConversionModeValues.NoConversion |
                             ImeConversionModeValues.Eudc         |
                             ImeConversionModeValues.Symbol       |
                             ImeConversionModeValues.Fixed        |
                             ImeConversionModeValues.DoNotCare);

           if (((int)mode & ~mask) != 0)
               return false;

           return true;
        }

        // This validates the ImeSentenceMode value.
        private bool IsValidSentenceMode(ImeSentenceModeValues mode)
        {
            int mask = (int)(ImeSentenceModeValues.None              |
                             ImeSentenceModeValues.PluralClause      |
                             ImeSentenceModeValues.SingleConversion  |
                             ImeSentenceModeValues.Automatic         |
                             ImeSentenceModeValues.PhrasePrediction  |
                             ImeSentenceModeValues.Conversation      |
                             ImeSentenceModeValues.DoNotCare);

           if (((int)mode & ~mask) != 0)
               return false;

           return true;
        }


        //------------------------------------------------------
        //
        //  Private Event
        //
        //------------------------------------------------------
                
#pragma warning disable CS0067
        private event InputMethodStateChangedEventHandler _StateChanged;
#pragma warning restore CS0067

        //------------------------------------------------------
        //
        //  Static Private Properties
        //
        //------------------------------------------------------

        private IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc == 0)
                {
                    // 
                    //  Get the default HIMC from default IME window.
                    // 
                    IntPtr hwnd = UnsafeNativeMethods.ImmGetDefaultIMEWnd(new HandleRef(this, IntPtr.Zero));
                    IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));

                    // Store the default imc to _defaultImc.
                    _defaultImc = himc;

                    UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                }

                return _defaultImc;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        // Per Dispatcher Cache for InputLanguageManager
        private InputLanguageManager _inputlanguagemanager;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled ; 

        // the default imc. The default imc is per thread and we cache it in ThreadStatic.
        [ThreadStatic]
        private static IntPtr _defaultImc;

        #endregion Private Fields
    }


    //------------------------------------------------------
    //
    //  InputMethodStateChangedEventHandler delegate
    //
    //------------------------------------------------------
 
    /// <summary>
    ///     The delegate to use for handlers that receive
    ///     input method state changed event.
    /// </summary>
    public delegate void InputMethodStateChangedEventHandler(Object sender, InputMethodStateChangedEventArgs e);
}

