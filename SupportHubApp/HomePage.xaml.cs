using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace SupportHubApp
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        private static void CloseCurrentWindow()
        {
            var window = (Application.Current as App)?.Window;
            window?.Close();
        }

        private async void CheckForUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Simulate checking for an update (replace with your actual update logic)
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:windowsupdate"));
            CloseCurrentWindow();
        }

        private void ReportIssueButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the ReportIssuePage.
            this.Frame.Navigate(typeof(AuthenticationPage), Window.Current); //Pass current window

        }

        private async void OpenAppStoreButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("companyportal://portal.manage.microsoft.com/apps/"));
            CloseCurrentWindow();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Reset visual state when navigating *back* to this page.
            VisualStateManager.GoToState(this, "IdleState", false);
            progress1.IsActive = false;
        }

        private async Task SimulateLoading()
        {
            // Show the ProgressRing and hide the icon.
            VisualStateManager.GoToState(this, "LoadingState", true);
            progress1.IsActive = true;
            await Task.Delay(3000); // Simulate a 3-second delay
            progress1.IsActive = false;
            VisualStateManager.GoToState(this, "IdleState", false);
        }
    }
}
