using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace QA40x_BareMetal
{
    public partial class Form1 : Form
    {
        CancellationTokenSource AcqCancellationToken = new CancellationTokenSource();

        System.Windows.Forms.Timer Timer = new Timer();
        bool LoopForever = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Usb.Open(0x16c0, 0x4e37))
            {
                Text = "Open";
            }
            else
            {
                Text = "Open Failed";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Usb.Open(0x16c0, 0x4e39))
            {
                Text = "Open";
            }
            else
            {
                Text = "Open Failed";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (Usb.Close())
            {
                Text = "Closed";
            }
            else
            {
                Text = "Close Failed";
            }
        }

        // Set Attenuators
        private void button5_Click(object sender, EventArgs e)
        {
            // If you change the input/output levels below, you will need to change the calibrations hard-coded in Acquisition.cs
            int maxInputLevel = 6;

            switch (maxInputLevel)
            {
                case 0: Usb.WriteRegister(5, 0); break;   // Atten OFF
                case 6: Usb.WriteRegister(5, 1); break;
                case 12: Usb.WriteRegister(5, 2); break;
                case 18: Usb.WriteRegister(5, 3); break;  // Atten OFF
                case 24: Usb.WriteRegister(5, 4); break;  // Ateen ON
                case 30: Usb.WriteRegister(5, 5); break;
                case 36: Usb.WriteRegister(5, 6); break;
                case 42: Usb.WriteRegister(5, 7); break;  // Atten ON
                default:
                    throw new Exception("Unexpected input level");
            }

            int maxOutputLevel = 18;
            switch (maxOutputLevel)
            {
                case -12: Usb.WriteRegister(6, 0); break;
                case -2: Usb.WriteRegister(6, 1); break;
                case 8: Usb.WriteRegister(6, 2); break;
                case 18: Usb.WriteRegister(6, 3); break;
                default: throw new Exception("Unexpected output level");
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            AcqCancellationToken.Dispose();
            AcqCancellationToken = new CancellationTokenSource();

            // Start an acquisition. Because of the await operator, the UI thread will suspend
            // execution on this code path until the call returns. However, the UI thread will 
            // remain active, meaning it can still respond to all button clicks. If you click the Send/Recv 
            // button quickly, each call will try to start another acq. But only the first will
            // succeed in starting an acq as long as the acq engine is busy.
            await Acquisition.DoStreamingAsync(AcqCancellationToken.Token);
        }


        /// <summary>
        /// Cancels an acquisition if one is in progress. To see this, increase the bufSize in DoStreaming. A 1Mbyte buffer
        /// will take about 20 seconds to stream. During this time, if you cancel the streaming, you should see the DAC activity
        /// immediately stop. You can then start another acq. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            AcqCancellationToken.Cancel();
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            groupBox1.Enabled = false;
            CancellationTokenSource cts = new CancellationTokenSource();
            LoopForever = true;

            while (LoopForever)
            {
                await Acquisition.DoStreamingAsync(cts.Token);
                await Task.Delay(10);
            }
            groupBox1.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            LoopForever = false;
        }
    }
}