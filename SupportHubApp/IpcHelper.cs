using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupportHubApp
{
    public static class IpcHelper
    {
        // --- Constants ---
        public const string UniqueMutexName = "SupportHubApp_SingleInstanceMutex_v1";
        public const string UniquePipeName = "SupportHubApp_SingleInstancePipe_v1";
        private const string ActivateMessage = "Activate";
        private const string PingMessage = "Ping";
        private const int ConnectTimeoutMs = 200; // Timeout for pipe connection attempt

        // --- Static Fields ---
        private static Mutex? _mutex;

        public static bool CheckProcessRunning(bool shouldBringToForeground)
        {
            bool mutexCreated;
            try
            {
                var potentialMutex = new Mutex(true, UniqueMutexName, out mutexCreated);

                if (mutexCreated)
                {
                    _mutex = potentialMutex;
                    return false; // This is the first instance
                }
                else
                {
                    // --- Another instance is running ---
                    potentialMutex.Dispose();

                    // *** CHANGE: Call the synchronous version ***
                    TrySendSignalSync(shouldBringToForeground); // Block here until signal sent or timeout

                    return true; // Another instance is running
                }
            }
            catch (AbandonedMutexException)
            {
                Console.WriteLine("Acquired abandoned mutex. Treating as first instance.");
                try
                {
                    _mutex = new Mutex(true, UniqueMutexName, out mutexCreated);
                    return false;
                }
                catch (Exception exInner)
                {
                    Console.WriteLine($"Error acquiring mutex after abandoned state: {exInner.Message}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during mutex check: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// *** NEW: Synchronously attempts to send a signal ("Activate" or "Ping") ***
        /// Blocks until the connection is made/fails or the write completes.
        /// </summary>
        /// <param name="shouldActivate">Determines the message to send.</param>
        private static void TrySendSignalSync(bool shouldActivate) // Changed signature: void, not Task
        {
            try
            {
                using var client = new NamedPipeClientStream(".", UniquePipeName, PipeDirection.Out);

                // *** CHANGE: Use synchronous Connect ***
                client.Connect(ConnectTimeoutMs); // Blocks up to timeout

                // No need to check IsConnected, Connect throws TimeoutException on failure
                string message = shouldActivate ? ActivateMessage : PingMessage;
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                // *** CHANGE: Use synchronous Write ***
                client.Write(messageBytes, 0, messageBytes.Length); // Blocks until written

                // *** CHANGE: Use synchronous Flush ***
                client.Flush(); // Ensure data is sent through the pipe buffer

                Console.WriteLine($"Sent '{message}' signal synchronously to existing instance.");

            }
            catch (TimeoutException)
            {
                // Expected if the server (first instance) isn't listening or ready within the timeout.
                Console.WriteLine($"Timeout connecting synchronously to pipe '{UniquePipeName}'.");
            }
            catch (Exception ex)
            {
                // Log or handle other unexpected errors during pipe communication.
                Console.WriteLine($"Error sending signal synchronously via pipe: {ex.Message}");
            }
            // No await, method completes here
        }


        /// <summary>
        /// Starts listening for activation signals on a named pipe (remains asynchronous).
        /// </summary>
        public static Task StartActivationListener(Action activationCallback, CancellationToken cancellationToken)
        {
            if (_mutex == null)
            {
                throw new InvalidOperationException("Cannot start listener: This instance does not hold the single-instance mutex.");
            }

            // Listener loop remains asynchronous using Task.Run
            return Task.Run(async () =>
            {
                NamedPipeServerStream? server = null;
                using var ctr = cancellationToken.Register(() =>
                {
                    Console.WriteLine("Cancellation requested. Releasing resources.");
                    try { server?.Dispose(); } catch (Exception ex) { Console.WriteLine($"Error disposing pipe server on cancel: {ex.Message}"); }
                    ReleaseMutex();
                });

                Console.WriteLine($"Starting activation listener on pipe '{UniquePipeName}'...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        server = new NamedPipeServerStream(
                            UniquePipeName, PipeDirection.In, 1,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous); // Still async for server

                        // Server still waits asynchronously
                        await server.WaitForConnectionAsync(cancellationToken);
                        if (cancellationToken.IsCancellationRequested) break;

                        Console.WriteLine("Client connected.");
                        string message;
                        using (var reader = new StreamReader(server, Encoding.UTF8, true, 1024, leaveOpen: true))
                        {
                            // Reading remains async
                            message = await reader.ReadToEndAsync(cancellationToken);
                        }

                        Console.WriteLine($"Received message: {message}");
                        if (message == ActivateMessage)
                        {
                            activationCallback?.Invoke();
                        }

                        server.Disconnect();
                        server.Dispose();
                        server = null;
                    }
                    // Exception handling (same as before)
                    catch (OperationCanceledException) { Console.WriteLine("Pipe listener operation cancelled."); break; }
                    catch (IOException ex) when (ex.Message.Contains("Pipe is broken"))
                    { Console.WriteLine($"Client disconnected abruptly: {ex.Message}"); server?.Dispose(); server = null; }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in pipe listener: {ex.GetType().Name} - {ex.Message}");
                        server?.Dispose(); server = null;
                        try { await Task.Delay(1000, cancellationToken); }
                        catch (OperationCanceledException) { break; }
                    }
                    finally
                    {
                        if (server != null) { try { server.Dispose(); } catch { /* Ignore */ } server = null; }
                    }
                }
                Console.WriteLine("Activation listener stopped.");
            }, cancellationToken);
        }

        public static void ReleaseMutex()
        {
            // Logic remains the same
            if (_mutex == null) return;
            try { _mutex.ReleaseMutex(); Console.WriteLine("Mutex released."); }
            catch (ApplicationException ex) { Console.WriteLine($"Attempted to release mutex not owned: {ex.Message}"); }
            catch (ObjectDisposedException) { /* Ignore */ }
            catch (Exception ex) { Console.WriteLine($"Unexpected error releasing mutex: {ex.Message}"); }
            finally
            {
                try { _mutex?.Dispose(); } catch (Exception ex) { Console.WriteLine($"Error disposing mutex: {ex.Message}"); }
                _mutex = null;
            }
        }
    }
}