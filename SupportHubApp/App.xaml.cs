using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using H.NotifyIcon;


namespace SupportHubApp
{
    public partial class App : Application
    {
        #region Properties

        public TaskbarIcon? TrayIcon { get; private set; }
        public Window? Window { get; set; }
        public bool HandleClosedEvents { get; set; } = true;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

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
            string[] arguments = Environment.GetCommandLineArgs();
            bool isBackground = arguments.Contains("/background");

            if (IpcHelper.CheckProcessRunning(!isBackground))
            {
                string filePath = "C:\\Users\\CarterStrickland(Adm\\Desktop\\AnotherInstanceRunning.txt";
                File.WriteAllText(filePath, "Another instance is running.");
                ExitApp(); // Use the centralized exit method
                return;
            }

            //if (IpcHelper.IsAnotherInstanceRunning())
            //{
            //    string filePath = "C:\\Users\\CarterStrickland(Adm\\Desktop\\AnotherInstanceRunning.txt";
            //    File.WriteAllText(filePath, "Another instance is running by pipe.");
            //    ExitApp(); // Use the centralized exit method
            //    return;
            //}
            Action activateWindowAction = ActivateMainWindow;
            _ = Task.Run(() => IpcHelper.StartActivationListener(activateWindowAction, _cancellationTokenSource.Token));
            InitializeTrayIcon();
            if (Window == null)
            {
                Window = new MainWindow();
                Window.Closed += MainWindow_Closed; // Use a named handler
            }
            if (!isBackground)
            {
                 Window.Activate();
                
            }
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                Window?.Hide();
            }
            else
            {
                //No longer needed as it will handled below.
            }
        }
        private void InitializeTrayIcon()
        {
            var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
            showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;

            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

            TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
            TrayIcon.ForceCreate();
        }

        private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            if (Window == null)
            {
                Window = new MainWindow();
                Window.Closed += MainWindow_Closed; // Use a named handler
                Window.Activate();
                return;
            }

            if (Window.Visible)
            {
                Window.Hide();
            }
            else
            {
                Window.Activate();
            }
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
            Window?.Close();
        }

        private void ExitApp()
        {
            CleanUp();
            Environment.Exit(0); // Now it's safe to call Environment.Exit
        }

    }
}