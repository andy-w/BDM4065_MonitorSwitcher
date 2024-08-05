namespace MonitorSwitcher
{
    using System.IO.Ports;
    using System.Threading;
    using System;

    public class LocalSerialPort : IMessageTransport
    {
        private SerialPort comPort;

        private static Mutex mutexComPort = new Mutex();

        public LocalSerialPort(string serialPort)
        {
            this.comPort = new SerialPort(serialPort)
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 2000
            };
        }

        public int SendMessage(byte[] msgData, out byte[] msgResponse)
        {
            try
            {
                mutexComPort.WaitOne();

                this.comPort.Open();

                this.comPort.Write(msgData, 0, msgData.Length);

                Thread.Sleep(2000);

                if (this.comPort.BytesToRead > 0)
                {
                    msgResponse = new byte[this.comPort.BytesToRead];

                    this.comPort.Read(msgResponse, 0, this.comPort.BytesToRead);

                    return 0;
                }
                else
                {
                    msgResponse = null;

                    return 1;
                }
            }
            catch (Exception)
            {
                msgResponse = null;

                return 2;
            }
            finally
            {
                this.comPort.Close();

                mutexComPort.ReleaseMutex();
            }
        }
    }
}
