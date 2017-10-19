﻿using System;
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
        private KinectSensorChooser sensorChooser;

        /// <summary>
        /// Object to handle colour image operations
        /// </summary>
        private Colour colourObj;

        /// <summary>
        /// Variable to hold colour image that will be sent to SkeletonLib for skeleton mapping
        /// </summary>
        private WriteableBitmap streamImg;

        /// <summary>
        /// Object to handle skeleton operations
        /// </summary>
        private SkeletonLib skelObj;

        /// <summary>
        /// Object to handle background removal operations
        /// </summary>
        private BackgroundRemovalLib backgroundObj;

        /// <summary>
        /// Core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();
        }

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            if (this.backgroundObj != null)
            {
                backgroundObj.backgroundDestructor();
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
            this.sensorChooser = null;
        }

        /// <summary>
        /// Starts the Colour Reading Process
        /// </summary>
        private void InitiateColour(KinectSensor newSensor)
        {
            //Create Colour Object with active sensor and initiate StartUp
            colourObj = new Colour(newSensor);
            colourObj.startUpColour();

            //Set the global variable to hold the RGB image from the Colour Object
            this.streamImg = colourObj.getImage();

            // Adds event handler for whenever a new colour frame is ready
            newSensor.ColorFrameReady += colourObj.SensorColorFrameReady;
        }

        /// <summary>
        /// Starts the skeleton reading process and sends the colour image to be used for skeleton mapping
        /// </summary>
        private void InitiateSkel(KinectSensor newSensor)
        {
            //Create SkeltonLib Obj with active sensor
            skelObj = new SkeletonLib(newSensor);
            skelObj.SkeletonStart();
            skelObj.setColourImage(this.streamImg);

            //Tie image source to output of object
            this.Image.Source = skelObj.getOutputImage();

            // Add an event handler to be called whenever there is new skeleton frame data
            newSensor.SkeletonFrameReady += skelObj.SensorSkeletonFrameReady;
        }

        /// <summary>
        /// Starts the background removal process
        /// </summary>
        private void InitiateBackgroundRemoval(KinectSensor newSensor)
        {
            //Create BackgroundRemovalLib Obj with active sensor
            this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(newSensor);
            backgroundObj = new BackgroundRemovalLib(newSensor, this.backgroundRemovedColorStream);
            backgroundObj.BackgroundStart();

            //Tie image source to output of object
            this.Image.Source = backgroundObj.getBackgroundRemovedImage();

            //this.backgroundRemovedColorStream = backgroundObj.getBackgroundRemovedColorStream();

            // Add an event handler to be called when the background removed color frame is ready, so that we can
            // composite the image and output to the app
            this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += backgroundObj.BackgroundRemovedFrameReadyHandler;

            // Add an event handler to be called whenever there is new depth frame data
            newSensor.AllFramesReady += backgroundObj.SensorAllFramesReady;
        }

        /// <summary>
        /// Disables the colour streaming functionality
        /// </summary>
        /// <param name="OldSensor"></param>
        private void DisableColour(KinectSensor OldSensor)
        {
            OldSensor.ColorFrameReady -= colourObj.SensorColorFrameReady;
            OldSensor.ColorStream.Disable();
            colourObj = null;
        }

        /// <summary>
        /// Disables the skeleton tracking
        /// </summary>
        /// <param name="OldSensor"></param>
        private void DisableSkel(KinectSensor OldSensor)
        {
            OldSensor.SkeletonFrameReady -= skelObj.SensorSkeletonFrameReady;
            OldSensor.SkeletonStream.Disable();
            skelObj = null;
        }

        private void DisableBackgroundRemoval(KinectSensor OldSensor)
        {
            if (backgroundObj != null)
            {
                backgroundObj.BackgroundStop(OldSensor);
                backgroundObj = null;
            }

        }

        /// <summary>
        /// Changes the status bar text to the measurements from the skeleton object
        /// </summary>
        private void LiveMeasure()
        {
            this.measureBarText.Text = skelObj.getMeasurements();
        }

        /// <summary>
        /// Event handler for when the Measure Button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    //DisableColour(args.OldSensor);
                    //DisableSkel(args.OldSensor);
                    DisableBackgroundRemoval(args.OldSensor);
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
                    //this.InitiateColour(args.NewSensor);
                    //this.InitiateSkel(args.NewSensor);
                    this.InitiateBackgroundRemoval(args.NewSensor);                    
                }
                catch (InvalidOperationException ex)
                {
                    measureBarText.Text = ex.HelpLink;
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }
    }
}
