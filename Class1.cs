using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using Microsoft.Psi;
using Microsoft.Psi.Kinect;
using Microsoft.Azure.Kinect;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.MixedReality;
using Microsoft.Psi.Remoting;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using StereoKit;
using System.Text;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi.Calibration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect;

//(0-9, A-F)
namespace TestCSspace
{
    [Guid("12345678-90AB-CDEF-1234-567890ABCDEF")]
    public interface IInterface
    {
        void Connect();

        void Disconnect();
    }
}

namespace TestCSspace
{
    [Guid("FEDCBA98-7654-3210-FEDC-BA9876543210")]
    public class ClassYouWantToUse : IInterface
    {
        private bool connected;

        public void Connect()
        {
            Console.WriteLine("c# connect");
        }

        public void Disconnect()
        {
            Console.WriteLine("c# disconnect");
        }
    }
}

namespace PSiCSspace
{
    [Guid("12345678-90AB-CDEF-1234-567890ABCDEE")]
    public interface IInterface
    {

        void Connect();

        void Disconnect();

        void initDepthCamera();

        int getImagedata(out IntPtr imageData, out int size);
    }
}

namespace PSiCSspace
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("FEDCBA98-7654-3210-FEDC-BA9876543212")]
    public class PsiWrapper: IInterface
    {
        private readonly object lockObject = new object();
        private byte[] imagedata = null;
        public void initDepthCamera()
        {
            Random rnd = new Random();
            Pipeline pipeline = Pipeline.Create();

            Microsoft.Psi.Imaging.DepthImageFromStreamDecoder depthstreamDecoder = new DepthImageFromStreamDecoder();
            Microsoft.Psi.Imaging.DepthImageDecoder depthImageDecoder = new DepthImageDecoder(pipeline, depthstreamDecoder);
            Microsoft.Psi.Imaging.ImageFromNV12StreamDecoder streamDecoder = new ImageFromNV12StreamDecoder();
            Microsoft.Psi.Imaging.ImageDecoder imagedecoder = new ImageDecoder(pipeline, streamDecoder);

            String host = "206.12.165.224";
            int port = 12345;
            var receiver = new RemoteImporter(pipeline, host, port, true);

            Console.WriteLine("Waiting for sender connection >>>");
            receiver.Connected.WaitOne();
            Console.WriteLine("Sender connected! " + receiver.Importer.Name);

            var azureKinectBodyTrackerCalibration = new Calibration();


            //var incomingVideo = receiver.Importer.OpenStreamOrDefault<Shared<EncodedImage>>("PhotoCameraStream");
            var incomingVideo = receiver.Importer.OpenStreamOrDefault<Shared<EncodedImage>>("PhotoCameraStream");
            while (incomingVideo == null) {
                Thread.Sleep(2000);
                //incomingVideo = receiver.Importer.OpenStreamOrDefault<Shared<EncodedImage>>("PhotoCameraStream");
                incomingVideo = receiver.Importer.OpenStreamOrDefault<Shared<EncodedImage>>("PhotoCameraStream");
            }
            Console.WriteLine("Image Stream exist");

            /*
            var incomingCallibration = receiver.Importer.OpenStreamOrDefault<Calibration>("CameraCallibration");
            while (incomingCallibration == null)
            {
                Thread.Sleep(2000);
                //incomingVideo = receiver.Importer.OpenStreamOrDefault<Shared<EncodedImage>>("PhotoCameraStream");
                incomingCallibration = receiver.Importer.OpenStreamOrDefault<Calibration>("CameraCallibration");
            }
            Console.WriteLine("Image Callibration exist");
            */

            var incomingdepthVideo = receiver.Importer.OpenStreamOrDefault<Shared<DepthImage>>("DepthCameraStream");
            while (incomingdepthVideo == null)
            {
                Thread.Sleep(2000);
                incomingdepthVideo = receiver.Importer.OpenStreamOrDefault<Shared<DepthImage>>("DepthCameraStream");
                //Console.WriteLine("Stream does not exist");
            }
            Console.WriteLine("Depth Stream exist");
            //incomingStream.Do(value => Console.WriteLine($"Received: {value}"));

            incomingVideo.Out.Do(image =>
            {
                Console.WriteLine("Received an image frame at " + DateTime.Now.ToString());
            });

            incomingVideo.PipeTo(imagedecoder.In);
            //incomingdepthVideo.PipeTo(depthImageDecoder.In);


            incomingdepthVideo.Out.Do(async image => {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + rnd.Next(1, 1000).ToString();
                    string filePath = $"C:/mirthus_humanteleop/mirthus_master-wifi/tmp_videos/captureddepth_{timestamp}.jpg";

                    using (var bitmap = new Bitmap(image.Resource.Width, image.Resource.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                        IntPtr ptr = bmpData.Scan0;

                        int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
                        byte[] rgbValues = new byte[image.Resource.Size];

                        image.Resource.CopyTo(rgbValues);
                        //Marshal.Copy(image.Resource.ImageData, rgbValues, 0, bytes);
                        Marshal.Copy(rgbValues, 0, ptr, image.Resource.Size);

                        bitmap.UnlockBits(bmpData);

                        //bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        //Console.WriteLine("depthImage saved to " + filePath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to save image: {e.Message}");
                }
            });

            imagedecoder.Out.Do(async image => {
                try
                {
                    Console.WriteLine("Received an image frame decoded at " + DateTime.Now.ToString());
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + rnd.Next(1, 1000).ToString();
                    string filePath = $"C:/mirthus_humanteleop/mirthus_master-wifi/tmp_videos/captured_{timestamp}.jpg";

                    using (var bitmap = new Bitmap(image.Resource.Width, image.Resource.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                        IntPtr ptr = bmpData.Scan0;

                        int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
                        byte[] rgbValues = new byte[bytes];

                        Marshal.Copy(image.Resource.ImageData, rgbValues, 0, bytes);
                        Marshal.Copy(rgbValues, 0, ptr, bytes);

                        bitmap.UnlockBits(bmpData);

                        //bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        // Console.WriteLine("Image saved to " + filePath);
                        
                        lock (lockObject)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                                //Console.WriteLine("Image saved: " + memoryStream.Length);
                                imagedata = memoryStream.ToArray();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to save image: {e.Message}");
                }
            });



            // StringBuilder sb = new StringBuilder();
            // SensorOrientation lastOrientation = (SensorOrientation)(-1);

            var bodyTracker = new AzureKinectBodyTracker(
                 pipeline,
                 new AzureKinectBodyTrackerConfiguration()
                 {
                     TemporalSmoothing = 0.5f,
                     // UseLiteModel = true,
                 });
            
            incomingdepthVideo.Join(imagedecoder).PipeTo(bodyTracker);
            //azureKinectBodyTrackerCalibration.PipeTo(bodyTracker.AzureKinectSensorCalibration);

            //var calibration = new Calibration();
            /*
            bodyTracker.Do(image =>
            {
                Console.WriteLine($"Body tracker receive input");
            });
            */

            bodyTracker.Out.Do(bodies =>
            {
                Console.WriteLine($"Bodies: {bodies.Count}");
                bodies.ForEach(body =>
                {
                    Console.WriteLine($"Joints: {body.Joints.Count}");
                    body.Joints.ToList().ForEach(joint =>
                    {
                        Console.WriteLine($"Joint key: {joint.Key} Joint value: {joint.Value}");
                    });
                });
            });
            

            Task.Run(() =>
            {
                pipeline.RunAsync();
            });
        }
        
        public int getImagedata(out IntPtr imageData, out int size)
        {
            //lock (lockObject)
           // {
            //    try
            //    {
                    if (imagedata == null)
                    {
                        imageData = IntPtr.Zero;
                        size = 0;
                        return 1;
                    }
                    else
                    {
                        size = imagedata.Length;
                        imageData = Marshal.AllocHGlobal(size);
                        Marshal.Copy(imagedata, 0, imageData, size);
                        return 0;
                    }
                    /*
                }
                catch (Exception ex)
                {
                    imageData = IntPtr.Zero;
                    size = 0;
                    Console.WriteLine("Error in getImagedata: " + ex.Message);
                    return -1; // Error code indicating exception
                }
                    */
            //}
        }

        public void Connect()
        {
            Console.WriteLine("c# connect");
        }

        public void Disconnect()
        {
            Console.WriteLine("c# disconnect");
        }
    }
}