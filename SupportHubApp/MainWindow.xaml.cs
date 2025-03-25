using System;
//using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
using Windows.Graphics;
//using Microsoft.UI.Xaml.Controls;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Broker;
using Microsoft.UI.Xaml.Navigation;
using System.Threading;
//using System.Windows;

namespace SupportHubApp
{
    public sealed partial class MainWindow : Window
    {
        private double _initialWidth;
        private double _initialHeight;
        private IntPtr _hwnd;
        private int _initialX;
        private int _initialY;
        private AppWindow? _appWindow;
        public MainWindow()
        {
            this.InitializeComponent();
            InitializeWindow();
            ContentFrame.Navigate(typeof(HomePage));
            ContentFrame.Navigated += ContentFrame_Navigated;
            // Removed: this.Activated += MainWindow_Activated; // No longer needed
        }

        

        private void InitializeWindow()
        {
            _hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null)
            {
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true; // Keep this
                _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                _appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

                //_appWindow.TitleBar.ExtendsContentIntoTitleBar = true; // Keep this
                //_appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                //_appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                //var presenter = _appWindow.Presenter as OverlappedPresenter;
                //if (presenter != null)
                //{
                //    presenter.IsAlwaysOnTop = true;
                //    presenter.SetBorderAndTitleBar(false, false); // Keep border and titlebar GONE
                //    presenter.IsResizable = false;
                //}
            }
            ////Correctly removes the border
            //int style = GetWindowLong(_hwnd, GWL_STYLE);
            //SetWindowLong(_hwnd, GWL_STYLE, (int)(style & ~WS_BORDER & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX & ~WS_SYSMENU));


            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            _initialWidth = workArea.Width * 0.25;
            _initialHeight = _initialWidth * 2.0 / 3.0;
            var width = (int)Math.Round(_initialWidth);
            var height = (int)Math.Round(_initialHeight);

            _initialX = (int)Math.Round((double)(workArea.X + workArea.Width - width - 10));
            _initialY = (int)Math.Round((double)(workArea.Y + workArea.Height - height - 10));

