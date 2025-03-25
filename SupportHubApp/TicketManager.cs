using System;
//using System.Collections.Generic;
//using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
//using System.Text;
using System.Text.Json;
//using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls; // For ContentDialog (error handling)

namespace SupportHubApp
{
    public class TicketManager
    {
        private readonly HttpClient _httpClient;
        private readonly string? _baseUrl;  // e.g., "https://your-api.com"
        private readonly string? _authToken; // Your API authentication token

        public TicketManager(string baseUrl, string authToken)
        {
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


        public async Task<string> CreateTicketAsync(string title, string description, byte[]? imageBytes, ContentDialog errorDialog)
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
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png"); // Or "image/jpeg", etc.
                    content.Add(imageContent, "image", "screenshot.png"); // "image" is the field name, "screenshot.png" is a suggested filename
                }

                HttpResponseMessage? response = await _httpClient.PostAsync($"{_baseUrl}/tickets", content);

                if (response.IsSuccessStatusCode)
                {
                    string? responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response to get the instance ID.  Use System.Text.Json.
                    using JsonDocument? doc = JsonDocument.Parse(responseContent);
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("instance_id", out JsonElement instanceIdElement))
                    {
                        if (instanceIdElement.ValueKind != JsonValueKind.String)
                        {
                            // Handle the case where "instance_id" is not a string.
                            await ShowErrorDialog(errorDialog, "Invalid response format: instance_id is not a string.");
                            throw new Exception("Invalid response format");
                        }
                        string? instanceId = instanceIdElement.GetString();
                        if (string.IsNullOrEmpty(instanceId))
                        {
                            // Handle the case where "instance_id" is empty.
                            await ShowErrorDialog(errorDialog, "Invalid response format: instance_id is empty.");
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
                        await ShowErrorDialog(errorDialog, "Invalid response format: missing instance_id.");
                        throw new Exception("Invalid response format");
                    }
                }
                else
                {
                    // Handle API errors (e.g., 400 Bad Request, 500 Internal Server Error).
                    string? errorContent = await response.Content.ReadAsStringAsync();
                    await ShowErrorDialog(errorDialog, $"API Error ({response.StatusCode}): {errorContent}");
                    throw new Exception("API Error");
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network errors (e.g., no internet connection).
                await ShowErrorDialog(errorDialog, $"Network Error: {ex.Message}");
                throw new Exception("Network Error");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                await ShowErrorDialog(errorDialog, $"An unexpected error occurred: {ex.Message}");
                throw new Exception("Unexpected Error");
            }
        }

        public async Task<(string status, string ticketId)> PollForStatusAsync(string instanceId, ContentDialog errorDialog)
        {
            string? status = "";
            string? ticketId = "";
            string? runtimeStatus = "";

            try
            {
                while (true) // Keep polling until we get a status
                {
                    HttpResponseMessage? response = await _httpClient.GetAsync($"{_baseUrl}/checkstatus?instance_id={instanceId}");

                    if (response.IsSuccessStatusCode)
                    {
                        string? responseContent = await response.Content.ReadAsStringAsync();
                        using JsonDocument? doc = JsonDocument.Parse(responseContent);
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("status", out JsonElement statusElement))
                        {
                            status = statusElement.GetString(); // Return the status
                        }
                        if (root.TryGetProperty("ticket_id", out JsonElement ticketIdElement))
                        {
                            ticketId = ticketIdElement.GetString(); // Return the status
                        }
                        if (root.TryGetProperty("runtimeStatus", out JsonElement runtimeStatusElement))
                        {
                            runtimeStatus = runtimeStatusElement.GetString(); // Return the status
                        }

                        if (string.IsNullOrEmpty(runtimeStatus))
                        {
                            await ShowErrorDialog(errorDialog, "Invalid response format: missing status.");
                            throw new Exception("Invalid response format");
                        }


                        if (runtimeStatus == "Completed")
                        {
                            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(ticketId))
                            {
                                await ShowErrorDialog(errorDialog, "Invalid response format: missing ticket_id.");
                                throw new Exception("Invalid response format");
                            }
                            return (status, ticketId);
                        }
                    }
                    else
                    {
                        string? errorContent = await response.Content.ReadAsStringAsync();
                        await ShowErrorDialog(errorDialog, $"API Error during polling ({response.StatusCode}): {errorContent}");
                        throw new Exception("API Error during polling");
                    }

                    // Wait before polling again (e.g., 5 seconds).  Don't overload the server!
                    await Task.Delay(5000);
                }
            }
            catch (HttpRequestException ex)
            {
                await ShowErrorDialog(errorDialog, $"Network Error during polling: {ex.Message}");
                throw new Exception("Network Error during polling");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(errorDialog, $"An unexpected error occurred during polling: {ex.Message}");
                throw new Exception("Unexpected Error during polling");
            }
        }

        private static async Task ShowErrorDialog(ContentDialog errorDialog, string message)
        {
            errorDialog.Title = "Error";
            errorDialog.Content = message;
            errorDialog.CloseButtonText = "OK";

            // Make sure the dialog is shown on the UI thread.
            if (errorDialog.DispatcherQueue.HasThreadAccess)
            {
                await errorDialog.ShowAsync();
            }
            else
            {
                errorDialog.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                    async () => await errorDialog.ShowAsync());
            }
        }
    }
}