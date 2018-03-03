using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloEmguWorkshop
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private VideoCapture _capture;
        private Thread _captureThread;

        // COM port may need to be changed, depending on how it is plugged in
        SerialPort _serialPort = new SerialPort("COM3", 2400);

        // LoCoMoCo Commands
        const byte STOP = 0x7F;
        const byte FLOAT = 0x0F;
        const byte FORWARD = 0x6f;
        const byte BACKWARD = 0x5F;

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set up serial communications
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.Two;
            _serialPort.Open();

            // Set up webcam capture
            _capture = new VideoCapture();

            // Start thread for vision
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();
        }


        // Process webcam data in separate thread
        private void DisplayWebcam()
        {
            while (_capture.IsOpened)
            {
                // Get one frame from the webcam
                Mat frame = _capture.QueryFrame();
                CvInvoke.Resize(frame, frame, pictureBox1.Size);

                // Mat to BGR Image
                Image<Bgr, Byte> img = frame.ToImage<Bgr, Byte>();

                // BGR Image to Gray Image
                Image<Gray, Byte> img2 = img.Convert<Gray, Byte>();

                // Threshold Gray Image
                img2 = img2.ThresholdBinary(new Gray(threshold), new Gray(255));

                // Get number of white pixels in each third
                int leftWhiteCount, centerWhiteCount, rightWhiteCount;

                img2.ROI = Rectangle.Empty; // Reset ROI

                // Left Third
                img2.ROI = new Rectangle(0, 0, img2.Width / 3, img2.Height);
                leftWhiteCount = img2.CountNonzero()[0];

                img2.ROI = Rectangle.Empty; // Reset ROI

                // Center Third
                img2.ROI = new Rectangle(img2.Width / 3, 0, img2.Width / 3, img2.Height);
                centerWhiteCount = img2.CountNonzero()[0];
                
                img2.ROI = Rectangle.Empty; // Reset ROI

                // Right Third
                img2.ROI = new Rectangle(2 * img2.Width / 3, 0, img2.Width / 3, img2.Height);
                rightWhiteCount = img2.CountNonzero()[0];
                img2.ROI = Rectangle.Empty; // Reset ROI

                // Turn torwards line
                if (rightWhiteCount > centerWhiteCount)
                {
                    byte left = FORWARD;
                    byte right = BACKWARD;

                    byte[] buffer = { 0x01, left, right };
                    _serialPort.Write(buffer, 0, 3);

                }
                else if (leftWhiteCount > centerWhiteCount)
                {
                    byte left = BACKWARD;
                    byte right = FORWARD;

                    byte[] buffer = { 0x01, left, right };
                    _serialPort.Write(buffer, 0, 3);
                }
                else
                {
                    byte left = FORWARD;
                    byte right = FORWARD;

                    byte[] buffer = {0x01, left, right};
                    _serialPort.Write(buffer, 0, 3);
                }

                // Display Gray Image
                pictureBox1.Image = img2.ToBitmap();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // End thread and close port to allow program to exit
            _captureThread.Abort();
            _serialPort.Close();
        }

        /// <summary>
        /// Threshold used to distinguish line from background
        /// </summary>
        private int threshold = 0;

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // Set threshold to value from slider
            threshold = trackBar1.Value;
        }
    }
}
