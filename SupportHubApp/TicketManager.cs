using System;
//using System.Collections.Generic;
//using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
//using System.Text;
using System.Text.Json;
//using System.Threading;
using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Controls; // For ContentDialog (error handling)

namespace SupportHubApp
{
    public class TicketManager
    {
        private readonly HttpClient _httpClient;
        private readonly string? _baseUrl;  // e.g., "https://your-api.com"
        private readonly string? _authToken; // Your API authentication token
        private readonly Logging _logging = new() { SubModuleName = "TicketManager" };

        public TicketManager(string baseUrl, string authToken)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logging.LogError("Base URL cannot be null or empty.");
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }
            _baseUrl = baseUrl;
            _authToken = authToken;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken); // Or "Basic", etc.
        }

        //public async Task<string> CreateAndPollTicketAsync(string title, string description, byte[] imageBytes, ContentDialog errorDialog)
        //{
        //    string instanceId = await CreateTicketAsync(title, description, imageBytes, errorDialog);

        //    if (string.IsNullOrEmpty(instanceId))
        //    {
        //        return null; // Ticket creation failed.  Error handled in CreateTicketAsync.
        //    }

        //    return await PollForStatusAsync(instanceId, errorDialog);
        //}


        public async Task<string> CreateTicketAsync(string title, string description, byte[]? imageBytes)
        {
            try
            {
                using var content = new MultipartFormDataContent
                {
                    { new StringContent(title), "title" },
                    { new StringContent(description), "description" }
                };

                if (imageBytes != null)
                {
                    _logging.LogInfo("Adding image to ticket.");
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png"); // Or "image/jpeg", etc.
                    content.Add(imageContent, "image", "screenshot.png"); // "image" is the field name, "screenshot.png" is a suggested filename
                }

                HttpResponseMessage? response = await _httpClient.PostAsync($"{_baseUrl}/tickets", content); // This and other calls need to start using the CancellationToken param for proper cancel operations

                if (response.IsSuccessStatusCode)
                {
                    _logging.LogInfo("Ticket created successfully.");
                    string? responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response to get the instance ID.  Use System.Text.Json.
                    using JsonDocument? doc = JsonDocument.Parse(responseContent);
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("instance_id", out JsonElement instanceIdElement))
                    {
                        _logging.LogInfo("Instance ID found in response.");
                        if (instanceIdElement.ValueKind != JsonValueKind.String)
                        {
                            _logging.LogError("Invalid response format: instance_id is not a string.");
                            // Handle the case where "instance_id" is not a string.
                            //await ShowErrorDialog(errorDialog, "There was an unexpected response. Please contact support.");
                            throw new Exception("Invalid response format");
                        }
                        string? instanceId = instanceIdElement.GetString();
                        _logging.LogInfo($"Instance ID: {instanceId}");
                        if (string.IsNullOrEmpty(instanceId))
                        {
                            _logging.LogError("Invalid response format: instance_id is empty.");
                            // Handle the case where "instance_id" is empty.
                            //await ShowErrorDialog(errorDialog, "There was an unexpected response. Please contact support.");
                            throw new Exception("Invalid response format");
                        }
                        else
                        {
                            return instanceId;
                        }
                    }
                    else
                    {
                        // Handle the case where "instance_id" is not in the response
                        //(even if the request returns 200).
                        _logging.LogError("Invalid response format: missing instance_id.");
                        //await ShowErrorDialog(errorDialog, "There was an unexpected response. Please contact support.");
                        throw new Exception("Invalid response format");
                    }
                }
                else
                {
                    // Handle API errors (e.g., 400 Bad Request, 500 Internal Server Error).
                    string? errorContent = await response.Content.ReadAsStringAsync();
                    _logging.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    //await ShowErrorDialog(errorDialog, "There was an unexpected response. Please contact support.");
                    throw new Exception("API Error");
                }
            }
            catch (HttpRequestException ex)
            {
                _logging.LogException(ex);
                // Handle network errors (e.g., no internet connection).
                //await ShowErrorDialog(errorDialog, $"Network Error: {ex.Message}");
                throw new Exception("Network Error");
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                // Handle unexpected errors
                //await ShowErrorDialog(errorDialog, $"An unexpected error occurred. Please contact support.");
                throw new Exception("Unexpected Error");
            }
        }

        public async Task<(string status, string ticketId)> PollForStatusAsync(string instanceId)
        {
            string? status = "";
            string? ticketId = "";
            string? runtimeStatus = "";

            try
            {
                _logging.LogInfo($"Polling for status of instance ID: {instanceId}");
                int pollCount = 0;
                while (true) // Keep polling until we get a status
                {
                    _logging.LogInfo($"Polling attempt {pollCount++}");
                    HttpResponseMessage? response = await _httpClient.GetAsync($"{_baseUrl}/checkstatus?instance_id={instanceId}");

                    if (response.IsSuccessStatusCode)
                    {
                        _logging.LogInfo("Polling successful.");
                        string? responseContent = await response.Content.ReadAsStringAsync();
                        using JsonDocument? doc = JsonDocument.Parse(responseContent);
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("status", out JsonElement statusElement))
                        {
                            _logging.LogInfo("Status found in response.");
                            if (statusElement.ValueKind != JsonValueKind.String)
                            {
                                _logging.LogError("Invalid response format: status is not a string.");
                                // Handle the case where "status" is not a string.
                                //await ShowErrorDialog(errorDialog, "Invalid response format: status is not a string.");
                                throw new Exception("Invalid response format");
                            }
                            status = statusElement.GetString(); // Return the status
                        }
                        _logging.LogInfo($"Status: {status}");

                        if (root.TryGetProperty("ticket_id", out JsonElement ticketIdElement))
                        {
                            _logging.LogInfo("Ticket ID found in response.");
                            if (ticketIdElement.ValueKind != JsonValueKind.String)
                            {
                                _logging.LogError("Invalid response format: ticket_id is not a string.");
                                // Handle the case where "ticket_id" is not a string.
                                //await ShowErrorDialog(errorDialog, "Invalid response format: ticket_id is not a string.");
                                throw new Exception("Invalid response format");
                            }
                            ticketId = ticketIdElement.GetString(); // Return the status
                        }
                        _logging.LogInfo($"Ticket ID: {ticketId}");

                        if (root.TryGetProperty("runtimeStatus", out JsonElement runtimeStatusElement))
                        {
                            _logging.LogInfo("Runtime status found in response.");
                            if (runtimeStatusElement.ValueKind != JsonValueKind.String)
                            {
                                _logging.LogError("Invalid response format: runtimeStatus is not a string.");
                                // Handle the case where "runtimeStatus" is not a string.
                                //await ShowErrorDialog(errorDialog, "Invalid response format: runtimeStatus is not a string.");
                                throw new Exception("Invalid response format");
                            }
                            runtimeStatus = runtimeStatusElement.GetString(); // Return the status
                        }
                        _logging.LogInfo($"Runtime Status: {runtimeStatus}");

                        if (string.IsNullOrEmpty(runtimeStatus))
                        {
                            _logging.LogError("Invalid response format: missing status.");
                            //await ShowErrorDialog(errorDialog, "There was an unexpected error. Please contact support.");
                            throw new Exception("Invalid response format");
                        }


                        if (runtimeStatus == "Completed")
                        {
                            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(ticketId))
                            {
                                _logging.LogError("Missing following from response:");
                                if (string.IsNullOrEmpty(status))
                                {
                                    _logging.LogError("\tStatus is empty.");
                                }
                                if (string.IsNullOrEmpty(ticketId))
                                {
                                    _logging.LogError("\tTicket ID is empty.");
                                }
                                //await ShowErrorDialog(errorDialog, "There was an unexpected error. Please contact support.");
                                throw new Exception("Invalid response format");
                            }
                            return (status, ticketId);
                        }
                    }
                    else
                    {
                        string? errorContent = await response.Content.ReadAsStringAsync();
                        _logging.LogError($"API Error during polling ({response.StatusCode}): {errorContent}");
                        //await ShowErrorDialog(errorDialog, "There was an unexpected error. Please contact support.");
                        throw new Exception("API Error during polling");
                    }

                    // Wait before polling again (e.g., 5 seconds).  Don't overload the server!
                    await Task.Delay(5000);
                }
            }
            catch (HttpRequestException ex)
            {
                _logging.LogException(ex);
                //await ShowErrorDialog(errorDialog, $"Network Error during polling: {ex.Message}");
                throw new Exception("Network Error during polling");
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                //await ShowErrorDialog(errorDialog, "There was an unexpected error. Please contact support.");
                throw new Exception("Unexpected Error during polling");
            }
        }

        //private static async Task ShowErrorDialog(ContentDialog errorDialog, string message)
        //{
        //    errorDialog.Title = "Error";
        //    errorDialog.Content = message;
        //    errorDialog.CloseButtonText = "OK";

        //    // Make sure the dialog is shown on the UI thread.
        //    if (errorDialog.DispatcherQueue.HasThreadAccess)
        //    {

        //        await errorDialog.ShowAsync();
        //    }
        //    else
        //    {
        //        errorDialog.DispatcherQueue.TryEnqueue(
        //            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
        //            async () => await errorDialog.ShowAsync());
        //    }
        //}
    }
}