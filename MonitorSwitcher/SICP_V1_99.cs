using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorSwitcher
{
    using System;

    public class SICP_V1_99 : MonitorMessages
    {
        private IMessageTransport msgTransport;

        private InputSourceTypeNumber defaultInputSourceTypeNumber;

        private byte[] msgHeader = new byte[] { 0x0, 0x01, 0x00 };

        public InputSourceTypeNumber currentSource { get; set; }

        public SICP_V1_99(IMessageTransport msgTransport)
        {
            this.msgTransport = msgTransport;
        }

        public enum InputSourceTypeNumber : byte
        {
            Video = 0x01,
            S_Video = 0x02,
            Component = 0x03,
            CVI2 = 0x04,  // (not applicable)
            VGA = 0x05,
            HDMI2 = 0x06,
            DisplayPort2 = 0x07,
            USB2 = 0x08,
            Card_DVID = 0x09,
            DisplayPort1 = 0x0A,
            CardOPS = 0x0B,
            USB1 = 0x0C,
            HDMI = 0x0D,
            DVID = 0x0E,
            HDMI3 = 0x0F,
            Browser = 0x10,
            SmartCMS = 0x11,
            DMS = 0X12,   // (Digital Media Server)
            InternalStorage = 0x13,
            Reserved1 = 0x14,
            Reserved2 = 0x15,
            MediaPlayer = 0x16,
            PDFPlayer = 0x17,
            Custom = 0x18,
        }

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
            SmartPowerSet = 0xDD
        }

        override public PowerState GetPowerState()
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.PowerStateGet });

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

        public InputSourceTypeNumber GetCurrentSource()
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.CurrentSourceGet });

            byte[] msgResponse;

            if (this.msgTransport.SendMessage(msgData, out msgResponse) == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return (InputSourceTypeNumber)msgReport[2];
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

        internal void SetInputSource(InputSourceTypeNumber sourceTypeNumber)
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.InputSourceSet, (byte)sourceTypeNumber, (byte)sourceTypeNumber, 0x01, 0x00 });

            byte[] msgResponse;

            if (this.msgTransport.SendMessage(msgData, out msgResponse) == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    this.currentSource = sourceTypeNumber;

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

        override public void SetPowerState(PowerState powerState)
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.PowerStateSet, (byte)powerState });

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

        override public int GetVolume()
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.VolumeGet });

            byte[] msgResponse;

            int ret = this.msgTransport.SendMessage(msgData, out msgResponse);

            if (ret == 0)
            {
                byte[] msgReport;

                if (this.ProcessResponse(msgResponse, out msgReport) == 0)
                {
                    return msgReport[1];
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

        override public void SetVolume(byte volume)
        {
            byte[] msgData = this.BuildMessage(new byte[] { (byte)MessageSet.VolumeSet, volume });

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
                throw new Exception("Failed to send message");
            }
        }

        private byte[] BuildMessage(byte[] msgData)
        {
            byte[] msg = new byte[this.msgHeader.Length + msgData.Length + 1];

            System.Buffer.BlockCopy(this.msgHeader, 0, msg, 0, this.msgHeader.Length);

            System.Buffer.BlockCopy(msgData, 0, msg, this.msgHeader.Length, msgData.Length);

            msg[0] = (byte)msg.Length;

            msg[msg.Length - 1] = this.CheckSum(msg);

            return msg;
        }

        private int ProcessResponse(byte[] msgResponse, out byte[] msgReport)
        {
            if (this.CheckSum(msgResponse) == msgResponse[msgResponse.Length - 1])
            {
                msgReport = new byte[msgResponse[0] - 4];

                System.Buffer.BlockCopy(msgResponse, 3, msgReport, 0, msgResponse[0] - 4);

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

        public override void SetDefaultInputSource(string inputSource)
        {
            switch (inputSource)
            {
                case "DP1":
                    this.defaultInputSourceTypeNumber = InputSourceTypeNumber.DisplayPort1;
                    break;

                case "HDMI":
                    this.defaultInputSourceTypeNumber = InputSourceTypeNumber.HDMI;
                    break;

                case "HDMI2":
                    this.defaultInputSourceTypeNumber = InputSourceTypeNumber.HDMI2;
                    break;

                case "HDMI3":
                    this.defaultInputSourceTypeNumber = InputSourceTypeNumber.HDMI3;
                    break;

                default:
                    this.defaultInputSourceTypeNumber = InputSourceTypeNumber.DisplayPort1;
                    break;

            }
        }

        public override void AddInputSourceToContextMenu(ContextMenu contextMenu)
        {
            contextMenu.MenuItems.Add(new MenuItem("DP1", this.OnInputSourceDP) { Name = "DP1" });
            contextMenu.MenuItems.Add(new MenuItem("HDMI", this.OnInputSourceHDMI) { Name = "HDMI" });
            contextMenu.MenuItems.Add(new MenuItem("HDMI2", this.OnInputSourceHDMI2) { Name = "HDMI2" });
            contextMenu.MenuItems.Add(new MenuItem("HDMI3", this.OnInputSourceHDMI3) { Name = "HDMI3" });
        }

        private void OnInputSourceHDMI(object sender, EventArgs e)
        {
            this.SetInputSource(InputSourceTypeNumber.HDMI);
        }
        private void OnInputSourceHDMI2(object sender, EventArgs e)
        {
            this.SetInputSource(InputSourceTypeNumber.HDMI2);
        }

        private void OnInputSourceHDMI3(object sender, EventArgs e)
        {
            this.SetInputSource(InputSourceTypeNumber.HDMI3);
        }


        private void OnInputSourceDP(object sender, EventArgs e)
        {
            this.SetInputSource(InputSourceTypeNumber.DisplayPort1);
        }

        public override void UpdateContextMenu(ContextMenu contextMenu)
        {
            try
            {
                this.currentSource = this.GetCurrentSource();

                contextMenu.MenuItems["DP1"].Enabled = true;
                contextMenu.MenuItems["HDMI"].Enabled = true;
                contextMenu.MenuItems["HDMI2"].Enabled = true;
                contextMenu.MenuItems["HDMI3"].Enabled = true;

                contextMenu.MenuItems["DP1"].Checked = currentSource == InputSourceTypeNumber.DisplayPort1;
                contextMenu.MenuItems["HDMI"].Checked = currentSource == InputSourceTypeNumber.HDMI;
                contextMenu.MenuItems["HDMI2"].Checked = currentSource == InputSourceTypeNumber.HDMI2;
                contextMenu.MenuItems["HDMI3"].Checked = currentSource == InputSourceTypeNumber.HDMI3;
            }
            catch
            {
                contextMenu.MenuItems["DP1"].Enabled = false;
                contextMenu.MenuItems["HDMI"].Enabled = false;
                contextMenu.MenuItems["HDMI2"].Enabled = false;
                contextMenu.MenuItems["HDMI3"].Enabled = false;
            }
        }

        public override void SetInputSourceToDefault()
        {
            if (this.currentSource != this.defaultInputSourceTypeNumber)
            {
                this.SetInputSource(this.defaultInputSourceTypeNumber);
            }
        }

        public override int RemoteSendMessage(byte[] msgData, out byte[] msgReport)
        {
            switch (msgData[this.msgHeader.Length])
            {
                case (byte)MessageSet.CurrentSourceGet:

                    msgReport = new byte[] { 0, (byte)MessageSet.CurrentSourceGet, (byte)this.currentSource, (byte)this.currentSource, 0, 0 };
                    
                    return msgTransport.SendMessage(msgData, out msgReport);

                default:

                    return msgTransport.SendMessage(msgData, out msgReport);

            }

            
        }
    }
}
