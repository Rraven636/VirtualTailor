using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.BackgroundRemoval;

namespace ColourSkel
{
    class BackgroundRemovalLib
    {
        /// <summary>
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap foregroundBitmap;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public BackgroundRemovalLib()
        {
            this.sensor = null;
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public BackgroundRemovalLib(KinectSensor sensorIn, BackgroundRemovedColorStream brColorStreamIn)
        {
            this.sensor = sensorIn;
            this.backgroundRemovedColorStream = brColorStreamIn;
        }

        /// <summary>
        /// This will run only if the Dispose method does not get called.
        /// </summary>
        public void backgroundDestructor()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (null != this.backgroundRemovedColorStream)
                {
                    this.backgroundRemovedColorStream.Dispose();
                    this.backgroundRemovedColorStream = null;
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == this.sensor)
            {
                return;
            }

            try
            {
                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                    }
                }

                using (var colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                    }
                }

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (null != skeletonFrame)
                    {
                        skeletonFrame.CopySkeletonDataTo(this.skeletons);
                        this.backgroundRemovedColorStream.ProcessSkeleton(this.skeletons, skeletonFrame.Timestamp);
                    }
                }

                this.ChooseSkeleton();
            }
            catch (InvalidOperationException)
            {
                // Ignore the exception. 
            }
        }

        /// <summary>
        /// Handle the background removed color frame ready event. The frame obtained from the background removed
        /// color stream is in RGBA format.
        /// </summary>
        /// <param name="sender">object that sends the event</param>
        /// <param name="e">argument of the event</param>
        public void BackgroundRemovedFrameReadyHandler(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
            {
                if (backgroundRemovedFrame != null)
                {
                    if (null == this.foregroundBitmap || this.foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width
                        || this.foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        this.foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);

                    }

                    // Write the pixel data into our bitmap
                    this.foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                        backgroundRemovedFrame.GetRawPixelData(),
                        this.foregroundBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Use the sticky skeleton logic to choose a player that we want to set as foreground. This means if the app
        /// is tracking a player already, we keep tracking the player until it leaves the sight of the camera, 
        /// and then pick the closest player to be tracked as foreground.
        /// </summary>
        private void ChooseSkeleton()
        {
            var isTrackedSkeltonVisible = false;
            var nearestDistance = float.MaxValue;
            var nearestSkeleton = 0;

            foreach (var skel in this.skeletons)
            {
                if (null == skel)
                {
                    continue;
                }

                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == this.currentlyTrackedSkeletonId)
                {
                    isTrackedSkeltonVisible = true;
                    break;
                }

                if (skel.Position.Z < nearestDistance)
                {
                    nearestDistance = skel.Position.Z;
                    nearestSkeleton = skel.TrackingId;
                }
            }

            if (!isTrackedSkeltonVisible && nearestSkeleton != 0)
            {
                this.backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                this.currentlyTrackedSkeletonId = nearestSkeleton;
            }
        }
        
        public WriteableBitmap getBackgroundRemovedImage()
        {
            return this.foregroundBitmap;
        }

        public BackgroundRemovedColorStream getBackgroundRemovedColorStream()
        {
            return this.backgroundRemovedColorStream;
        }

        public void BackgroundStart()
        {
            try
            {
                this.sensor.DepthStream.Enable(DepthFormat);
                this.sensor.ColorStream.Enable(ColorFormat);
                this.sensor.SkeletonStream.Enable();


                this.backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);

                // Allocate space to put the depth, color, and skeleton data we'll receive
                if (null == this.skeletons)
                {
                    this.skeletons = new Skeleton[this.sensor.SkeletonStream.FrameSkeletonArrayLength];
                }

                /*
                // Add an event handler to be called when the background removed color frame is ready, so that we can
                // composite the image and output to the app
                this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                // Add an event handler to be called whenever there is new depth frame data
                this.sensor.AllFramesReady += this.SensorAllFramesReady;
                */
            }
            catch (InvalidOperationException)
            {
                // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                // E.g.: sensor might be abruptly unplugged.
            }

        }

        public void BackgroundStop(KinectSensor OldSensor)
        {
            try
            {
                OldSensor.AllFramesReady -= this.SensorAllFramesReady;
                OldSensor.DepthStream.Disable();
                OldSensor.ColorStream.Disable();
                OldSensor.SkeletonStream.Disable();

                // Create the background removal stream to process the data and remove background, and initialize it.
                if (null != this.backgroundRemovedColorStream)
                {
                    this.backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                    this.backgroundRemovedColorStream.Dispose();
                    this.backgroundRemovedColorStream = null;
                }
            }
            catch (InvalidOperationException)
            {
                // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                // E.g.: sensor might be abruptly unplugged.
            }
        }
    }

}
