using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace MonitorSwitcher
{
    class RemoteSerialPort : MessageTransport
    {
        private TcpClient client;

        public RemoteSerialPort()
        {
            this.client = new TcpClient();
        }

        public int SendMessage(byte[] msgData, out byte[] msgResponse)
        {
            try
            {
                if (client.Connected == false)
                {
                    this.client.Connect("192.168.1.202", 11000);
                }

                this.client.GetStream().Write(msgData, 0, msgData.Length);

                Thread.Sleep(200);

                if (this.client.GetStream().DataAvailable)
                {
                    byte[] recvBuffer = new byte[255];

                    int noBytes = this.client.GetStream().Read(recvBuffer, 0, recvBuffer.Length);

                    if (recvBuffer[0] == 0)
                    {
                        msgResponse = new byte[noBytes - 1];

                        System.Buffer.BlockCopy(recvBuffer, 1, msgResponse, 0, noBytes - 1);

                        return 0;
                    }
                    else
                    {
                        msgResponse = null;

                        return 1;
                    }
                }
                else
                {
                    msgResponse = null;

                    return 1;
                }
            }
            finally
            {
            }
        }
    }
}
