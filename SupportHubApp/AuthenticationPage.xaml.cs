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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace SupportHubApp
{
    public sealed partial class AuthenticationPage : Page
    {
        private AuthenticationHelper? _authHelper; // Use the AuthenticationHelper
        public static string? AccessToken { get; private set; } // Static property for token
        public static string? UserName { get; private set; } // Static property for name
        public static string? Email { get; private set; } // Static property for email

        private CancellationTokenSource? cts;

        public AuthenticationPage()
        {
            this.InitializeComponent();
        }

        private void CancelAuthButton_Click(object sender, RoutedEventArgs e)
        {

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


            try
            {
                cts?.Dispose();
                cts = new CancellationTokenSource();
                (AccessToken, UserName, Email) = await _authHelper.GetAccessTokenAsync(cts.Token);
                // Successful authentication: Navigate to ReportIssuePage.
                this.Frame.Navigate(typeof(ReportIssuePage));
            }
            catch (Exception ex)
            {

                //if type is System.OperationCanceledException, ignore
                if (ex is System.OperationCanceledException)
                {
                    return;
                }
                // Authentication failed:  Show an error message (and *don't* navigate).
                // You should handle this more gracefully, perhaps with a dialog.
                progressRing.IsActive = false; // Stop the ProgressRing
                var errorDialog = new ContentDialog
                {
                    Title = "Authentication Failed",
                    Content = $"Could not authenticate: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();

                // Go back to home page
                this.Frame.GoBack();

            }
        }
    }
}
