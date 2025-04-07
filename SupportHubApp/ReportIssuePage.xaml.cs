// ReportIssuePage.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
//using Windows.Graphics.Imaging;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Windows.Storage.Streams;
//using System.IO;



//using Microsoft.Graphics.Canvas;
////using Microsoft.Graphics.Canvas.UI.Composition;
//using Windows.Graphics.DirectX;
////using Windows.UI.Composition;
//using Microsoft.UI.Windowing;
//using Microsoft.Graphics.Canvas.UI.Composition;
//using Microsoft.UI.Composition;
//using Windows.UI.Core;
//using Windows.ApplicationModel.Core;
//using Windows.UI.ViewManagement;
using WinRT.Interop;
//using H.NotifyIcon;
//using Windows.Data.Xml.Dom;  // Add this using
using Windows.UI.Notifications;  // Add this using
using Microsoft.UI.Dispatching;
using System.Configuration; // Add this for DispatcherQueue


namespace SupportHubApp
{


    public sealed partial class ReportIssuePage : Page
    {
        // Capture API objects.
        //private CanvasDevice _canvasDevice;
        //private CompositionGraphicsDevice _compositionGraphicsDevice;
        //private Compositor _compositor;
        //private CompositionDrawingSurface _surface;
        //private GraphicsCaptureSession _session;
        //private Direct3D11CaptureFramePool _framePool;
        //private GraphicsCaptureItem _item;
        //private byte[] _screenshotBytes;

        //private CoreApplicationView _compactOverlayView;
        //private CoreWindow _compactOverlayCoreWindow;
        //private int _compactOverlayViewId;
        private IntPtr _hwnd;
        //readonly ContentDialog errorDialog = new();
        private DispatcherQueue? _dispatcherQueue; // Store the DispatcherQueue
        private readonly Logging _logging = new() { SubModuleName = "ReportIssuePage" };
        private Window? _window;
        private MainWindow? _mainWindow;
        readonly Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader;



        public ReportIssuePage()
        {
            this.InitializeComponent();
            this.Loaded += ReportIssuePage_Loaded; // Use the Loaded event
            _resourceLoader = ((Application.Current as App)?._resourceLoader) ?? throw new InvalidOperationException("App instance not found.");




        }

        private void ReportIssuePage_Loaded(object sender, RoutedEventArgs e)
        {
            _window = (Application.Current as App)?.Window; //Replace 'App' by the class name of your Application.
                                                            //OR, if the XAML root is available:

            _mainWindow = (_window as MainWindow) ?? throw new InvalidOperationException("Unable to get window as MainWindow instance");
            
            





            if (_window != null)  //Always check!
            {

                _hwnd = WindowNative.GetWindowHandle(_window);
                // Now you can use _hwnd in your ScreenshotHelper
                _dispatcherQueue = this.DispatcherQueue; // Or window.DispatcherQueue, any UI element's DispatcherQueue

                // Get user info.
                string? name = AuthenticationPage.UserName;  // Assuming these are static
                string? email = AuthenticationPage.Email;
                var prefix = _resourceLoader.GetString("ReportIssue/UserInfoPrefix/Text");
                userInfoTextBlock.Text = $"{prefix} {name} ({email})";

                // Check for capture support.
                if (!GraphicsCaptureSession.IsSupported())
                {
                    _logging.LogWarning("Graphics Capture was reported as unsupported.");
                    AttachScreenshotCheckBox.Visibility = Visibility.Collapsed;
                }
            }
        }


        private async void CancelIssueButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("Cancel button was clicked");
            string Title = _resourceLoader.GetString("ReportIssue/CancelDialog/Title");
            string Content = _resourceLoader.GetString("ReportIssue/CancelDialog/Content");
            string PrimaryButtonText = _resourceLoader.GetString("ReportIssue/CancelDialog/PrimaryButtonText");
            string CloseButtonText = _resourceLoader.GetString("ReportIssue/CancelDialog/CloseButtonText");
            if (_mainWindow != null)
            {
                ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText, CloseButtonText);


