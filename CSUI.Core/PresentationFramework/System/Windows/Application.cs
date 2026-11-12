// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description:
//              The Application object is a global object that the Avalon platform
//              uses to identify, reference, and communicate with an Avalon application.
//
//              The application object also presents a consistent logical view of an application
//              to an Avalon developer.
//              An Avalon developer uses the application object to listen and respond to application-wide
//              events (like startup, shutdown, and navigation events),
//              to define global properties and maintain state across multiple pages of markup.
//

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Markup;

using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.Utility;
using MS.Utility;
using MS.Win32;
using Microsoft.Win32;
using MS.Internal.Telemetry.PresentationFramework;
using System.Diagnostics.CodeAnalysis;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{
    /// <summary>
    /// Delegate for Startup Event
    /// </summary>
    public delegate void StartupEventHandler(Object sender, StartupEventArgs e);

    /// <summary>
    /// Delegate for the Exit event.
    /// </summary>
    public delegate void ExitEventHandler(Object sender, ExitEventArgs e);

    /// <summary>
    /// Delegate for SessionEnding event
    /// </summary>
    public delegate void SessionEndingCancelEventHandler(Object sender, SessionEndingCancelEventArgs e);

    #region Application Class

    /// <summary>
    /// Application base class
    /// </summary>
    public class Application : DispatcherObject, IHaveResources, IQueryAmbient
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// The static constructor calls ApplicationInit
        /// </summary>
        static Application()
        {
            ApplicationInit();
        }

        /// <summary>
        ///     Application constructor
        /// </summary>
        public Application()
        {
#if DEBUG_CLR_MEM
            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                CLRProfilerControl.CLRLogWriteLine("Application_Ctor");
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppCtor);

            lock(_globalLock)
            {
                // set the default statics
                // DO NOT move this from the begining of this constructor
                if (!_appCreatedInThisAppDomain)
                {
                    Debug.Assert(_appInstance == null, "_appInstance must be null here.");
                    _appInstance = this;
                    IsShuttingDown    = false;
                    _appCreatedInThisAppDomain = true;
                }
                else
                {
                    //lock will be released, so no worries about throwing an exception inside the lock
                    throw new InvalidOperationException(SR.MultiSingleton);
                }
            }


            //
            // (Application not shutting down when calling
            // Application.Current.Shutdown())
            //
            // post item to do startup work
            // posting it here so that this is the first item in the queue. Devs
            // could post items before calling run and then those will be serviced
            // before if we don't post this one here.
            //
            // Also, doing startup (firing OnStartup etc.) once our dispatcher
            // is run ensures that we run before any external code is run in the
            // application's Dispatcher.
            Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                (DispatcherOperationCallback) delegate(object unused)
                {
                    // Shutdown may be started before the Dispatcher gets to this callback.
                    // This can happen in browser-hosted applications.
                    if (IsShuttingDown)
                        return null;

                    // Event handler exception continuality: if exception occurs in Startup event handler,
                    // our state would not be corrupted because it is fired by posting the item in the queue.
                    // Please check Event handler exception continuality if the logic changes.
                    StartupEventArgs e = new StartupEventArgs();
                    OnStartup(e);

                    // PerformDefaultAction is used to cancel the default navigation for the case
                    // when the app is being loaded as a result of a history navigation.  In such
                    // a case, we don't want to show the startupUri page, rather we show the last
                    // page of the app that we in use before navigating away from it.
                    if (e.PerformDefaultAction)
                    {
                        DoStartup();
                    }
                    return null;
                },
                null);
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        ///<summary>
        ///     Run is called to start an application.
        ///
        ///     Typically a developer will do some setting of properties/attaching to events after instantiating an application object,
        ///     and then call Run() to start the application.
        ///</summary>
        ///<remarks>
        ///     Once run has been called - an application's OnStartup override and Startup event is called
        ///     immediately afterwards.
        ///</remarks>
        /// <returns>ExitCode of the application</returns>
        public int Run()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppRun);
            return this.Run(null);
        }

        ///<summary>
        ///     Run is called to start an application.
        ///
        ///     Typically a developer will do some setting of properties/attaching to events after instantiating an application object,
        ///     and then call Run() to start the application.
        ///     Any arguments passed in as a string array will be added to the command Line property of the StartupEventArgs property.
        ///     The exit code returned by Run will typically be returned to the OS as a return value.
        ///</summary>
        ///<remarks>
        ///     Once run has been called - an application's OnStartup override and Startup event is called
        ///     immediately afterwards.
        ///</remarks>
        /// <param name="window">Window that will be added to the Windows property and made the MainWindow of the Applcation.
        /// The passed Window must be created on the same thread as the Application object.  Furthermore, this Window is
        /// shown once the Application is run.</param>
        /// <returns>ExitCode of the application</returns>
        public int Run(Window window)
        {
            VerifyAccess();
            return RunInternal(window);
        }

        /// <summary>
        ///     Shutdown is called to programmatically shutdown an application.
        ///
        ///     Once shutdown() is called, the application gets called with the
        ///     OnShutdown method to raise the Shutdown event.
        ///     Requires SecurityPermission for unmanaged code
        /// </summary>
        /// <remarks>
        ///     Requires UIPermission with AllWindows access
        /// </remarks>
        public void Shutdown()
        {
            Shutdown(0);
        }

        /// <summary>
        ///     Shutdown is called to programmatically shutdown an application.
        ///
        ///     Once shutdown() is called, the application gets called with the
        ///     OnShutdown method to raise the Shutdown event.
        ///     The exitCode parameter passed in at Shutdown will be returned as a
        ///     return parameter on the run() method, so it can be passed back to the OS.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <param name="exitCode">returned to the Application.Run() method. Typically this will be returned to the OS</param>
        public void Shutdown(int exitCode)
        {
            CriticalShutdown(exitCode);
        }
        internal void CriticalShutdown(int exitCode)
        {
            VerifyAccess();
            //Already called once??
            if (IsShuttingDown)
            {
                return;
            }

            ControlsTraceLogger.LogUsedControlsDetails();

            SetExitCode(exitCode);
            IsShuttingDown = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(ShutdownCallback), null);
}

        /// <summary>
        ///     Searches for a resource with the passed resourceKey and returns it
        /// </summary>
        /// <remarks>
        ///     If the sources is not found on the App, SystemResources are searched.
        /// </remarks>
        /// <param name="resourceKey">Name of the resource</param>
        /// <returns>The found resource.</returns>
        /// <exception cref="ResourceReferenceKeyNotFoundException"> if the key is not found.</exception>
        public object FindResource(object resourceKey)
        {
            ResourceDictionary resources = _resources;
            object resource = null;

            if (resources != null)
            {
                resource = resources[resourceKey];
            }

            if (resource == DependencyProperty.UnsetValue  || resource == null)
            {
                // This is the top of the tree, try the system resource collection
                // Safe from multithreading issues, SystemResources uses the SyncRoot lock to access the resource
                resource = SystemResources.FindResourceInternal(resourceKey);
            }

            if (resource == null)
            {
                Helper.ResourceFailureThrow(resourceKey);
            }

            return resource;
        }

        /// <summary>
        ///     Searches for a resource with the passed resourceKey and returns it
        /// </summary>
        /// <remarks>
        ///     If the sources is not found on the App, SystemResources are searched.
        /// </remarks>
        /// <param name="resourceKey">Name of the resource</param>
        /// <returns>The found resource.  Null if not found.</returns>
        public object TryFindResource(object resourceKey)
        {
            ResourceDictionary resources = _resources;
            object resource = null;

            if (resources != null)
            {
                resource = resources[resourceKey];
            }

            if (resource == DependencyProperty.UnsetValue  || resource == null)
            {
                // This is the top of the tree, try the system resource collection
                // Safe from multithreading issues, SystemResources uses the SyncRoot lock to access the resource
                resource = SystemResources.FindResourceInternal(resourceKey);
            }
            return resource;
        }

        //
        // Internal routine only look up in application resources
        //
        internal object FindResourceInternal(object resourceKey)
        {
            // Call Forwarded
            return FindResourceInternal(resourceKey, false /*allowDeferredResourceReference*/, false /*mustReturnDeferredResourceReference*/);
        }

        internal object FindResourceInternal(object resourceKey, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference)
        {
            ResourceDictionary resources = _resources;

            if (resources == null)
            {
                return null;
            }
            else
            {
                bool canCache;
                return resources.FetchResource(resourceKey, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The Current property enables the developer to always get to the application in
        ///     AppDomain in which they are running.
        /// </summary>
        public static Application Current
        {
            get
            {
                // There is no need to take the _globalLock because reading a
                // reference is an atomic operation. Moreover taking a lock
                // also causes risk of re-entrancy because it pumps messages.

                return _appInstance;
            }
        }

        /// <summary>
        ///     The Windows property exposes a WindowCollection object, from which a developer
        ///     can iterate over all the windows that have been opened in the current application.
        /// </summary>
        // DO-NOT USE THIS PROPERY IF YOU MEAN TO MODIFY THE UNDERLYING COLLECTION.  USE
        // WindowsInternal PROPERTY FOR MODIFYING THE UNDERLYING DATASTRUCTURE.
        public WindowCollection Windows
        {
            get
            {
                VerifyAccess();
                return WindowsInternal.Clone();
            }
        }

        /// <summary>
        ///     The MainWindow property indicates the primary window of the application.
        ///     Note that setting the mainWindow property is not possible for browser hosted applications.
        /// </summary>
        /// <remarks>
        ///     By default - MainWindow will be set to the first window opened in the application.
        ///     However the MainWindow may be set programmatically to indicate "this is my main window".
        ///     It is a recommended programming style to refer to MainWindow in code instead of Windows[0].
        ///
        /// </remarks>
        public Window MainWindow
        {
            get
            {
                VerifyAccess();
                return _mainWindow;
            }

            set
            {
                VerifyAccess();

                if (value != _mainWindow)
                {
                    _mainWindow = value;
                }
            }
        }

        /// <summary>
        ///     The ShutdownMode property is called to set the shutdown specific mode of
        ///     the application. Setting this property controls the way in which an application
        ///     will shutdown.
        ///         The three values for the ShutdownMode enum are :
        ///                 OnLastWindowClose
        ///                 OnMainWindowClose
        ///                 OnExplicitShutdown
        ///
        ///         OnLastWindowClose - this mode will shutdown the application when  the
        ///                             last window is closed, or an explicit call is made
        ///                             to Application.Shutdown(). This is the default mode.
        ///
        ///         OnMainWindowClose - this mode will shutdown the application when the main
        ///                             window has been closed, or Application.Shutdown() is
        ///                             called. Note that if the MainWindow property has not
        ///                             been set - this mode is equivalent to OnExplicitOnly.
        ///
        ///         OnExplicitShutdown- this mode will shutdown the application only when an
        ///                             explicit call to OnShutdown() has been made.
        /// </summary>
        public ShutdownMode ShutdownMode
        {
            get
            {
                VerifyAccess();
                return _shutdownMode;
            }

            set
            {
                VerifyAccess();
                if ( !IsValidShutdownMode(value) )
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ShutdownMode));
                }
                if (IsShuttingDown || _appIsShutdown)
                {
                    throw new InvalidOperationException(SR.ShutdownModeWhenAppShutdown);
                }

                _shutdownMode = value;
            }
        }

        /// <summary>
        ///     Current locally defined Resources
        /// </summary>
        [Ambient]
        public ResourceDictionary Resources
        {
            //Don't use  VerifyAccess() here since Resources can be set from any thread.
            //We synchronize access using _globalLock

            get
            {
                ResourceDictionary resources;
                bool needToAddOwner = false;

                lock(_globalLock)
                {
                    if (_resources == null)
                    {
                        // Shouldn't return null for property of type collection.
                        // It enables the Mort scenario: application.Resources.Add();
                        _resources = new ResourceDictionary();
                        needToAddOwner = true;
                    }

                    resources = _resources;
                }

                if (needToAddOwner)
                {
                    resources.AddOwner(this);
                }

                return resources;
            }
            set
            {
                bool invalidateResources = false;
                ResourceDictionary oldValue;

                lock(_globalLock)
                {
                    oldValue = _resources;
                    _resources = value;
                }

                // This app is no longer an owner for the old RD
                oldValue?.RemoveOwner(this);

                if (_reloadFluentDictionary && !_resourcesInitialized)
                {
                    if(value != null && ThemeMode != ThemeMode.None)
                    {
                        value.MergedDictionaries.Insert(0, ThemeManager.GetThemeDictionary(ThemeMode));
                    }
                    _reloadFluentDictionary = false;
                    invalidateResources = true;
                }

                if (value != null)
                {
                    if (!value.ContainsOwner(this))
                    {
                        // This app is an owner for the new RD
                        value.AddOwner(this);
                    }
                }

                if (oldValue != value)
                {
                    invalidateResources = true;
                }

                if (invalidateResources)
                {
                    InvalidateResourceReferences(new ResourcesChangeInfo(oldValue, value));
                }
            }
        }

        ResourceDictionary IHaveResources.Resources
        {
            get { return Resources; }
            set { Resources = value; }
        }

        /// <summary>
        /// Gets or sets the Fluent theme mode of the application.
        /// </summary>
        /// <remarks>
        /// Setting this property controls if Fluent theme is loaded in Light, Dark or System mode. 
        /// It also controls the application of backdrop and darkmode on window.
        /// The four values for the ThemeMode enum are :
        ///     <see cref="ThemeMode.None"/> - No Fluent theme is loaded.
        ///     <see cref="ThemeMode.System"/> - Fluent theme is loaded based on the system theme.
        ///     <see cref="ThemeMode.Light"/> - Fluent theme is loaded in Light mode.
        ///     <see cref="ThemeMode.Dark"/> - Fluent theme is loaded in Dark mode.
        ///
        /// These values are predefined in <see cref="ThemeMode"/> struct
        ///
        /// The default value is <see cref="ThemeMode.None"/>.
        ///     <see cref="ThemeMode"/> and <see cref="Resources"/> are designed to be in sync with each other.
        ///     Syncing is done in order to avoid UI inconsistencies, for example, if the application is in dark mode 
        ///     but the windows are in light mode or vice versa. 
        ///     
        ///     Setting this property loads the Fluent theme dictionaries in the application resources.
        ///     So, if you set this property, it is preferrable to not include Fluent theme dictionaries
        ///     in the application resources manually. If you do, the Fluent theme dictionaries added in the application
        ///     resources will take precedence over the ones added by setting this property.
        ///     
        ///     This property is experimental and may be removed in future versions.
        /// </remarks>
        public ThemeMode ThemeMode
        {
            get
            {
                return _themeMode;
            }
            set
            {
                VerifyAccess();
                if (!ThemeManager.IsValidThemeMode(value))
                {
                    throw new ArgumentException(string.Format("ThemeMode value {0} is invalid. Use None, System, Light or Dark", value));
                }
                
                ThemeMode oldValue = _themeMode;
                _themeMode = value;

                if(!_resourcesInitialized)
                {

                    ThemeManager.OnApplicationThemeChanged(oldValue, value);

                    // If the resources are not initializd, fluent dictionary
                    // included in this operation will be reset.
                    // Hence, we need to reload the fluent dictionary.
                    _reloadFluentDictionary = true;

                    // OnApplicationThemeChanged will trigger InvalidateResourceReferences
                    // which will mark _resourcesInitialized = true, however since 
                    // the value earlier was false, it means that Resources may not have been
                    // parsed from BAML yet. Hence, we need to reset the value to false.
                    _resourcesInitialized = false;

                    return;
                }

                ThemeManager.OnApplicationThemeChanged(oldValue, value);
            }
        }

        bool IQueryAmbient.IsAmbientPropertyAvailable(string propertyName)
        {
            // We want to make sure that StaticResource resolution checks the .Resources
            // Ie.  The Ambient search should look at Resources if it is set.
            // Even if it wasn't set from XAML (eg. the Ctor (or derived Ctor) added stuff)
            return (propertyName == "Resources" && _resources != null);
        }

        // Says if App.Resources has any implicit styles
        internal bool HasImplicitStylesInResources
        {
            get { return _hasImplicitStylesInResources; }
            set { _hasImplicitStylesInResources = value; }
        }

        /// <summary>
        /// The main page
        /// </summary>
        /// <remarks>
        /// This property should only be called from the Application thread
        /// </remarks>
        public Uri StartupUri
        {
            get
            {
                // Don't call VerifyAccess.  System.Printing can call this from another thread.
                return _startupUri;
            }
            set
            {
                VerifyAccess();
                ArgumentNullException.ThrowIfNull(value);

                _startupUri = value;
            }
        }


        /// <summary>
        /// Gets the property Hashtable.
        /// </summary>
        /// <returns>IDictionary interface</returns>
        /// <remarks>
        /// This property is accessible from any thread
        /// </remarks>
        public IDictionary Properties
        {
            get
            {
                // change from before; Before we used to return null
                // but that might throw a nullpointer exception if
                // indexers are being used on the Hashtable object
                lock(_globalLock)
                {
                    if (_htProps == null)
                    {
                        // so we will have 5 entries before we resize
                        _htProps = new HybridDictionary(5);
                    }
                    return _htProps;
                }
            }
        }


        /// <summary>
        /// This property gets and sets the assembly pack://application:,,,/ refers to.
        /// </summary>
        public static Assembly ResourceAssembly
        {
            get
            {
                if (_resourceAssembly == null)
                {
                    lock (_globalLock)
                    {
                        _resourceAssembly = Assembly.GetEntryAssembly();
                    }
                }

                return _resourceAssembly;
            }
            set
            {
                lock (_globalLock)
                {
                    if (_resourceAssembly != value)
                    {
                        if ((_resourceAssembly == null) && (Assembly.GetEntryAssembly() == null))
                        {
                            _resourceAssembly = value;
                            System.Windows.Navigation.BaseUriHelper.ResourceAssembly = value;
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Format(SR.PropertyIsImmutable, "ResourceAssembly", "Application"));
                        }
                    }
                }
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events
        /// <summary>
        ///     The Startup event is fired when an application is starting.
        ///     This event is raised by the OnStartup method.
        /// </summary>
        public event StartupEventHandler Startup
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_STARTUP, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_STARTUP, value); }
        }

        /// <summary>
        /// The Exit event is fired when an application is shutting down.
        /// This event is raised by the OnExit method.
        /// </summary>
        public event ExitEventHandler Exit
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_EXIT, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_EXIT, value); }
        }

        /// <summary>
        /// The Activated event is fired when an applications window has been activated from
        /// the OS ( alt-tab, or changing application from taskbar, or clicking on a winodw).
        /// This event is raised by the OnActivated method.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        /// The Deactivated event is fired when an applications window has been de-activated
        /// from the OS ( alt-tab, or changing application from taskbar, or clicking away
        /// from an applications window). This event is raised by the OnDeactivated method.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// The SessionEnding event is fired when windows is ending, either due to a shutdown,
        /// or loggoff from the start menu ( or calling the ExitWindows function).  The
        /// ReasonSessionEnding enum on the  SessionEndingEventArgs indicates whether the session
        /// is ending in response to a shutdown of the OS, or if the user is logging off.
        /// </summary>
        public event SessionEndingCancelEventHandler SessionEnding
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_SESSIONENDING, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_SESSIONENDING, value); }
        }

        /// <summary>
        /// The DispatcherUnhandledException event is fired when an unhandled exception
        /// is caught at the Dispatcher level (by the dispatcher).
        /// </summary>
        public event DispatcherUnhandledExceptionEventHandler DispatcherUnhandledException
        {
            //Dispatcher.Invoke will call the callback on the dispatcher thread so a VerifyAccess()
            //check is redundant (and wrong) outside the invoke call

            add
            {
                Dispatcher.Invoke(
                    DispatcherPriority.Send,
                    (DispatcherOperationCallback) delegate(object unused)
                    {
                        Dispatcher.UnhandledException += value;
                        return null;
                    },
                    null);
            }
            remove
            {
                Dispatcher.Invoke(
                    DispatcherPriority.Send,
                    (DispatcherOperationCallback) delegate(object unused)
                    {
                        Dispatcher.UnhandledException -= value;
                        return null;
                    },
                    null);
            }
        }

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods
        /// <summary>
        ///     OnStartup is called to raise the Startup event. The developer will typically override this method
        ///     if they want to take action at startup time ( or they may choose to attach an event).
        ///     This method will be called once when the application begins, once that application's Run() method
        ///     has been called.
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual method
        ///     that raises an event, to provide a convenience for developers that subclass the event.
        ///     If you override this method - you need to call Base.OnStartup(...) for the corresponding event
        ///     to be raised.
        /// </remarks>
        /// <param name="e">The event args that will be passed to the Startup event</param>
        protected virtual void OnStartup(StartupEventArgs e)
        {
            VerifyAccess();

            StartupEventHandler handler = (StartupEventHandler)Events[EVENT_STARTUP];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        ///     OnExit is called to raise the Exit event.
        ///     The developer will typically override this method if they want to take
        ///     action when the application exits  ( or they may choose to attach an event).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual method
        ///     that raises an event, to provide a convenience for developers that subclass the event.
        ///     If you override this method - you need to call Base.OnExit(...) for the
        ///     corresponding event to be raised.
        /// </remarks>
        /// <param name="e">The event args that will be passed to the Exit event</param>
        protected virtual void OnExit(ExitEventArgs e)
        {
            VerifyAccess();

            ExitEventHandler handler = (ExitEventHandler)Events[EVENT_EXIT];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        ///     OnActivated is called to raise the Activated event.
        ///     The developer will typically override this method if they want to take action
        ///     when the application gets activated ( or they may choose to attach an event).
        ///     This method will be called when one of the current applications windows gets
        ///     activated on the desktop. ( This corresponds to Users WM_ACTIVATEAPP message).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected
        ///     virtual method that raises an event, to provide a convenience for developers
        ///     that subclass the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnActivated(EventArgs e)
        {
            VerifyAccess();
            if (Activated != null)
            {
                Activated(this, e);
            }
        }

        /// <summary>
        ///     OnDeactivated is called to raise the Deactivated event. The developer will
        ///     typically override this method if they want to take action when the application
        ///     gets deactivated ( or they may choose to attach an event).
        ///     This method will be called when one of the current applications windows gets
        ///     activated on the desktop. ( This corresponds to Users WM_ACTIVATEAPP message,
        ///     with an wparam indicating the app is being deactivated).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that
        ///     subclass the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnDeactivated(EventArgs e)
        {
            VerifyAccess();
            if (Deactivated != null)
            {
                Deactivated(this, e);
            }
        }

        /// <summary>
        ///     OnSessionEnding is called to raise the SessionEnding event. The developer will
        ///     typically override this method if they want to take action when the OS is ending
        ///     a session ( or they may choose to attach an event). This method will be called when
        ///     the user has chosen to either logoff or shutdown. These events are equivalent
        ///     to receiving a WM_QUERYSESSION window event. Windows will send it when user is
        ///     logging out/shutting down. ( See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/sysinfo/base/wm_queryendsession.asp ).
        ///     By default if this event is not cancelled - Avalon will then call Application.Shutdown.
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            VerifyAccess();

            SessionEndingCancelEventHandler handler = (SessionEndingCancelEventHandler)Events[EVENT_SESSIONENDING];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Application Startup.
        /// </summary>
        internal void DoStartup()
        {
            Debug.Assert(CheckAccess(), "This should only be called on the Application thread");

            if (StartupUri != null)
            {
                throw new NotSupportedException("XAML/BAML StartupUri is not supported in NativeAOT mode.");
            }
        }

        /// <summary>
        /// DO NOT USE - internal method
        /// </summary>
        internal virtual void DoShutdown()
        {
            Debug.Assert(CheckAccess(), "DoShutdown can only be called on the Dispatcer thread");
            // We need to know if we have been shut down already.
            // We cannot check the IsShuttingDown variable because it is set true
            // in the function that calls us.

            // We use a while loop like this because closing a window will modify the windows list.
            while(WindowsInternal.Count > 0)
            {
                if (!WindowsInternal[0].IsDisposed)
                {
                    WindowsInternal[0].InternalClose(true, true);
                }
                else
                {
                    WindowsInternal.RemoveAt(0);
                }
            }
            WindowsInternal = null;

            ExitEventArgs e = new ExitEventArgs(_exitCode);

            // Event handler exception continuality: if exception occurs in ShuttingDown event handler,
            // our cleanup action is to finish Shuttingdown.  Since Shuttingdown cannot be cancelled.
            // We don't want user to use throw exception and catch it to cancel Shuttingdown.
            try
            {
                // fire Applicaiton Exit event
                OnExit(e);
            }
            finally
            {
                SetExitCode(e._exitCode);

                // By default statics are shared across appdomains, so need to clear
                lock (_globalLock)
                {
                    _appInstance = null;
                }

                _mainWindow = null;
                _htProps = null;
                NonAppWindowsInternal = null;

                // this will always be null in the browser hosted case since we we don't
                // support Activate, Deactivate, and SessionEnding events in the
                // browser scenario and thus we never create this hwndsource.
                _parkingHwnd?.Dispose();

                _events?.Dispose();

                _appIsShutdown = true; // mark app as shutdown
            }
        }

        /// <summary>
        /// This function is called from the public Run methods to start the application.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal int RunInternal(Window window)
        {
            VerifyAccess();

#if DEBUG_CLR_MEM
            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                CLRProfilerControl.CLRLogWriteLine("Application_Run");
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppRun);

            //
            // (Can't create app and do run/shutdown followed
            // by run/shutdown)
            //
            // Devs could write the following code
            //
            // Application app = new Application();
            // app.Run();
            // app.Run();
            //
            // In this case, we should throw an exception when Run is called for the second time.
            // When app is shutdown, _appIsShutdown is set to true.  If it is true here, then we
            // throw an exception
            if (_appIsShutdown)
            {
                throw new InvalidOperationException(SR.Format(SR.CannotCallRunMultipleTimes, this.GetType().FullName));
            }

            if (window != null)
            {
                if (!window.CheckAccess())
                {
                    throw new ArgumentException(SR.Format(SR.WindowPassedShouldBeOnApplicationThread, window.GetType().FullName, this.GetType().FullName));
                }

                if (!WindowsInternal.HasItem(window))
                {
                    WindowsInternal.Add(window);
                }

                if (MainWindow == null)
                {
                    MainWindow = window;
                }

                if (window.Visibility != Visibility.Visible)
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        (DispatcherOperationCallback) delegate(object obj)
                        {
                            Window win = obj as Window;
                            win.Show();
                            return null;
                        },
                        window);
                }
            }

            EnsureHwndSource();

            //Even if the subclass app cancels the event we still want to create and run the dispatcher
            //so that when the app explicitly calls Shutdown, we have a dispatcher to service the posted
            //Shutdown DispatcherOperationCallback

            // Invoke the Dispatcher synchronously if we are not in the browser
            RunDispatcher(null);

            return _exitCode;
        }

        internal void InvalidateResourceReferences(ResourcesChangeInfo info)
        {
            _resourcesInitialized = true;
            
            // Sync needs to be performed only under the following conditions:
            //  - the resource change event raised is due to a collection change
            //      i.e. it is not a IsIndividualResourceAddOperation
            //  - the event is not raised due to the change in Application.ThemeMode
            //      i.e. SkipAppThemeModeSyncing is set to true
            if (!info.IsIndividualResourceChange
                    && !ThemeManager.SkipAppThemeModeSyncing)
            {
                ThemeManager.SyncApplicationThemeMode();
            }
            
            // Invalidate ResourceReference properties on all the windows.
            // we Clone() the collection b/c if we don't then some other thread can be
            // modifying the collection while we iterate over it
            InvalidateResourceReferenceOnWindowCollection(WindowsInternal.Clone(), info);
            InvalidateResourceReferenceOnWindowCollection(NonAppWindowsInternal.Clone(), info);
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // The public Windows property returns a copy of the underlying
        // WindowCollection.  This property is used internally to enable
        // modyfying the underlying collection.
        internal WindowCollection WindowsInternal
        {
            get
            {
                lock(_globalLock)
                {
                    if(_appWindowList == null)
                    {
                        _appWindowList = new WindowCollection();
                    }
                    return _appWindowList;
                }
            }
            private set
            {
                lock(_globalLock)
                {
                    _appWindowList = value;
                }
            }
        }

        internal WindowCollection NonAppWindowsInternal
        {
            get
            {
                lock(_globalLock)
                {
                    if (_nonAppWindowList == null)
                    {
                        _nonAppWindowList = new WindowCollection();
                    }
                    return _nonAppWindowList;
                }
            }

            private set
            {
                lock(_globalLock)
                {
                    _nonAppWindowList = value;
                }
            }
        }

        internal static bool IsShuttingDown
        {
            get
            {
                // If we are shutting down normally, Application.IsShuttingDown will be true. Be sure to check this first.
                if (_isShuttingDown)
                {
                    return _isShuttingDown;
                }

                return false;
            }
            set
            {
                lock(_globalLock)
                {
                    _isShuttingDown = value;
                }
            }
        }

        // This returns the static variable _isShuttingDown.
        internal static bool IsApplicationObjectShuttingDown
        {
            get
            {
                return _isShuttingDown;
            }
        }

        /// <summary>
        /// Returns the handle of the parking window.
        /// </summary>
        internal IntPtr ParkingHwnd
        {
            get
            {
                if (_parkingHwnd != null)
                {
                    return _parkingHwnd.Handle;
                }
                else
                {
                    return IntPtr.Zero;
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        // <summary>
        // Sets up several things on startup.  If you want to use avalon services without using the
        // Application class you will need to call this method explicitly.  Standard avalon applications
        // will not have to worry about this detail.
        // </summary>
        private static void ApplicationInit()
        {
            _globalLock = new object();
        }

        /// <summary>
        ///     Creates hwndsource so that we can listen to some window msgs.
        /// </summary>
        private void EnsureHwndSource()
        {
            if (_parkingHwnd == null)
            {
                // _appFilterHook needs to be member variable otherwise
                // it is GC'ed and we don't get messages from HwndWrapper
                // (HwndWrapper keeps a WeakReference to the hook)

                _appFilterHook = new HwndWrapperHook(AppFilterMessage);
                HwndWrapperHook[] wrapperHooks = {_appFilterHook};

                _parkingHwnd = new HwndWrapper(
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                "",
                                IntPtr.Zero,
                                wrapperHooks);
            }
        }

        private IntPtr AppFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr retInt = IntPtr.Zero;
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_ACTIVATEAPP:
                    handled = WmActivateApp(NativeMethods.IntPtrToInt32(wParam));
                    break;
                case WindowMessage.WM_QUERYENDSESSION :
                    handled = WmQueryEndSession(lParam, ref retInt);
                    break;
                default:
                    handled = false;
                    break;
            }
            return retInt;
        }

        private bool WmActivateApp(Int32 wParam)
        {
            int temp = wParam;
            bool isActivated = (temp == 0? false : true);

            // Event handler exception continuality: if exception occurs in Activate/Deactivate event handlers, our state would not
            // be corrupted because no internal state are affected by Activate/Deactivate. Please check Event handler exception continuality
            // if a state depending on those events is added.
            if (isActivated)
            {
                OnActivated(EventArgs.Empty);
            }
            else
            {
                OnDeactivated(EventArgs.Empty);
            }
            return false;
        }

        private bool WmQueryEndSession(IntPtr lParam, ref IntPtr refInt)
        {
            int reason = NativeMethods.IntPtrToInt32(lParam);
            bool retVal = false;

            // Event handler exception continuality: if exception occurs in SessionEnding event handlers, our state would not
            // be corrupted because no internal state are affected by SessionEnding. Please check Event handler exception continuality
            // if a state depending on this event is added.
            SessionEndingCancelEventArgs secEventArgs = new SessionEndingCancelEventArgs( (reason & NativeMethods.ENDSESSION_LOGOFF) != 0? ReasonSessionEnding.Logoff : ReasonSessionEnding.Shutdown );
            OnSessionEnding( secEventArgs );

            // shut down the app if not cancelled
            if (!secEventArgs.Cancel)
            {
                Shutdown();
                // return true to the wnd proc to signal that we can terminate properly
                refInt = new IntPtr(1);
                retVal = false;
            }
            else
            {
                refInt = IntPtr.Zero;

                // we have handled the event DefWndProc will not be called for this msg
                retVal = true;
            }

            return retVal;
        }

        private void InvalidateResourceReferenceOnWindowCollection(WindowCollection wc, ResourcesChangeInfo info)
        {
            bool hasImplicitStyles  = info.IsResourceAddOperation && HasImplicitStylesInResources;

            for (int i = 0; i < wc.Count; i++)
            {
                // calling thread is the same as the wc[i] thread so synchronously invalidate
                // resouces, else, post a dispatcher workitem to invalidate resources.
                if (wc[i].CheckAccess())
                {
                    // Set the ShouldLookupImplicitStyles flag on the App's windows
                    // to true if App.Resources has implicit styles.

                    if (hasImplicitStyles)
                        wc[i].ShouldLookupImplicitStyles = true;

                    TreeWalkHelper.InvalidateOnResourcesChange(wc[i], null, info);
                }
                else
                {
                    wc[i].Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        (DispatcherOperationCallback) delegate(object obj)
                        {
                            object[] args = obj as object[];

                            // Set the ShouldLookupImplicitStyles flag on the App's windows
                            // to true if App.Resources has implicit styles.

                            if (hasImplicitStyles)
                                ((FrameworkElement)args[0]).ShouldLookupImplicitStyles = true;

                            TreeWalkHelper.InvalidateOnResourcesChange((FrameworkElement)args[0], null, (ResourcesChangeInfo)args[1]);
                            return null;
                        },
                        new object[] {wc[i], info}
                        );
                }
            }
        }

        private void SetExitCode(int exitCode)
        {
            if (_exitCode != exitCode)
            {
                _exitCode = exitCode;
                System.Environment.ExitCode = exitCode;
            }
        }

        private object ShutdownCallback(object arg)
        {
            ShutdownImpl();
            return null;
        }
        /// <summary>
        /// This method gets called on dispatch of the Shutdown DispatcherOperationCallback
        /// </summary>
        private void ShutdownImpl()
        {
            // Event handler exception continuality: if exception occurs in Exit event handler,
            // our cleanup action is to finish Shutdown since Exit cannot be cancelled. We don't
            // want user to use throw exception and catch it to cancel Shutdown.
            try
            {
                DoShutdown();
            }
            finally
            {
                // Quit the dispatcher if we ran our own.
                if (_ownDispatcherStarted)
                {
                    Dispatcher.CriticalInvokeShutdown();
                }
            }
        }

        private static bool IsValidShutdownMode(ShutdownMode value)
        {
            return value == ShutdownMode.OnExplicitShutdown
                || value == ShutdownMode.OnLastWindowClose
                || value == ShutdownMode.OnMainWindowClose;
        }

        private EventHandlerList Events
        {
            get
            {
                if (_events == null)
                {
                    _events = new EventHandlerList();
                }
                return _events;
            }
        }

        private object RunDispatcher(object ignore)
        {
            if (_ownDispatcherStarted)
            {
                throw new InvalidOperationException(SR.ApplicationAlreadyRunning);
            }
            _ownDispatcherStarted = true;
            Dispatcher.Run();
            return null;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private static object                           _globalLock;
        private static bool                             _isShuttingDown;
        private static bool                             _appCreatedInThisAppDomain;
        private static Application                      _appInstance;
        private static Assembly                         _resourceAssembly;

        private Uri                         _startupUri;
        private HybridDictionary            _htProps;
        private WindowCollection            _appWindowList;
        private WindowCollection            _nonAppWindowList;
        private Window                      _mainWindow;
        private ResourceDictionary          _resources;

        private bool                        _ownDispatcherStarted;

        private ThemeMode                   _themeMode = ThemeMode.None;
        private bool                        _resourcesInitialized = false;
        private bool                        _reloadFluentDictionary = false;

        private bool                        _appIsShutdown;
        private int                         _exitCode;

        private ShutdownMode                _shutdownMode = ShutdownMode.OnLastWindowClose;

        private HwndWrapper                 _parkingHwnd;

        private HwndWrapperHook             _appFilterHook;

        private EventHandlerList            _events;
        private bool                        _hasImplicitStylesInResources;

        private static readonly object EVENT_STARTUP = new object();
        private static readonly object EVENT_EXIT = new object();
        private static readonly object EVENT_SESSIONENDING = new object();

        #endregion Private Fields
    }

    #endregion Application Class

    #region enum ShutdownMode

    /// <summary>
    ///     Enum for ShutdownMode
    /// </summary>
    public enum ShutdownMode : byte
    {
        /// <summary>
        ///
        /// </summary>
        OnLastWindowClose = 0,

        /// <summary>
        ///
        /// </summary>
        OnMainWindowClose = 1,

        /// <summary>
        ///
        /// </summary>
        OnExplicitShutdown

        // NOTE: if you add or remove any values in this enum, be sure to update Application.IsValidShutdownMode()
    }

    #endregion enum ShutdownMode

    #region enum ReasonSessionEnding

    /// <summary>
    ///     Enum for ReasonSessionEnding
    /// </summary>
    public enum ReasonSessionEnding : byte
    {
        /// <summary>
        ///
        /// </summary>
        Logoff = 0,
        /// <summary>
        ///
        /// </summary>
        Shutdown
    }
    #endregion enum ReasonSessionEnding
}

