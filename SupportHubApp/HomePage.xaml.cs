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

        private readonly Logging _logging = new() { subModuleName = "HomePage" };
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
            _logging.LogInfo("User clicked the Check for Updates button");
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:windowsupdate"));
            CloseCurrentWindow();
        }

        private void ReportIssueButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("User clicked the Report Issue button.");
            // Navigate to the ReportIssuePage.
            this.Frame.Navigate(typeof(AuthenticationPage), Window.Current); //Pass current window

        }

        private async void OpenAppStoreButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("User clicked the Open App Store button.");
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

    }
}
