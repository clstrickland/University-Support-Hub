using System;
//using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Storage.Streams;
using WinRT;
//using WinRT.Interop;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
//using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;

namespace SupportHubApp
{
    [GeneratedComInterface]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    partial interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    class ScreenshotHelper(IntPtr hwnd)
    {
        private readonly IntPtr _hwnd = hwnd;
        private readonly CanvasDevice _canvasDevice = new(); // Use CanvasDevice

        private readonly Logging _logging = new() { SubModuleName = "ScreenshotHelper" };

        // No longer needed! Win2D handles this.
        // private IDirect3DDevice CreateDirect3DDevice() { ... }

        private static IDirect3DDevice Device =>
            // Get the underlying IDirect3DDevice from the CanvasDevice
            CanvasDevice.GetSharedDevice();
        private async Task<byte[]> CaptureItemAsync(GraphicsCaptureItem item, TimeSpan timeout)
        {
            _logging.LogInfo("Starting screen capture...");

            if (item == null)
            {
                _logging.LogError("GraphicsCaptureItem is null.");
                throw new ArgumentNullException(nameof(item), "GraphicsCaptureItem cannot be null.");
            }

            // Use a TaskCompletionSource with a timeout.
            var tcs = new TaskCompletionSource<byte[]>();
            using var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            Direct3D11CaptureFramePool? framePool = null;
            GraphicsCaptureSession? session = null;
            int frameCount = 0;

            try
            {
                // Use Create instead of CreateFreeThreaded. This is a KEY change.
                framePool = Direct3D11CaptureFramePool.Create(
                    Device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    1,
                    item.Size);

                session = framePool.CreateCaptureSession(item);


                framePool.FrameArrived += (s, e) =>
                {
                    try
                    {
                        frameCount++;
                        _logging.LogInfo($"Frame {frameCount} arrived.");

                        using var frame = s.TryGetNextFrame();
                        if (frame != null)
                        {
                            _logging.LogInfo($"Frame {frameCount} captured.");
                            using var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface);
                            using var stream = new InMemoryRandomAccessStream();
                            bitmap.SaveAsync(stream, CanvasBitmapFileFormat.Png).AsTask(cts.Token).Wait(cts.Token);
                            var bytes = new byte[stream.Size];
                            stream.Seek(0);
                            var buffer = bytes.AsBuffer();
                            stream.ReadAsync(buffer, (uint)bytes.Length, InputStreamOptions.None).AsTask(cts.Token).Wait(cts.Token);
                            tcs.TrySetResult(bytes); // Set result on FIRST frame.
                        }
                        else
                        {
                            _logging.LogError($"Frame {frameCount} is null.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logging.LogWarning($"Frame {frameCount} capture canceled.");
                    }
                    catch (Exception ex)
                    {
                        _logging.LogException(ex);
                        tcs.TrySetException(ex);
                    }
                };

                // Start the capture SYNCHRONOUSLY *before* waiting on the task.
                session.StartCapture();
                _logging.LogInfo("Capture session started.");


                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _logging.LogInfo($"Capture timed out after {frameCount} frames.");
                throw new TimeoutException("Screen capture timed out.", new OperationCanceledException());
            }
            catch (Exception ex)
            {
                // Log the exception for debugging.
                _logging.LogException(ex);
                tcs.TrySetException(ex); // Ensure exception is propagated.
                throw; // Re-throw the exception after logging.
            }
            finally
            {
                session?.Dispose();
                framePool?.Dispose();
            }


        }
        public async Task<byte[]> CaptureWithPickerAsync()
        {
            _logging.LogInfo("Starting screen capture with picker...");
            var picker = new GraphicsCapturePicker();
            var initializeWithWindow = picker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(_hwnd);
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            TimeSpan timeout = TimeSpan.FromSeconds(5);

            return await CaptureItemAsync(item, timeout);
        }
    }
}
