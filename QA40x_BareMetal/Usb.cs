using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QA40x_BareMetal
{
    /// <summary>
    /// A simple class to help wrap async transfers
    /// </summary>
    class AsyncResult
    {
        /// <summary>
        /// This is specific to the underlying library. LIBUSBDOTNET uses UsbTransfer. 
        /// WinUsbDotNet uses IAsyncResult.
        /// </summary>
        UsbTransfer UsbXfer;

        /// <summary>
        /// Buffer of data to be received.
        /// </summary>
        public byte[] ReadBuffer;

        /// <summary>
        /// This will change depending on lib used.
        /// </summary>
        /// <param name="usb"></param>
        public AsyncResult(UsbTransfer usb)
        {
            UsbXfer = usb;
        }

        /// <summary>
        /// This will change depending on lib used.
        /// </summary>
        /// <param name="usb"></param>
        public AsyncResult(UsbTransfer usb, byte[] readBuffer) : this(usb)
        {
            ReadBuffer = readBuffer;
        }

        /// <summary>
        /// Waits until the data associated with this USB object has been read from 
        /// or written to, or timed out
        /// </summary>
        /// <returns></returns>
        public int Wait()
        {
            UsbXfer.Wait(out int transferred);
            return transferred;
        }
    }

    class Usb
    {
        static UsbDevice QaAnalyzer;

        static UsbEndpointWriter RegWrite;
        static UsbEndpointReader RegRead;
        static UsbEndpointWriter DataWrite;
        static UsbEndpointReader DataRead;

        static UsbDeviceFinder UsbFinder;

        static object ReadRegLock = new object();

        static List<AsyncResult> WriteQueue = new List<AsyncResult>();
        static List<AsyncResult> ReadQueue = new List<AsyncResult>();

        static readonly int RegReadWriteTimeout = 20;
        static readonly int MainI2SReadWriteTimeout = 1000;

        public static bool Open(int vid, int pid)
        {
            Random r = new Random();
            try
            {
                UsbFinder = new UsbDeviceFinder(vid, pid);
                QaAnalyzer = UsbDevice.OpenUsbDevice(UsbFinder);

                // See if it was opened
                if (QaAnalyzer == null)
                {
                    throw new Exception("Usb.Open(): Device Not Found.");
                }

                // See https://github.com/LibUsbDotNet/LibUsbDotNet/blob/master/src/Examples/Read.Write.Async/ReadWriteAsync.cs
                IUsbDevice wholeUsbDevice = QaAnalyzer as IUsbDevice;
                if ((wholeUsbDevice is null) == false)
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                RegWrite = QaAnalyzer.OpenEndpointWriter(WriteEndpointID.Ep01);
                RegRead = QaAnalyzer.OpenEndpointReader(ReadEndpointID.Ep01);

                DataWrite = QaAnalyzer.OpenEndpointWriter(WriteEndpointID.Ep02);       
                DataRead = QaAnalyzer.OpenEndpointReader(ReadEndpointID.Ep02);

                try
                {
                    if (VerifyConnection())
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                   
                }

                return false;
            }
            catch (Exception ex)
            {
                //Log.WriteLine("Exception in Usb.Open: " + ex.Message);
            }

            return false;
        }

        static public bool Close()
        {
            try
            {
                if (QaAnalyzer != null)
                {
                    if (QaAnalyzer.IsOpen)
                    {
                        // See https://github.com/LibUsbDotNet/LibUsbDotNet/blob/master/src/Examples/Read.Write.Async/ReadWriteAsync.cs
                        IUsbDevice wholeUsbDevice = QaAnalyzer as IUsbDevice;
                        if ((wholeUsbDevice is null) == false)
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }

                        RegWrite.Dispose();
                        RegRead.Dispose();
                        DataWrite.Dispose();
                        DataRead.Dispose();
                        QaAnalyzer.Close();
                    }



                    QaAnalyzer = null;
                }

                // Needed for linux, harmless for win
                UsbDevice.Exit();

                return true;
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        /// <summary>
        /// Generates a random number, writes that to register 0, and attempts to read that same value back.
        /// </summary>
        /// <returns></returns>
        static public bool VerifyConnection()
        {
            uint val;

            unchecked
            {
                val = Convert.ToUInt32(new Random().Next());
            }

            Usb.WriteRegister(0, val);

            if (Usb.ReadRegister(0) == val)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Performs a read on a USB register
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        static public UInt32 ReadRegister(byte reg)
        {
            byte[] data = new byte[4];
            UInt32 val;

            // Lock so reads (two step USB operation) can't be broken up by writes (single step USB operation)
            lock (ReadRegLock)
            {
                try
                {
                    byte[] txBuf = WriteRegisterPrep((byte)(0x80 + reg), 0);
                    WriteRegisterRaw(txBuf);
                    RegRead.Read(data, RegReadWriteTimeout, out int len);

                    if (len == 0)
                        throw new Exception($"Usb.ReadRegister failed to read data. Register: {reg}");

                    val = (UInt32)((data[0] << 24) + (data[1] << 16) + (data[2] << 8) + data[3]);

                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return val;
        }

        /// <summary>
        /// Writes to a USB register
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="val"></param>
        static public void WriteRegister(byte reg, uint val)
        {
            // Values greater than or equal to 0x80 signify a read. Not allowed here
            if (reg >= 0x80)
            {
                throw new Exception("Usb.WriteRegister(): Invalid register");
            }

            byte[] buf = WriteRegisterPrep(reg, val);

            lock (ReadRegLock)
            {
                WriteRegisterRaw(buf);
            }
        }

        static void WriteRegisterRaw(byte[] data)
        {
            int len = data.Length;
            try
            {
                RegWrite.Write(data, RegReadWriteTimeout, out len);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        static byte[] WriteRegisterPrep(byte reg, uint val)
        {
            byte[] array = val.ToBigEndianBytes();

            byte[] r = new byte[5];

            r[0] = reg;
            Array.Copy(array, 0, r, 1, 4);
            return r;
        }

        //
        // METHODS BELOW MUST BE CALLED FROM A SINGLE THREAD. The code below is simply a wrapper for overlapped IO
        //

        static public void InitOverlapped()
        {
            WriteQueue.Clear();
            ReadQueue.Clear();
        }

        /// <summary>
        /// Submits a buffer to be written and returns immediately
        /// </summary>
        /// <param name="data"></param>
        static public void WriteDataBegin(byte[] data, int offset, int len)
        {
            ErrorCode ec;

            if (len == 0)
                return;

            byte[] localBuf = new byte[len];
            Array.Copy(data, offset, localBuf, 0, len);
            ec = DataWrite.SubmitAsyncTransfer(localBuf, 0, localBuf.Length, MainI2SReadWriteTimeout, out UsbTransfer ar);
            if (ec != ErrorCode.None)
            {
                //Log.WriteLine(LogType.Error, "Error code in Usb.WriteDataBegin: ");
                throw new Exception("Bad result in WriteDataBegin in Usb.cs");
            }
            WriteQueue.Add(new AsyncResult(ar));
        }

        /// <summary>
        /// Waits until the oldest submitted buffers has been written successfully OR timed out.
        /// The number of bytes written is returned.
        /// </summary>
        /// <returns></returns>
        static public int WriteDataEnd()
        {
            if (WriteQueue.Count == 0)
                throw new Exception("No buffers in Usb WriteDataEnd()");

            AsyncResult ar = WriteQueue[0];
            WriteQueue.RemoveAt(0);
            return ar.Wait();
        }

        /// <summary>
        /// Creates and submits a buffer to be read asynchronously. Returns immediately.
        /// </summary>
        /// <param name="data"></param>
        static public void ReadDataBegin(int bufSize)
        {
            byte[] readBuffer = new byte[bufSize];
            DataRead.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out UsbTransfer ar);
            ReadQueue.Add(new AsyncResult(ar, readBuffer));
        }

        /// <summary>
        /// Waits until the oldest submitted buffer has been read successfully OR timed out
        /// </summary>
        /// <returns></returns>
        static public byte[] ReadDataEnd()
        {
            AsyncResult ar = ReadQueue[0];
            ReadQueue.RemoveAt(0);
            if (ar.Wait() == 0)
            {
                return null;
            }
            else
            {
                return ar.ReadBuffer;
            }
        }

    }
}
