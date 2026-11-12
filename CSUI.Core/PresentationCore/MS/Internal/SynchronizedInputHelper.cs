// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace MS.Internal
{
    internal static class SynchronizedInputHelper
    {
        internal static DependencyObject GetUIParentCore(DependencyObject o)
        {
            if (o is UIElement e)
            {
                return e.GetUIParentCore();
            }
            else if (o is ContentElement ce)
            {
                return ce.GetUIParentCore();
            }
            return null;
        }

        internal static bool IsMappedEvent(RoutedEventArgs args)
        {
            RoutedEvent re = args.RoutedEvent;

            return (re == Keyboard.KeyUpEvent ||
                    re == Keyboard.KeyDownEvent ||
                    re == TextCompositionManager.TextInputEvent ||
                    re == Mouse.MouseDownEvent ||
                    re == Mouse.MouseUpEvent);
        }

        internal static SynchronizedInputType GetPairedInputType(SynchronizedInputType inputType)
        {
            SynchronizedInputType pairedInputType = SynchronizedInputType.KeyDown;

            switch (inputType)
            {
                case SynchronizedInputType.KeyDown:
                    pairedInputType = SynchronizedInputType.KeyUp;
                    break;

                case SynchronizedInputType.KeyUp:
                    pairedInputType = SynchronizedInputType.KeyDown;
                    break;

                case SynchronizedInputType.MouseLeftButtonDown:
                    pairedInputType = SynchronizedInputType.MouseLeftButtonUp;
                    break;

                case SynchronizedInputType.MouseLeftButtonUp:
                    pairedInputType = SynchronizedInputType.MouseLeftButtonDown;
                    break;

                case SynchronizedInputType.MouseRightButtonDown:
                    pairedInputType = SynchronizedInputType.MouseRightButtonUp;
                    break;

                case SynchronizedInputType.MouseRightButtonUp:
                    pairedInputType = SynchronizedInputType.MouseRightButtonDown;
                    break;
            }

            return pairedInputType;
        }

        // Check whether InputManager is listening for this input.
        internal static bool IsListening(RoutedEventArgs args)
        {
            return Array.IndexOf(InputManager.SynchronizedInputEvents, args.RoutedEvent) >= 0;
        }

        // Check whether this element is listening for input.
        internal static bool IsListening(DependencyObject o, RoutedEventArgs args)
        {
            return InputManager.ListeningElement == o &&
                   Array.IndexOf(InputManager.SynchronizedInputEvents, args.RoutedEvent) >= 0;
        }

        internal static bool ShouldContinueListening(RoutedEventArgs args)
        {
            return args.RoutedEvent == Keyboard.KeyDownEvent;
        }

        // Add a preopportunity handler for the logical parent incase of templated element.
        internal static void AddParentPreOpportunityHandler(DependencyObject o, EventRoute route, RoutedEventArgs args)
        {
            // If the logical parent is different from visual parent then add handler on behalf of the
            // parent into the route. This is to cover the templated elements, where event could be
            // handled by one of the child visual element but we should consider it as if event is handled by
            // parent element ( logical parent).
            DependencyObject visualParent = null;
            if (o is Visual)
            {
                visualParent = UIElementHelper.GetUIParent(o);
            }
            DependencyObject logicalParent = SynchronizedInputHelper.GetUIParentCore(o);
            if (logicalParent != null && logicalParent != visualParent)
            {
                if (logicalParent is UIElement e)
                {
                    e.AddSynchronizedInputPreOpportunityHandler(route, args);
                }
                else if (logicalParent is ContentElement ce)
                {
                    ce.AddSynchronizedInputPreOpportunityHandler(route, args);
                }
            }
        }

        // If the routed event type matches one the element listening on then add handler to the event route.
        internal static void AddHandlerToRoute(DependencyObject o, EventRoute route, RoutedEventHandler eventHandler, bool handledToo)
        {
            // Add a synchronized input handler to the route.
            route.Add(o, eventHandler, handledToo);
        }

        // If this handler is invoked then it indicates the element had the opportunity to handle event.
        internal static void PreOpportunityHandler(object sender, RoutedEventArgs args)
        {
            if (args is KeyboardEventArgs)
            {
                InputManager.SynchronizedInputState = SynchronizedInputStates.HadOpportunity;
            }
            else if (args is TextCompositionEventArgs)
            {
                InputManager.SynchronizedInputState = SynchronizedInputStates.HadOpportunity;
            }
            else if (args is MouseButtonEventArgs mbArgs)
            {
                switch (mbArgs.ChangedButton)
                {
                    case MouseButton.Left:
                        if (InputManager.SynchronizeInputType == SynchronizedInputType.MouseLeftButtonDown ||
                            InputManager.SynchronizeInputType == SynchronizedInputType.MouseLeftButtonUp)
                        {
                            InputManager.SynchronizedInputState = SynchronizedInputStates.HadOpportunity;
                        }
                        break;
                    case MouseButton.Right:
                        if (InputManager.SynchronizeInputType == SynchronizedInputType.MouseRightButtonDown ||
                            InputManager.SynchronizeInputType == SynchronizedInputType.MouseRightButtonUp)
                        {
                            InputManager.SynchronizedInputState = SynchronizedInputStates.HadOpportunity;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // This handler will be called after all class and instance handlers are called, here we
        // decide whether the event is handled by this element or some other element.
        internal static void PostOpportunityHandler(object sender, RoutedEventArgs args)
        {
            if (args is KeyboardEventArgs)
            {
                InputManager.SynchronizedInputState = SynchronizedInputStates.Handled;
            }
            else if (args is TextCompositionEventArgs)
            {
                InputManager.SynchronizedInputState = SynchronizedInputStates.Handled;
            }
            else if (args is MouseButtonEventArgs mbArgs)
            {
                switch (mbArgs.ChangedButton)
                {
                    case MouseButton.Left:
                        if (InputManager.SynchronizeInputType == SynchronizedInputType.MouseLeftButtonDown ||
                            InputManager.SynchronizeInputType == SynchronizedInputType.MouseLeftButtonUp)
                        {
                            InputManager.SynchronizedInputState = SynchronizedInputStates.Handled;
                        }
                        break;
                    case MouseButton.Right:
                        if (InputManager.SynchronizeInputType == SynchronizedInputType.MouseRightButtonDown ||
                            InputManager.SynchronizeInputType == SynchronizedInputType.MouseRightButtonUp)
                        {
                            InputManager.SynchronizedInputState = SynchronizedInputStates.Handled;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // Map a Synchronized input type received from automation client to routed event
        internal static RoutedEvent[] MapInputTypeToRoutedEvents(SynchronizedInputType inputType)
        {
            RoutedEvent[] e = null;
            switch (inputType)
            {
                case SynchronizedInputType.KeyUp:
                    e = new RoutedEvent[] { Keyboard.KeyUpEvent };
                    break;
                case SynchronizedInputType.KeyDown:
                    e = new RoutedEvent[] { Keyboard.KeyDownEvent, TextCompositionManager.TextInputEvent };
                    break;
                case SynchronizedInputType.MouseLeftButtonDown:
                case SynchronizedInputType.MouseRightButtonDown:
                    e = new RoutedEvent[] { Mouse.MouseDownEvent };
                    break;
                case SynchronizedInputType.MouseLeftButtonUp:
                case SynchronizedInputType.MouseRightButtonUp:
                    e = new RoutedEvent[] { Mouse.MouseUpEvent };
                    break;
                default:
                    Debug.Assert(false);
                    e = null;
                    break;
            }
            return e;
        }

        internal static void RaiseAutomationEvents()
        {
            if (InputManager.ListeningElement is UIElement e)
            {
                //Raise InputDiscarded automation event
                SynchronizedInputHelper.RaiseAutomationEvent(e.GetAutomationPeer());
            }
            else if (InputManager.ListeningElement is ContentElement ce)
            {
                //Raise InputDiscarded automation event
                SynchronizedInputHelper.RaiseAutomationEvent(ce.GetAutomationPeer());
            }
        }

        // Raise synchronized input automation events here.
        internal static void RaiseAutomationEvent(AutomationPeer peer)
        {
            if (peer != null)
            {
                switch (InputManager.SynchronizedInputState)
                {
                    case SynchronizedInputStates.Handled:
                        peer.RaiseAutomationEvent(AutomationEvents.InputReachedTarget);
                        break;
                    case SynchronizedInputStates.Discarded:
                        peer.RaiseAutomationEvent(AutomationEvents.InputDiscarded);
                        break;
                    default:
                        peer.RaiseAutomationEvent(AutomationEvents.InputReachedOtherElement);
                        break;
                }
            }
        }
    }

    internal enum SynchronizedInputStates
    {
        NoOpportunity  = 0x01,
        HadOpportunity = 0x02,
        Handled        = 0x04,
        Discarded      = 0x08
    };
}
