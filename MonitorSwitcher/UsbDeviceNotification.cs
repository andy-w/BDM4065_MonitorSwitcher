namespace MonitorSwitcher
{
    using System;
    using System.Runtime.InteropServices;

    internal static class UsbDeviceNotification
    {
        public const int DbtDevicearrival = 0x8000; // system detected a new device        
        public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
        public const int WmDevicechange = 0x0219; // device change event      
        private const int DbtDevtypDeviceinterface = 5;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private static IntPtr notificationHandle;
        private const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x0;

        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            DEV_BROADCAST_DEVICEINTERFACE dbi = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_devicetype = DbtDevtypDeviceinterface,
                dbcc_reserved = 0,
                dbcc_classguid = GuidDevinterfaceUSBDevice,
                dbcc_name = 0
            };

            dbi.dbcc_size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.dbcc_size);
            Marshal.StructureToPtr(dbi, buffer, true);

            notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_HDR
    {
        internal uint dbch_Size;
        internal uint dbch_DeviceType;
        internal uint dbch_Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class DEV_BROADCAST_DEVICEINTERFACE
    {
        internal int dbcc_size;
        internal int dbcc_devicetype;
        internal int dbcc_reserved;
        internal Guid dbcc_classguid;
        internal short dbcc_name;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class DEV_BROADCAST_DEVICEINTERFACE1
    {
        internal int dbcc_size;
        internal int dbcc_devicetype;
        internal int dbcc_reserved;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        internal byte[] dbcc_classguid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        internal char[] dbcc_name;
    }
}
