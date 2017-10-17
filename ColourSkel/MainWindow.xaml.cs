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
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        ///////////////////////// Colour Vairable/////////////////
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
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;                    
                    break;
                }
                                
            }

            if (this.sensor != null)
            {
                this.InitiateColour();
                this.InitiateSkel();
                //this.InitiateBackgroundRemoval();

                // Start the sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Starts the Colour Reading Process
        /// </summary>
        private void InitiateColour()
        {
            //Create Colour Object with active sensor and initiate StartUp
            colourObj = new Colour(this.sensor);
            colourObj.startUpColour();

            /*
            // Tie image source to the output bitmap
            this.Image.Source = colourObj.getImage();
            */

            this.streamImg = colourObj.getImage();

            // Adds event handler for whenever a new colour frame is ready
            this.sensor.ColorFrameReady += colourObj.SensorColorFrameReady;
        }

        private void InitiateSkel()
        {
            //Create SkeltonLib Obj with active sensor
            skelObj = new SkeletonLib(this.sensor);
            skelObj.SkeletonStart();
            skelObj.setColourImage(this.streamImg);

            //Tie image source to output of object
            this.Image.Source = skelObj.getOutputImage();

            // Add an event handler to be called whenever there is new color frame data
            this.sensor.SkeletonFrameReady += skelObj.SensorSkeletonFrameReady;
        }

        private void InitiateBackgroundRemoval()
        {
            //Create BackgroundRemovalLib Obj with active sensor
            backgroundObj = new BackgroundRemovalLib(this.sensor);
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
    }
}
