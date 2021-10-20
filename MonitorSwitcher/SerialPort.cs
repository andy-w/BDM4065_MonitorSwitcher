namespace MonitorSwitcher
{
    using System.IO.Ports;
    using System.Threading;
    using System;

    public class LocalSerialPort : IMessageTransport
    {
        private SerialPort comPort;

        private static Mutex mutexComPort = new Mutex();

        public LocalSerialPort()
        {
            this.comPort = new SerialPort("COM1")
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 100
            };
        }

        public int SendMessage(byte[] msgData, out byte[] msgResponse)
        {
            try
            {
                mutexComPort.WaitOne();

                this.comPort.Open();

                this.comPort.Write(msgData, 0, msgData.Length);

                Thread.Sleep(1000);

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
