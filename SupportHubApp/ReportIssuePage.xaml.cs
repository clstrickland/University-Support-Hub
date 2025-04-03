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
using Microsoft.UI.Dispatching; // Add this for DispatcherQueue


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
        readonly ContentDialog errorDialog = new();
        private DispatcherQueue? _dispatcherQueue; // Store the DispatcherQueue
        private readonly Logging _logging = new() { SubModuleName = "ReportIssuePage" };



        public ReportIssuePage()
        {
            this.InitializeComponent();
            this.Loaded += ReportIssuePage_Loaded; // Use the Loaded event



            // Get user info.
            string? name = AuthenticationPage.UserName;  // Assuming these are static
            string? email = AuthenticationPage.Email;
            userInfoTextBlock.Text = $"Submitting as {name} ({email})";

            // Check for capture support.
            if (!GraphicsCaptureSession.IsSupported())
            {
                _logging.LogWarning("Graphics Capture was reported as unsupported.");
                AttachScreenshotCheckBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ReportIssuePage_Loaded(object sender, RoutedEventArgs e)
        {
            var window = (Application.Current as App)?.Window; //Replace 'App' by the class name of your Application.
                                                               //OR, if the XAML root is available:

            if (window != null)  //Always check!
            {
                _hwnd = WindowNative.GetWindowHandle(window);
                // Now you can use _hwnd in your ScreenshotHelper
                _dispatcherQueue = this.DispatcherQueue; // Or window.DispatcherQueue, any UI element's DispatcherQueue

            }
        }


        private async void CancelIssueButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("Cancel button was clicked");
            // ... (No changes here, same as previous response) ...
            var errorDialog = new ContentDialog
            {
                Title = "Cancel Submission?",
                Content = "Do you want to close without sending the ticket?",
                PrimaryButtonText = "Go Back",
                CloseButtonText = "Cancel Ticket",
                XamlRoot = this.XamlRoot
            };
            ContentDialogResult? result = await errorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // User clicked "Go Back", do nothing and return.
                _logging.LogInfo("User clicked Go Back on Cancel Confirmation dialog");
                return; // Stop processing if user wants to go back
            }
            else
            {
                _logging.LogInfo("User clicked Cancel Ticket on Cancel Confirmation dialog");
                // Navigate back to HomePage.
                this.Frame.Navigate(typeof(HomePage), Window.Current); //Pass current window

                // Close the current window.
                var window = (Application.Current as App)?.Window;
                window?.Close();
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
                var errorDialog = new ContentDialog
                {
                    Title = "Missing Information",
                    Content = "Please enter both an issue title and description.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            ProgressRing.IsActive = true;
            ProgressRing.Visibility = Visibility.Visible;

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
                screenshotBytes = await helper.CaptureWithPickerAsync();
                //pickerWindow.Close();
                //window.Show();
            }
            //CoreWindow idkwindow = CoreWindow.FromAbi(_hwnd);


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
                errorDialog.XamlRoot = this.Content.XamlRoot;
                if (accessToken == null)
                {
                    _logging.LogError("Access token is null. Cannot submit ticket.");
                    // raise an exception
                    throw new Exception("Missing accessToken");
                }
                TicketManager? ticketManager = new("http://10.119.40.135:7071/api", accessToken);
                _logging.LogInfo("Invoking ticket creation.");
                string instanceId = await ticketManager.CreateTicketAsync(issueTitle, issueDescription, screenshotBytes, errorDialog);


                this.Frame.Navigate(typeof(TicketSubmittedPage));

                // Start polling in the background
                _logging.LogInfo("Starting polling for ticket status.");
                _ = Task.Run(async () =>
                {
                    string? status;
                    string? ticketId;
                    (status, ticketId) = await ticketManager.PollForStatusAsync(instanceId, errorDialog);
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
                var errorDialog = new ContentDialog
                {
                    Title = "Submission Failed",
                    Content = $"Could not submit the ticket: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                ProgressRing.IsActive = false;
                ProgressRing.Visibility = Visibility.Collapsed;
            }
        }

    }
}
