﻿namespace MonitorSwitcher
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Net.Sockets;
    using System.IO.Ports;
    using System.Threading;
    using System.Net;
    using System.Runtime.InteropServices;

    public class SysTrayApp : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private TcpListener listener;
        private MessageTransport comPort;
        private BDM4065Messages msg;
        private Thread serverThread = null;

        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x05;

        public SysTrayApp()
        {
            try
            {
                comPort = new LocalSerialPort();

                msg = new BDM4065Messages(comPort);

                msg.GetPowerState();

                serverThread = new Thread(new ThreadStart(StartServer));
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to communicate with monitor", "", MessageBoxButtons.OK);

                comPort = new RemoteSerialPort();

                msg = new BDM4065Messages(comPort);
            }

            System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 10000;
            refreshTimer.Tick += refreshTimer_Tick;

            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add(new MenuItem("DP", OnInputSourceDP) { Name = "DP" });
            trayMenu.MenuItems.Add(new MenuItem("MiniDP", OnInputSourceMiniDP) { Name = "MiniDP" });
            trayMenu.MenuItems.Add(new MenuItem("Exit", OnExit));

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "MonitorSwitcher";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            refreshTimer_Tick(this, null);

            if (serverThread != null)
            {
                serverThread.Start();
            }

            refreshTimer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == UsbDeviceNotification.WmDevicechange)
            {
                switch ((int)m.WParam)
                {
                    case UsbDeviceNotification.DbtDevicearrival:

                        int devType = Marshal.ReadInt32(m.LParam, 4);

                        if (devType == DBT_DEVTYP_DEVICEINTERFACE)
                        {
                            DEV_BROADCAST_DEVICEINTERFACE1 dbi;

                            dbi = (DEV_BROADCAST_DEVICEINTERFACE1)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE1));

                            String name = new string(dbi.dbcc_name);

                            if (name.Contains("USB#VID_046D&PID_C046"))
                            {
                                msg.SetInputSource(BDM4065Messages.InputSourceType.DisplayPort, BDM4065Messages.InputSourceNumber.DP);
                            }
                        }

                        break;
                }
            }
        }

        private void StartServer()
        {
            IPAddress localAddr = GetLocalIPAddress();

            this.listener = new TcpListener(localAddr, 11000);

            this.listener.Start();

            while (true)
            {
                try
                {
                    TcpClient client = this.listener.AcceptTcpClient();

                    Thread t = new Thread(new ParameterizedThreadStart(HandleClient));

                    t.Start(client);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return;
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;

            Boolean clientConnected = true;

            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[250];

            // SendMessageMethod sendMessageMethod = this.SendMessageThreadSafe;

            while (clientConnected)
            {
                int noBytes = stream.Read(buffer, 0, buffer.Length);

                if (noBytes > 0)
                {
                    byte[] msgData = new byte[noBytes];

                    System.Buffer.BlockCopy(buffer, 0, msgData, 0, noBytes);

                    byte[] msgReport;

                    int status = comPort.SendMessage(msgData,out msgReport);

                    buffer[0] = (byte)status;

                    System.Buffer.BlockCopy(msgReport, 0, buffer, 1, msgReport.Length);

                    stream.Write(buffer, 0, msgReport.Length+1);
                }
                else
                {
                    clientConnected = false;
                }
            }
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            BDM4065Messages.InputSourceNumber currentSource = msg.GetCurrentSource();

            trayMenu.MenuItems["DP"].Checked = (currentSource == BDM4065Messages.InputSourceNumber.DP);
            trayMenu.MenuItems["MiniDP"].Checked = (currentSource == BDM4065Messages.InputSourceNumber.miniDP);
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);

            UsbDeviceNotification.RegisterUsbDeviceNotification(this.Handle);
        }

        private void OnInputSourceDP(object sender, EventArgs e)
        {
            msg.SetInputSource(BDM4065Messages.InputSourceType.DisplayPort, BDM4065Messages.InputSourceNumber.DP);
        }

        private void OnInputSourceMiniDP(object sender, EventArgs e)
        {
            msg.SetInputSource(BDM4065Messages.InputSourceType.DisplayPort, BDM4065Messages.InputSourceNumber.miniDP);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private static IPAddress GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}