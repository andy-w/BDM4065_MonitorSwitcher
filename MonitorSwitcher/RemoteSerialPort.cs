namespace MonitorSwitcher
{
    using System.Net.Sockets;
    using System.Threading;

    public class RemoteSerialPort : IMessageTransport
    {
        private TcpClient client;
        private int remotePort;

        public RemoteSerialPort(int remotePort)
        {
            this.remotePort = remotePort;

            this.client = new TcpClient() { ReceiveTimeout = 1000 };
        }

        public int SendMessage(byte[] msgData, out byte[] msgResponse)
        {
            try
            {
                if (this.client.Connected == false)
                {
                    this.client = new TcpClient();

                    this.client.Connect("RemoteMonitor", this.remotePort);

                    this.client.ReceiveTimeout = 2000;
                }

                this.client.GetStream().Write(msgData, 0, msgData.Length);

                Thread.Sleep(2000);

                ////if (this.client.GetStream().DataAvailable)
                {
                    byte[] recvBuffer = new byte[255];

                    int noBytes = this.client.GetStream().Read(recvBuffer, 0, recvBuffer.Length);

                    if (recvBuffer[0] == 0)
                    {
                        msgResponse = new byte[noBytes - 1];

                        System.Buffer.BlockCopy(recvBuffer, 1, msgResponse, 0, noBytes- 1);

                        return 0;
                    }
                    else
                    {
                        msgResponse = null;

                        return 1;
                    }
                }

                /*else
                {
                    msgResponse = null;

                    return 1;
                }*/
            }
            catch
            {
                msgResponse = null;

                if (this.client.Connected)
                {
                    this.client.Close();

                    this.client = new TcpClient();
                }

                return 1;
            }
        }
    }
}