            _appWindow?.MoveAndResize(new RectInt32(_initialX, _initialY, width, height));
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is ReportIssuePage reportIssuePage)
            {
                // Enable vertical resizing.  
                var presenter = _appWindow?.Presenter as CompactOverlayPresenter;
                //if (presenter != null)
                //{
                //    presenter.IsResizable = true;
                //}

                // Hide the custom close button on ReportIssuePage.  
                //CloseButton.Visibility = Visibility.Collapsed;  

                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, (Microsoft.UI.Dispatching.DispatcherQueueHandler)(() =>
                {
                    reportIssuePage.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    var desiredHeight = reportIssuePage.DesiredSize.Height;
                    //var desiredHeight = reportIssuePage.getHeight();

                    int newHeight = (int)Math.Round(desiredHeight + 48);  // +40 for padding  
                    int newWidth = (int)Math.Round(_initialWidth);
                    int newX = _initialX;
                    int newY = _initialY - (newHeight - (int)Math.Round(_initialHeight));

                    var displayArea = DisplayArea.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd), DisplayAreaFallback.Nearest);
                    var workArea = displayArea.WorkArea;

                    // Keep window within work area bounds.  
                    if (newY < workArea.Y) { newY = workArea.Y; }
                    if (newY + newHeight > workArea.Y + workArea.Height)
                    {
                        newY = workArea.Y + workArea.Height - newHeight - 10;
                        if (newY < workArea.Y)
                        {
                            newHeight = workArea.Height;
                            newY = workArea.Y;
                        }
                    }
                    if (newX < workArea.X) { newX = workArea.X; }
                    if (newX + newWidth > workArea.X + workArea.Width)
                    {
                        newX = workArea.X + workArea.Width - newWidth - 10;
                        if (newX < workArea.X)
                        {
                            newX = workArea.X;
                            newWidth = workArea.Width;
                        }
                    }
                    _appWindow?.MoveAndResize(new RectInt32 { X = newX, Y = newY, Width = newWidth, Height = newHeight });
                }));
            } else
            {
                var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;
                _initialWidth = workArea.Width * 0.25;
                _initialHeight = _initialWidth * 2.0 / 3.0;
                var width = (int)Math.Round(_initialWidth);
                var height = (int)Math.Round(_initialHeight);

                _initialX = (int)Math.Round((double)(workArea.X + workArea.Width - width - 10));
                _initialY = (int)Math.Round((double)(workArea.Y + workArea.Height - height - 10));

                _appWindow?.MoveAndResize(new RectInt32(_initialX, _initialY, width, height));

            }
            //else  
            //{  
            //    // Disable resizing, restore initial size/position.  
            //    var presenter = _appWindow.Presenter as CompactOverlayPresenter;  
            //    if (presenter != null)  
            //    {  
            //        presenter.IsResizable = false;  
            //    }  

            //    // Show the custom close button on other pages.  
            //    CloseButton.Visibility = Visibility.Visible;  
            //    _appWindow.MoveAndResize(new RectInt32(_initialX, _initialY, (int)Math.Round(_initialWidth), (int)Math.Round(_initialHeight)));  
            //}  
        }


        //private void CloseButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _appWindow.Hide(); // Hide the window.
        //}

        // --- P/Invoke Declarations ---
        //private const int GWL_STYLE = -16;
        //private const uint WS_BORDER = 0x00800000;
        //private const uint WS_CAPTION = 0x00C00000;
        //private const uint WS_THICKFRAME = 0x00040000;
        //private const uint WS_MINIMIZEBOX = 0x00020000;
        //private const uint WS_MAXIMIZEBOX = 0x00010000;
        //private const uint WS_SYSMENU = 0x00080000;

        //[LibraryImport("user32.dll", SetLastError = true)]
        //private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        //[LibraryImport("user32.dll")]
        //private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        // --- End P/Invoke Declarations ---

    }
    public class AuthenticationHelper(Window parentWindow)
    {

        // Add CancellationToken parameter
        public async Task<(string AccessToken, string Name, string Email)> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var scopes = new[] { "openid", "api://42c1dd1d-4e40-4776-aa9d-a8c494f9aabb/Tickets.Create" };

            BrokerOptions options = new(BrokerOptions.OperatingSystems.Windows)
            {
                Title = "Self-Service Ticket Submission Tool"
            };

            IPublicClientApplication app =
                PublicClientApplicationBuilder.Create("ecf53a39-bddf-4b61-8dfc-a58cf9ac0c22")
                .WithDefaultRedirectUri()
                .WithParentActivityOrWindow(() => WindowNative.GetWindowHandle(parentWindow)) // Use _parentWindow
                .WithBroker(options)
                .WithAuthority("https://login.microsoftonline.com/68f381e3-46da-47b9-ba57-6f322b8f0da1/v2.0")
                .Build();

            AuthenticationResult? result = null;

            IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
            try
            {
                IAccount? existingAccount = accounts.FirstOrDefault();
                if (existingAccount != null)
                {
                    // Pass cancellationToken to AcquireTokenSilent
                    result = await app.AcquireTokenSilent(scopes, existingAccount)
                                      .WithForceRefresh(true)
                                      .ExecuteAsync(cancellationToken); // Pass token here
                }
                else
                {
                    // Pass cancellationToken to AcquireTokenSilent
                    result = await app.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount)
                                      .WithForceRefresh(true)
                                      .ExecuteAsync(cancellationToken); // Pass token here
                }
            }
            catch (MsalUiRequiredException)
            {
                // Pass cancellationToken to AcquireTokenInteractive
                result = await app.AcquireTokenInteractive(scopes)
                                  .ExecuteAsync(cancellationToken); // Pass token here
            }
            catch (MsalException ex) when (ex.ErrorCode == "access_denied") //common exception
            {
                throw new Exception("Access denied.");
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully (e.g., log, return a specific value)
                Console.WriteLine("Authentication was canceled.");
                throw new Exception("Authentication was canceled.");
            }


            // Check for cancellation *before* processing the result
            cancellationToken.ThrowIfCancellationRequested();

            var claims = result.ClaimsPrincipal.Claims;
            string? name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
            string? email = claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value; // Check both claim types

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                throw new Exception("Name and/or email attributes missing from token");
            }

            return (result.AccessToken, name, email);
        }
    }

}
