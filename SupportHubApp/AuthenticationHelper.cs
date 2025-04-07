using System;
//using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
//using Microsoft.UI.Xaml.Controls;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Broker;
using System.Threading;
//using System.Windows;

namespace SupportHubApp
{

    public class AuthenticationExceptions()
    {
        [Serializable]
        public class AccessDenied : Exception
        {
            public AccessDenied()
            { }

            public AccessDenied(string message)
                : base(message)
            { }

            public AccessDenied(string message, Exception innerException)
                : base(message, innerException)
            { }
        }
        [Serializable]
        public class AuthCancelled : Exception
        {
            public AuthCancelled()
            { }

            public AuthCancelled(string message)
                : base(message)
            { }

            public AuthCancelled(string message, Exception innerException)
                : base(message, innerException)
            { }
        }

        [Serializable]
        public class ResponseMalformed : Exception
        {
            public ResponseMalformed()
            { }

            public ResponseMalformed(string message)
                : base(message)
            { }

            public ResponseMalformed(string message, Exception innerException)
                : base(message, innerException)
            { }
        }

    }
    public class AuthenticationHelper(Window parentWindow)
    {

        private readonly Logging _logging = new() { SubModuleName = "AuthenticationHelper" };

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
                try
                {
                    _logging.LogInfo("Attempting to acquire token silently.");
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

                    _logging.LogInfo("Silent token acquisition failed. Attempting interactive authentication.");
                    // Pass cancellationToken to AcquireTokenInteractive
                    result = await app.AcquireTokenInteractive(scopes)
                                      .ExecuteAsync(cancellationToken); // Pass token here
                }
            }
            catch (MsalException ex) when (ex.ErrorCode == "access_denied") //common exception
            {
                _logging.LogWarning("Access denied.");
                throw new AuthenticationExceptions.AccessDenied();
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled") //common exception
            {
                _logging.LogInfo("Authentication was canceled.");
                throw new AuthenticationExceptions.AuthCancelled();
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully (e.g., log, return a specific value)
                _logging.LogInfo("Authentication was canceled.");
                throw new AuthenticationExceptions.AuthCancelled();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging.
                _logging.LogException(ex);
                throw;
            }


            // Check for cancellation *before* processing the result
            cancellationToken.ThrowIfCancellationRequested();

            var claims = result.ClaimsPrincipal.Claims;
            string? name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
            string? email = claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value; // Check both claim types

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                _logging.LogWarning("Name and/or email attributes missing from token");
                throw new AuthenticationExceptions.ResponseMalformed("Name and/or email attributes missing from token");
            }

            return (result.AccessToken, name, email);
        }
    }

}
