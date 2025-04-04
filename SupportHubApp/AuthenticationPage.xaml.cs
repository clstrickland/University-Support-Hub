using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Data;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading;
//using System.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace SupportHubApp
{
    public sealed partial class AuthenticationPage : Page
    {
        private readonly Logging _logging = new() { SubModuleName = "Authentication" };

        private AuthenticationHelper? _authHelper; // Use the AuthenticationHelper
        public static string? AccessToken { get; private set; } // Static property for token
        public static string? UserName { get; private set; } // Static property for name
        public static string? Email { get; private set; } // Static property for email

        private CancellationTokenSource? cts;

        private MainWindow? _mainWindow;

        readonly Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader;

        public AuthenticationPage()
        {
            this.InitializeComponent();
            _resourceLoader = ((Application.Current as App)?._resourceLoader) ?? throw new InvalidOperationException("App instance not found.");

        }

        private void CancelAuthButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("Authentication canceled by user.");

            cts?.Cancel();

            // Navigate back to HomePage.
            this.Frame.Navigate(typeof(HomePage), Window.Current); //Pass current window

            // Close the current window.
            var window = (Application.Current as App)?.Window;
            window?.Close();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Initialize AuthenticationHelper with the parent window.
            var window = ((Application.Current as App)?.Window) ?? throw new InvalidOperationException("Window not found.");
            _authHelper = new AuthenticationHelper(window);
            _mainWindow = (window as MainWindow) ?? throw new InvalidOperationException("Unable to get window as MainWindow instance");




            try
            {
                cts?.Dispose();
                cts = new CancellationTokenSource();
                (AccessToken, UserName, Email) = await _authHelper.GetAccessTokenAsync(cts.Token);
                // Successful authentication: Navigate to ReportIssuePage.
                this.Frame.Navigate(typeof(ReportIssuePage));
            }
            catch (AuthenticationExceptions.AuthCancelled)
            {
                _logging.LogInfo("Authentication canceled by user via WAM dialog.");
                this.Frame.GoBack();

            }
            catch (AuthenticationExceptions.AccessDenied)
            {
                _logging.LogWarning("Access denied.");
                progressRing.IsActive = false; // Stop the ProgressRing

                string Title = _resourceLoader.GetString("Auth/LoginFailedDialog/Title");
                string Content = _resourceLoader.GetString("Auth/LoginFailedDialog/AccessDeniedMessage");
                string PrimaryButtonText = _resourceLoader.GetString("Global_Confirm/Content");
                if (_mainWindow != null)
                {
                    ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                }
                else
                {
                    _logging.LogError("MainWindow instance is null. Cannot show alert.");
                }

                // Go back to home page
                this.Frame.GoBack();
            }
            catch (AuthenticationExceptions.ResponseMalformed)
            {
                _logging.LogError("Malformed response from authentication.");
                progressRing.IsActive = false; // Stop the ProgressRing

                string Title = _resourceLoader.GetString("Auth/LoginFailedDialog/Title");
                string Content = _resourceLoader.GetString("Auth/LoginFailedDialog/UnknownErrorMessage");
                string PrimaryButtonText = _resourceLoader.GetString("Global_Confirm/Content");
                if (_mainWindow != null)
                {
                    ContentDialogResult? result = await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                }
                else
                {
                    _logging.LogError("MainWindow instance is null. Cannot show alert.");
                }

                // Go back to home page
                this.Frame.GoBack();
            }
            catch (Exception ex)
            {

                // Authentication failed:  Show an error message (and *don't* navigate).
                // You should handle this more gracefully, perhaps with a dialog.
                _logging.LogException(ex);
                progressRing.IsActive = false; // Stop the ProgressRing


                string Title = _resourceLoader.GetString("Auth/LoginFailedDialog/Title");
                string Content = _resourceLoader.GetString("Auth/LoginFailedDialog/UnknownErrorMessage");
                string PrimaryButtonText = _resourceLoader.GetString("Global_Confirm/Content");
                if (_mainWindow != null)
                {
                    await _mainWindow.ShowAlert(Title, Content, PrimaryButtonText);
                }
                else
                {
                    _logging.LogError("MainWindow instance is null. Cannot show alert.");
                }

                // Go back to home page
                this.Frame.GoBack();

            }
        }
    }
}
