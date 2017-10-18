using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.BackgroundRemoval;



namespace ColourSkel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;
        */

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensorChooser sensorChooser;

        /// <summary>
        /// Object to handle colour image operations
        /// </summary>
        private Colour colourObj;

        private WriteableBitmap streamImg;

        /// <summary>
        /// Object to handle skeleton operations
        /// </summary>
        private SkeletonLib skelObj;

        /// <summary>
        /// Object to handle background removal operations
        /// </summary>
        private BackgroundRemovalLib backgroundObj;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
            this.sensorChooser = null;
        }

        /// <summary>
        /// Starts the Colour Reading Process
        /// </summary>
        private void InitiateColour()
        {
            //Create Colour Object with active sensor and initiate StartUp
            colourObj = new Colour(this.sensorChooser.Kinect);
            colourObj.startUpColour();

            /*
            // Tie image source to the output bitmap
            this.Image.Source = colourObj.getImage();
            */

            this.streamImg = colourObj.getImage();

            // Adds event handler for whenever a new colour frame is ready
            this.sensorChooser.Kinect.ColorFrameReady += colourObj.SensorColorFrameReady;
        }

        private void InitiateSkel()
        {
            //Create SkeltonLib Obj with active sensor
            skelObj = new SkeletonLib(this.sensorChooser.Kinect);
            skelObj.SkeletonStart();
            skelObj.setColourImage(this.streamImg);

            //Tie image source to output of object
            this.Image.Source = skelObj.getOutputImage();

            // Add an event handler to be called whenever there is new color frame data
            this.sensorChooser.Kinect.SkeletonFrameReady += skelObj.SensorSkeletonFrameReady;
        }

        private void InitiateBackgroundRemoval()
        {
            //Create BackgroundRemovalLib Obj with active sensor
            backgroundObj = new BackgroundRemovalLib(this.sensorChooser.Kinect);
            backgroundObj.BackgroundStart();

            //Tie image source to output of object
            this.Image.Source = backgroundObj.getBackgroundRemovedImage();
        }

        private void LiveMeasure()
        {
            this.measureBarText.Text = skelObj.getMeasurements();
        }

        private void ButtonMeasureClick(object sender, RoutedEventArgs e)
        {
            LiveMeasure();
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.SkeletonFrameReady -= skelObj.SensorSkeletonFrameReady;
                    args.OldSensor.ColorFrameReady -= colourObj.SensorColorFrameReady;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.ColorStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                    /*
                    // Create the background removal stream to process the data and remove background, and initialize it.
                    if (null != this.backgroundRemovedColorStream)
                    {
                        this.backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                        this.backgroundRemovedColorStream.Dispose();
                        this.backgroundRemovedColorStream = null;
                    }
                    */
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    this.InitiateColour();
                    this.InitiateSkel();
                    //this.InitiateBackgroundRemoval();

                    /*
                    this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    this.backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);

                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == this.skeletons)
                    {
                        this.skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += this.SensorAllFramesReady;
                    

                    try
                    {
                        args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    this.statusBarText.Text = Properties.Resources.ReadyForScreenshot;
                    */
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }
    }
}
