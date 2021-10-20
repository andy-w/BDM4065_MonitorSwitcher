using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorSwitcher
{
    abstract public class MonitorMessages
    {
        public enum PowerState : byte
        {
            Off = 0x01,
            On = 0x02,
        }

        abstract public void SetDefaultInputSource(string inputSource);

        abstract public void SetInputSourceToDefault();

        abstract public PowerState GetPowerState();

        abstract public void SetPowerState(PowerState powerState);

        abstract public int GetVolume();

        abstract public void SetVolume(byte volume);

        abstract public void AddInputSourceToContextMenu(ContextMenu contextMenu);

        abstract public void UpdateContextMenu(ContextMenu contextMenu);

        abstract public int RemoteSendMessage(byte[] msgData, out byte[] msgReport);
    }
}
