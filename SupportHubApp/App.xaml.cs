using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using H.NotifyIcon;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;



namespace SupportHubApp
{


    public partial class App : Application
    {
        #region Properties
        public TaskbarIcon? TrayIcon { get; private set; }
        public Window? Window { get; set; }
        public bool HandleClosedEvents { get; set; } = true;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly Logging _logging = new() { SubModuleName = "App" };

        public readonly Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
        #endregion

        public App()
        {
            this.InitializeComponent();

            // Subscribe to ProcessExit - This is the key to catching Environment.Exit
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            // Handle unhandled exceptions gracefully
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _logging.LogInfo($"--- Instance Start at {DateTime.Now} ---");
            


            string[] arguments = Environment.GetCommandLineArgs();
            bool isBackground = arguments.Contains("/background");

            if (IpcHelper.CheckProcessRunning(!isBackground))
            {
                _logging.LogInfo("Another instance is running.");
                ExitApp(); // Use the centralized exit method
                return;
            }

            Action activateWindowAction = ActivateMainWindow;
            _ = Task.Run(() => IpcHelper.StartActivationListener(activateWindowAction, _cancellationTokenSource.Token));
            if (Window == null)
            {
                Window = new MainWindow();
                Window.Closed += MainWindow_Closed; // Use a named handler
            }
            if (!isBackground)
            {
                 Window.Activate();
                
            }
            InitializeTrayIcon();
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                Window?.Hide();
            }
        }
        private void InitializeTrayIcon()
        {
            var showWindowCommand = (XamlUICommand)Resources["ShowWindowCommand"];
            showWindowCommand.ExecuteRequested += ShowWindowCommand_ExecuteRequested;

            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

            var OpenFeedbackCommand = (XamlUICommand)Resources["OpenFeedbackCommand"];
            OpenFeedbackCommand.ExecuteRequested += OpenFeedbackCommand_ExecuteRequested;

            var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
            showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;

            TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
            TrayIcon.ToolTipText = _resourceLoader.GetString("TrayIcon/ToolTipText");
            TrayIcon.ForceCreate();
            _logging.LogInfo("Tray icon initialized.");
        }

        private async void OpenFeedbackCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {

            string? endpoint = ConfigurationManager.AppSettings["FeedbackEndpoint"];

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _logging.LogError("Feedback URI value from system configuration is invalid.");
                return;
                //throw new Exception("Invalid app configuration. Please contact support.");
            }


            // Execute an msteams:// link
            await Windows.System.Launcher.LaunchUriAsync(new Uri(endpoint));
            //try
            //{
            //    var uri = new Uri("msteams://");
            //    var process = new System.Diagnostics.Process
            //    {
            //        StartInfo = new ProcessStartInfo
            //        {
            //            FileName = uri.ToString(),
            //            UseShellExecute = true
            //        }
            //    };
            //    process.Start();
            //    _logging.LogInfo("Feedback command executed: msteams://");
            //}
            //catch (Exception ex)
            //{
            //    _logging.LogError($"Failed to execute feedback command: {ex.Message}");
            //}
        }

        private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            TrayIcon?.CloseTrayPopup();
            if (Window == null)
            {
                Window = new MainWindow();
                Window.Closed += MainWindow_Closed; // Use a named handler
                Window.Activate();
                _logging.LogInfo("Window created and activated.");
                return;
            }

            if (Window.Visible)
            {
                _logging.LogInfo("Window hidden.");
                // testing potential race condition, add 1 second delay

                Window.Hide();
            }
            else
            {
                Window.Activate();
                
                _logging.LogInfo("Window activated.");
            }
        }

        private void ShowWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            if (Window == null)
            {
                Window = new MainWindow();
                Window.Closed += MainWindow_Closed; // Use a named handler
                Window.Activate();
                _logging.LogInfo("Window created and activated.");
                return;
            }

            
            Window.Activate();
            _logging.LogInfo("Window activated.");
            
        }

        private void ExitApplicationCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            ExitApp();
        }

        private void ActivateMainWindow()
        {
            // Ensure this runs on the UI thread.
            if (Window != null)
            {
                if (Window.DispatcherQueue.HasThreadAccess)
                {
                    Window.Activate();
                }
                else
                {
                    Window.DispatcherQueue.TryEnqueue(() => Window.Activate());
                }
            }
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            CleanUp();
        }

        private void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            // Log the exception (consider using a logging library)
            Exception ex = (Exception)e.ExceptionObject;
            Console.Error.WriteLine($"Unhandled exception: {ex}");

            // Perform cleanup
            CleanUp();

            // You might want to display an error message to the user *before* exiting,
            // but be careful about UI operations in this context.  A simple
            // MessageBox might be okay, but avoid anything complex.
        }

        private void CleanUp()
        {
            // Centralized cleanup logic
            HandleClosedEvents = false; // Prevent re-entrancy issues
            _cancellationTokenSource.Cancel(); // Stop IPC listener
            TrayIcon?.Dispose(); // Dispose of the tray icon
            //Window?.Close();
        }

        private void ExitApp()
        {
            _logging.LogInfo("Exiting application.");
            CleanUp();
            Environment.Exit(0); // Now it's safe to call Environment.Exit
        }

    }
}