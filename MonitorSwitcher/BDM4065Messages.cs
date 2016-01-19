using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorSwitcher
{
    class BDM4065Messages
    {
        private MessageTransport msgTransport;

        private byte[] msgHeader = new byte[] { 0xA6, 0x01, 0x00, 0x00, 0x00 };

        private enum MessageSet : byte
        {
            SerialCodeGet = 0x15,
            PowerStateSet = 0x18,
            PowerStateGet = 0x19,
            TemperatureSensorGet = 0x2F,
            VideoParametersSet = 0x32,
            VideoParametersGet = 0x33,
            PictureFormatSet = 0x3A,
            PictureFormatGet = 0x3B,
            PictureInPictureSet = 0x3C,
            VolumeSet = 0x44,
            VolumeGet = 0x45,
            PictureInPictureSourceGet = 0x85,
            InputSourceSet = 0xAC,
            CurrentSourceGet = 0xAD,
        }

        public enum InputSourceType : byte
        {
            Video = 0x01,
            SVideo = 0x02,
            DVDHD = 0x03,
            RGBHV = 0x04,
            VGA = 0x05,
            HDMI = 0x06,
            DVI = 0x07,
            CardOPS = 0x08,
            DisplayPort = 0x09
        }

        public enum InputSourceNumber : byte
        {
            VGA = 0x00,
            DVI = 0x01,
            HDMI = 0x02,
            MHLHDMI2 = 0x03,
            DP = 0x04,
            miniDP = 0x05,
        }

        public enum PowerState : byte
        {
            Off = 0x01,
            On = 0x02,
        }

        public BDM4065Messages(MessageTransport msgTransport)
        {
            this.msgTransport = msgTransport;
        }

        public PowerState GetPowerState()
        {
            byte[] msgData = this.BuildMessage(new byte[] { 0x03, 
                0x01, 
                (byte)MessageSet.PowerStateGet, 
                0x00});

            byte[] msgResponse;

            if (this.msgTransport.SendMessage(msgData, out msgResponse) == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return (PowerState)msgReport[1];
                }
                else
                {
                    throw new Exception("Invalid response");
                }
            }
            else
            {
                throw new Exception("Failed to send message");
            }
        }

        public InputSourceNumber GetCurrentSource()
        {
            byte[] msgData = this.BuildMessage(new byte[] { 0x03, 0x01, (byte)MessageSet.CurrentSourceGet, 0x00 });

            byte[] msgResponse;

            if (this.msgTransport.SendMessage(msgData, out msgResponse) == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return (InputSourceNumber)msgReport[2];
                }
                else
                {
                    throw new Exception("Invalid response");
                }
            }
            else
            {
                throw new Exception("Failed to send message");
            }
        }

        private byte[] BuildMessage(byte[] msgData)
        {
            byte[] msg = new byte[this.msgHeader.Length + msgData.Length];

            System.Buffer.BlockCopy(this.msgHeader, 0, msg, 0, this.msgHeader.Length);

            System.Buffer.BlockCopy(msgData, 0, msg, this.msgHeader.Length, msgData.Length);

            msg[msg.Length - 1] = this.CheckSum(msg);

            return msg;
        }

        private int ProcessResponse(byte[] msgResponse, out byte[] msgReport)
        {
            if (this.CheckSum(msgResponse) == msgResponse[msgResponse.Length - 1])
            {
                msgReport = new byte[msgResponse[4] - 2];

                System.Buffer.BlockCopy(msgResponse, 6, msgReport, 0, msgResponse[4] - 2);

                return 0;
            }
            else
            {
                msgReport = null;

                return 1;
            }
        }

        private byte CheckSum(byte[] msg)
        {
            byte hashValue = 0;

            for (int i = 0; i < msg.Length - 1; i++)
            {
                hashValue ^= msg[i];
            }

            return hashValue;
        }

        internal void SetInputSource(InputSourceType sourceType, InputSourceNumber sourceNumber)
        {
            byte[] msgData = this.BuildMessage(new byte[] { 0x07, 0x01, (byte)MessageSet.InputSourceSet, (byte)sourceType, (byte)sourceNumber, 0x01, 0x00, 0x00 });

            byte[] msgResponse;

            if (this.msgTransport.SendMessage(msgData, out msgResponse) == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return;
                }
                else
                {
                    throw new Exception("Invalid response");
                }
            }
            else
            {
                throw new Exception("Failed to send message");
            }
        }

        internal void SetPowerState(PowerState powerState)
        {
            byte[] msgData = this.BuildMessage(new byte[] { 0x04, 0x01, (byte)MessageSet.PowerStateSet, (byte)powerState, 0x00 });

            byte[] msgResponse;

            int ret = this.msgTransport.SendMessage(msgData, out msgResponse);
            
            if (ret == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return;
                }
                else
                {
                    throw new Exception("Invalid response");
                }
            }
            else
            {
                if (powerState == PowerState.On)
                {
                    throw new Exception("Failed to send message");
                }
            }
        }
    }
}
