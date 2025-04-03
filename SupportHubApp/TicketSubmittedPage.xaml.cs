// TicketSubmittedPage.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Navigation;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Windowing; //For presenter


namespace SupportHubApp
{
    public sealed partial class TicketSubmittedPage : Page
    {
        private readonly Logging _logging = new() { SubModuleName = "TicketSubmittedPage" };
        //private string _ticketId;
        //private CancellationTokenSource _cancellationTokenSource;

        public TicketSubmittedPage()
        {
            this.InitializeComponent();
            //_cancellationTokenSource = new CancellationTokenSource(); // Initialize the cancellation token source

        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);

        //    if (e.Parameter is string ticketId)
        //    {
        //        _ticketId = ticketId;
        //        ConfirmationTextBlock.Text = $"Ticket submitted! Your ticket ID is: {_ticketId}";
        //        //Start the polling
        //        //TicketManager.StartPollingTicketStatusAsync(_ticketId, UpdateStatus, _cancellationTokenSource.Token);
        //    }
        //    else
        //    {
        //        ConfirmationTextBlock.Text = "Ticket submitted! Error: Could not retrieve Ticket ID";
        //    }
        //}
        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    base.OnNavigatedFrom(e);
        //    _cancellationTokenSource.Cancel(); // Cancel the polling when navigating away
        //}

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _logging.LogInfo("User clicked the Close button.");
            // Navigate back to HomePage.
            this.Frame.Navigate(typeof(HomePage), Window.Current); //Pass current window

            // Close the current window.
            var window = (Application.Current as App)?.Window;
            window?.Close();

        }
    }
}
