﻿namespace MonitorSwitcher
{
    using System;
    using System.Windows.Forms;

    public class BDM4065Messages : MonitorMessages
    {
        private IMessageTransport msgTransport;

        private byte[] msgHeader = new byte[] { 0xA6, 0x01, 0x00, 0x00, 0x00 };

        private InputSourceNumber defaultInputSourceNumber = InputSourceNumber.DP;
        private InputSourceType defaultInputSourceType = InputSourceType.DisplayPort;

        public BDM4065Messages(IMessageTransport msgTransport)
        {
            this.msgTransport = msgTransport;
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
            byte[] msgData = this.BuildMessage(new byte[]
            {
                0x03,
                0x01,
                (byte)MessageSet.PowerStateGet,
                0x00
            });

            byte[] msgResponse;

            byte[] tmsgData = { 0x05, 0x01, 0x0, 0x19, 0x1D };

            if (this.msgTransport.SendMessage(tmsgData, out msgResponse) == 0)
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

        override public void SetPowerState(PowerState powerState)
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

        override public int GetVolume()
        {
            byte[] msgData = this.BuildMessage(new byte[] { 0x03, 0x01, (byte)MessageSet.VolumeGet, 0x00 });

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
            byte[] msgData = this.BuildMessage(new byte[] { 0x04, 0x01, (byte)MessageSet.VolumeSet, volume, 0x00 });

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

        public override void SetDefaultInputSource(string inputSource)
        {
            switch (inputSource)
            {
                case "VGA":
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.VGA;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.VGA;
                    break;

                case "MiniDP":
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.miniDP;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.DisplayPort;
                    break;

                case "DP":
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.DP;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.DisplayPort;
                    break;

                case "HDMI":
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.HDMI;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.HDMI;
                    break;

                case "MHL-HDMI":
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.MHLHDMI2;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.HDMI;
                    break;

                default:
                    this.defaultInputSourceNumber = BDM4065Messages.InputSourceNumber.DP;
                    this.defaultInputSourceType = BDM4065Messages.InputSourceType.DisplayPort;
                    break;
            }
        }

        public override void AddInputSourceToContextMenu(ContextMenu contextMenu)
        {
            contextMenu.MenuItems.Add(new MenuItem("DP", this.OnInputSourceDP) { Name = "DP" });
            contextMenu.MenuItems.Add(new MenuItem("MiniDP", this.OnInputSourceMiniDP) { Name = "MiniDP" });
            contextMenu.MenuItems.Add(new MenuItem("HDMI", this.OnInputSourceHDMI) { Name = "HDMI" });
            contextMenu.MenuItems.Add(new MenuItem("MHL-HDMI", this.OnInputSourceMHLHDMI) { Name = "MHL-HDMI" });
            contextMenu.MenuItems.Add(new MenuItem("VGA", this.OnInputSourceVGA) { Name = "VGA" });
        }

        private void OnInputSourceHDMI(object sender, EventArgs e)
        {
            this.SetInputSource(InputSourceType.HDMI, InputSourceNumber.HDMI);
        }

        private void OnInputSourceDP(object sender, EventArgs e)
        {
            this.SetInputSource(BDM4065Messages.InputSourceType.DisplayPort, BDM4065Messages.InputSourceNumber.DP);
        }

        private void OnInputSourceMiniDP(object sender, EventArgs e)
        {
            this.SetInputSource(BDM4065Messages.InputSourceType.DisplayPort, BDM4065Messages.InputSourceNumber.miniDP);
        }

        private void OnInputSourceMHLHDMI(object sender, EventArgs e)
        {
            this.SetInputSource(BDM4065Messages.InputSourceType.HDMI, BDM4065Messages.InputSourceNumber.MHLHDMI2);
        }

        private void OnInputSourceVGA(object sender, EventArgs e)
        {
            this.SetInputSource(BDM4065Messages.InputSourceType.VGA, BDM4065Messages.InputSourceNumber.VGA);
        }

        public override void UpdateContextMenu(ContextMenu contextMenu)
        {
            try
            {
                BDM4065Messages.InputSourceNumber currentSource = this.GetCurrentSource();

                contextMenu.MenuItems["DP"].Enabled = true;
                contextMenu.MenuItems["MiniDP"].Enabled = true;
                contextMenu.MenuItems["MHL-HDMI"].Enabled = true;
                contextMenu.MenuItems["HDMI"].Enabled = true;
                contextMenu.MenuItems["VGA"].Enabled = true;

                contextMenu.MenuItems["DP"].Checked = currentSource == BDM4065Messages.InputSourceNumber.DP;
                contextMenu.MenuItems["MiniDP"].Checked = currentSource == BDM4065Messages.InputSourceNumber.miniDP;
                contextMenu.MenuItems["MHL-HDMI"].Checked = currentSource == BDM4065Messages.InputSourceNumber.MHLHDMI2;
                contextMenu.MenuItems["HDMI"].Checked = currentSource == BDM4065Messages.InputSourceNumber.HDMI;
                contextMenu.MenuItems["VGA"].Checked = currentSource == BDM4065Messages.InputSourceNumber.VGA;
            }
            catch
            {
                contextMenu.MenuItems["DP"].Enabled = false;
                contextMenu.MenuItems["MiniDP"].Enabled = false;
                contextMenu.MenuItems["MHL-HDMI"].Enabled = false;
                contextMenu.MenuItems["HDMI"].Enabled = false;
                contextMenu.MenuItems["VGA"].Enabled = false;
            }

        }

        public override void SetInputSourceToDefault()
        {
            this.SetInputSource(this.defaultInputSourceType,this.defaultInputSourceNumber);
        }

        public override int RemoteSendMessage(byte[] msgData, out byte[] msgReport)
        {
            return msgTransport.SendMessage(msgData, out msgReport);
        }

    }
}
