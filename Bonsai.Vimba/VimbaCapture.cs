using AVT.VmbAPINET;
using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bonsai.Vimba
{
    [Description("Acquires a sequence of images from a camera using the Vimba SDK.")]
    public class VimbaCapture : Source<VimbaDataFrame>
    {
        static readonly object systemLock = new object();

        [Description("The optional index of the camera from which to acquire images.")]
        public int? Index { get; set; }

        [TypeConverter(typeof(SerialNumberConverter))]
        [Description("The optional serial number of the camera from which to acquire images.")]
        public string SerialNumber { get; set; }

        [Description("Specifies the optional number of frames to allocate for continuous acquisition.")]
        public int? FrameCount { get; set; }

        static unsafe Func<Frame, IplImage> GetConverter(VmbPixelFormatType pixelFormat)
        {
            int outputChannels;
            IplDepth outputDepth;
            ColorConversion? colorConversion = default;
            switch (pixelFormat)
            {
                case VmbPixelFormatType.VmbPixelFormatMono8:
                    outputChannels = 1;
                    outputDepth = IplDepth.U8;
                    break;
                case VmbPixelFormatType.VmbPixelFormatBgr8:
                case VmbPixelFormatType.VmbPixelFormatRgb8:
                    outputChannels = 3;
                    outputDepth = IplDepth.U8;
                    if (pixelFormat == VmbPixelFormatType.VmbPixelFormatRgb8)
                    {
                        colorConversion = ColorConversion.Rgb2Bgr;
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Unable to convert pixel format {0}.", pixelFormat));
            }

            return frame =>
            {
                var imageSize = new Size((int)frame.Width, (int)frame.Height);
                var imageFormat = frame.PixelFormat;
                fixed (byte* buffer = frame.Buffer)
                {
                    var bufferHeader = new IplImage(imageSize, outputDepth, outputChannels, (IntPtr)buffer);
                    var output = new IplImage(imageSize, outputDepth, outputChannels);
                    if (colorConversion.HasValue) CV.CvtColor(bufferHeader, output, colorConversion.Value);
                    else CV.Copy(bufferHeader, output);
                    return output;
                }
            };
        }

        public override IObservable<VimbaDataFrame> Generate()
        {
            return Generate(Observable.Return(Unit.Default));
        }

        public IObservable<VimbaDataFrame> Generate<TSource>(IObservable<TSource> start)
        {
            return Observable.Create<VimbaDataFrame>((observer, cancellationToken) =>
            {
                var serialNumber = SerialNumber;
                var index = Index.GetValueOrDefault(0);
                var frameCount = FrameCount.GetValueOrDefault(3);
                return Task.Factory.StartNew(async () =>
                {
                    Camera camera;
                    try
                    {
                        lock (systemLock)
                        {
                            VimbaApi.Handle.Startup();

                            var cameraList = VimbaApi.Handle.Cameras;
                            if (!string.IsNullOrEmpty(serialNumber))
                            {
                                camera = null;
                                for (int i = 0; i < cameraList.Count; i++)
                                {
                                    if (cameraList[i].SerialNumber == serialNumber)
                                    {
                                        camera = cameraList[i];
                                    }
                                }

                                if (camera == null)
                                {
                                    var message = string.Format("No Vimba camera was found with serial number {0}.", serialNumber);
                                    throw new InvalidOperationException(message);
                                }
                            }
                            else
                            {
                                if (index < 0 || index >= cameraList.Count)
                                {
                                    var message = string.Format("No Vimba camera was found at index {0}.", index);
                                    throw new InvalidOperationException(message);
                                }

                                camera = cameraList[index];
                            }
                        }

                        var imageFormat = default(VmbPixelFormatType);
                        var converter = default(Func<Frame, IplImage>);
                        using var waitHandle = new ManualResetEvent(false);
                        using var notification = cancellationToken.Register(() => waitHandle.Set());
                        camera.Open(VmbAccessModeType.VmbAccessModeFull);
                        try
                        {
                            camera.OnFrameReceived += frame =>
                            {
                                try
                                {
                                    if (frame.ReceiveStatus == VmbFrameStatusType.VmbFrameStatusComplete)
                                    {
                                        if (converter == null || frame.PixelFormat != imageFormat)
                                        {
                                            converter = GetConverter(frame.PixelFormat);
                                            imageFormat = frame.PixelFormat;
                                        }

                                        var output = converter(frame);
                                        observer.OnNext(new VimbaDataFrame(output, frame.FrameID, frame.Timestamp));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    observer.OnError(ex);
                                    waitHandle.Set();
                                    throw;
                                }
                                finally
                                {
                                    try { camera.QueueFrame(frame); }
                                    catch (VimbaException)
                                    {
                                        if (camera.PermittedAccess != VmbAccessModeType.VmbAccessModeNone)
                                        {
                                            throw;
                                        }
                                    }
                                }
                            };

                            await start;
                            camera.StartContinuousImageAcquisition(frameCount);
                            waitHandle.WaitOne();
                            camera.StopContinuousImageAcquisition();
                        }
                        finally { camera.Close(); }
                    }
                    catch (Exception ex) { observer.OnError(ex); throw; }
                    finally
                    {
                        lock (systemLock)
                        {
                            VimbaApi.Handle.Shutdown();
                        }
                    }
                });
            });
        }
    }
}