                if (result == ContentDialogResult.Primary)
                {
                    _logging.LogInfo("User clicked Go Back on Cancel Confirmation dialog");
                    return;
                }
                else
                {
                    _logging.LogInfo("User clicked Cancel Ticket on Cancel Confirmation dialog");
                    this.Frame.Navigate(typeof(HomePage), Window.Current);
                }
            }
            else
            {
                _logging.LogError("MainWindow instance is null. Cannot show alert.");
            }
        }

        private async void SubmitIssueButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("Submit button was clicked");
            // Get the issue details.
            string? issueTitle = IssueTitleTextBox.Text;
            string? issueDescription = IssueDescriptionTextBox.Text;
            bool attachScreenshot = AttachScreenshotCheckBox.IsChecked ?? false;
            string? accessToken = AuthenticationPage.AccessToken;

            // Input validation (same as before)
            if (string.IsNullOrWhiteSpace(issueTitle) || string.IsNullOrWhiteSpace(issueDescription))
            {
                _logging.LogWarning("Following validation errors:");
                if (string.IsNullOrWhiteSpace(issueTitle))
                {
                    _logging.LogWarning("Issue title is empty.");
                }
                if (string.IsNullOrWhiteSpace(issueDescription))
                {
                    _logging.LogWarning("Issue description is empty.");
                }

                string Title = _resourceLoader.GetString("ReportIssue/ValidationDialog/Title");
                string Content = _resourceLoader.GetString("ReportIssue/ValidationDialog/Content");
                string PrimaryButtonText = _resourceLoader.GetString("Global/ActionsLabels/Confirm/Content");
                if (_mainWindow != null)
                {
                    ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                }
                else
                {
                    _logging.LogError("MainWindow instance is null. Cannot show alert.");
                }
                return;
            }



            byte[]? screenshotBytes = null;

            if (attachScreenshot)
            {
                _logging.LogInfo("User selected to attach a screenshot. Starting capture process.");
                var window = (Application.Current as App)?.Window;
                //window.Hide();
                //var pickerWindow = new Window();
                //pickerWindow.Activate();
                //pickerWindow.
                //var pickerHwnd = WindowNative.GetWindowHandle(pickerWindow);

                // Capture a screenshot.
                var helper = new ScreenshotHelper(_hwnd);
                try
                {
                    screenshotBytes = await helper.CaptureWithPickerAsync();
                }
                catch (ArgumentNullException)
                {
                    // the user probably cancelled out of the picker
                    _logging.LogWarning("Screenshot capture was cancelled by the user.");

                    string Title = _resourceLoader.GetString("ReportIssue/NoScreenshotDialog/Title");
                    string Content = _resourceLoader.GetString("ReportIssue/NoScreenshotDialog/Content");
                    string PrimaryButtonText = _resourceLoader.GetString("Global/ActionsLabels/Confirm/Content");
                    if (_mainWindow != null)
                    {
                        ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                    }
                    else
                    {
                        _logging.LogError("MainWindow instance is null. Cannot show alert.");
                    }
                    return;

                }

                //pickerWindow.Close();
                //window.Show();
            }
            //CoreWindow idkwindow = CoreWindow.FromAbi(_hwnd);
            ProgressRing.IsActive = true;
            ProgressRing.Visibility = Visibility.Visible;

            //await idkwindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //{


            //if (screenshotBytes != null)
            //{
            //    // save it to a file on the user desktop
            //    string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\screenshot.png";
            //    System.IO.File.WriteAllBytes(filePath, screenshotBytes);

            //}

            // *** HIDE WINDOW (Corrected to use CompactOverlay) ***
            //var window = (Application.Current as App)?.Window;
            //if (window != null)
            //{
            //    if (window.AppWindow.Presenter.Kind == AppWindowPresenterKind.CompactOverlay)
            //    {
            //        window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
            //    }
            //    else
            //    {
            //        window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            //    }

            //}


            try
            {
                //errorDialog.XamlRoot = this.Content.XamlRoot;
                if (accessToken == null)
                {
                    _logging.LogError("Access token is null. Cannot submit ticket.");
                    // raise an exception
                    throw new Exception("Missing accessToken");
                }

                string? endpoint = ConfigurationManager.AppSettings["TicketSubmissionEndpoint"];

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    _logging.LogError("Ticket Submission Endpoint value from system configuration is invalid.");
                    throw new Exception("Invalid app configuration. Please contact support.");
                }

                TicketManager? ticketManager = new(endpoint, accessToken);
                _logging.LogInfo("Invoking ticket creation.");
                // disable cancel button
                CancelIssueButton.IsEnabled = false;
                SubmitIssueButton.IsEnabled = false;
                string instanceId = await ticketManager.CreateTicketAsync(issueTitle, issueDescription, screenshotBytes);


                this.Frame.Navigate(typeof(TicketSubmittedPage));

                // Start polling in the background
                _logging.LogInfo("Starting polling for ticket status.");
                _ = Task.Run(async () =>
                {
                    string? status;
                    string? ticketId;
                    (status, ticketId) = await ticketManager.PollForStatusAsync(instanceId);
                    // Use DispatcherQueue to update UI elements
                    _dispatcherQueue?.TryEnqueue(
                          DispatcherQueuePriority.Normal,
                           () =>
                           {
                               _logging.LogInfo("Polling completed. Updating UI.");

                               var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

                               // Modify the toast XML to support actions (buttons).  ToastText02 doesn't support them by default.
                               toastXml.DocumentElement.SetAttribute("template", "ToastGeneric");


                               var toastTextElements = toastXml.GetElementsByTagName("text");
                               toastTextElements[0].AppendChild(toastXml.CreateTextNode("Self-Service Ticket Submission"));

                               if (status == "success")
                               {
                                   _logging.LogInfo("Ticket created successfully.");
                                   toastTextElements[1].AppendChild(toastXml.CreateTextNode($"Ticket #{ticketId} was created successfully"));

                                   // Add a "go to ticket" button that goes to a URL.
                                   var actions = toastXml.GetElementsByTagName("actions").Item(0);

                                   // If actions node doesn't exist (which it won't in ToastText02), create it.
                                   if (actions == null)
                                   {
                                       actions = toastXml.CreateElement("actions");
                                       toastXml.DocumentElement.AppendChild(actions); // Append to the root of the toast XML.
                                   }

                                   var action = toastXml.CreateElement("action");
                                   action.SetAttribute("content", "Go to ticket");
                                   // IMPORTANT:  Use a valid protocol for the arguments.  "https://" is required.
                                   action.SetAttribute("arguments", $"https://www.tamu.edu"); //Or any other valid URL.
                                   action.SetAttribute("activationType", "protocol"); // This makes the button launch a URL

                                   // *CRITICAL FIX:*  Append the action to the actions element.
                                   actions.AppendChild(action);
                               }
                               else if (ticketId != "")
                               {
                                   _logging.LogError($"Ticket creation failed. Status: {status}");
                                   toastTextElements[1].AppendChild(toastXml.CreateTextNode($"There was an issue creating your ticket\nError: {status}\nTicket ID: {ticketId}"));
                               }
                               else
                               {
                                   _logging.LogError($"Ticket creation failed. Status: {status}");
                                   toastTextElements[1].AppendChild(toastXml.CreateTextNode($"There was an issue creating your ticket\nError: {status}"));
                               }


                               var toast = new ToastNotification(toastXml);
                               ToastNotificationManager.CreateToastNotifier().Show(toast);
                           });
                });
            }

            catch (Exception ex)
            {
                _logging.LogException(ex);
                // ... (Error handling, same as before) ...

                string Title = _resourceLoader.GetString("ReportIssue/SubmitFailedDialog/Title");
                string Content = $"{_resourceLoader.GetString("ReportIssue/SubmitFailedDialog/Content")}: {ex.Message}";
                string PrimaryButtonText = _resourceLoader.GetString("Global/ActionsLabels/Confirm/Content");
                if (_mainWindow != null)
                {
                    ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                }
                else
                {
                    _logging.LogError("MainWindow instance is null. Cannot show alert.");
                }
                return;

            }
            finally
            {
                ProgressRing.IsActive = false;
                ProgressRing.Visibility = Visibility.Collapsed;
                CancelIssueButton.IsEnabled = true;
                SubmitIssueButton.IsEnabled = true;
            }
        }

    }
}
