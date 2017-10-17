#ifndef COLOUR_HEADER
#define COLOUR_HEADER
	
	/// <summary>
    /// Kinect Sensor for colour image
    /// </summary>
    public KinectSensor colorSensor;

    /// <summary>
    /// Bitmap variable to tie to Image Source - Will be output of Colour Object
    /// </summary>
    public WriteableBitmap outputBitmap;

    /// <summary>
    /// Array to store colour pixels from Kinect Sensor Stream
    /// </summary>
    public byte[] colorPixels;        

	/// <summary>
	/// Handles the startup initialisations for outputting the colour image
	/// </summary>
	public void startUpColour()
	{            
		// Turn on the colour stream to receive colour frames from the sensor
		colorSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

		// Creating an array to store all the individual pixels received
		colorPixels = new byte[colorSensor.ColorStream.FramePixelDataLength];

		// Formats bitmap to be outputted to screen
		//  - Pixels in RGB
		//  - Dimensions according to sensor stream 
		//  - DPI = 96.0 (Standard for Windows) 
		outputBitmap = new WriteableBitmap(colorSensor.ColorStream.FrameWidth, colorSensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

	}

	/// <summary>
	/// Prepares the output bitmap whenever a colour frame is sent to it
	/// </summary>
	/// <param name="cFrame"></param>
	public void readStream(ColorImageFrame cFrame)
	{
		// Copy data from inputted ColourImageFrame to array for pixel data
		cFrame.CopyPixelDataTo(colorPixels);

		//Write the copied data to the Bitmap to be outputted
		outputBitmap.WritePixels(
			new Int32Rect(0, 0, outputBitmap.PixelWidth, outputBitmap.PixelHeight),
			colorPixels,
			outputBitmap.PixelWidth * sizeof(int),
			0);
	}
	
#endif