namespace MonitorSwitcher
{
    using System;
    using System.Drawing;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    public class SysTrayApp : Form
    {
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x05;
        private NotifyIcon trayIcon;

        private ContextMenu trayMenu;
        private TcpListener listener;
        private IMessageTransport comPort;
        private MonitorMessages msg;
        private Thread serverThread = null;
        private bool serverRunning = false;

        private static Mutex mutexProcess = new Mutex();

        public SysTrayApp()
        {
            try
            {
                this.comPort = new LocalSerialPort();

                //this.msg = new BDM4065Messages(this.comPort);

                this.msg = new SICP_V1_99(this.comPort);

                this.msg.GetPowerState();

                this.serverThread = new Thread(new ThreadStart(this.StartServer));
            }
            catch (Exception)
            {
                AutoClosingMessageBox.Show("Unable to communicate with monitor, using remote connection", string.Empty, 2000);

                this.comPort = new RemoteSerialPort();

                //this.msg = new BDM4065Messages(this.comPort);

                this.msg = new SICP_V1_99(this.comPort);
            }

            String[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                this.msg.SetDefaultInputSource(args[1]);
            }

            System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 30000;
            refreshTimer.Tick += this.RefreshTimer_Tick;

            // Create a simple tray menu with only one item.
            this.trayMenu = new ContextMenu();

            this.msg.AddInputSourceToContextMenu(this.trayMenu);

            this.trayMenu.MenuItems.Add(new MenuItem("Volume Up", this.OnVolumeUp) { Name = "Volume Up" });
            this.trayMenu.MenuItems.Add(new MenuItem("Volume Down", this.OnVolumeDown));
            this.trayMenu.MenuItems.Add(new MenuItem("Off", this.OnOff));
            this.trayMenu.MenuItems.Add(new MenuItem("Exit", this.OnExit));

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            this.trayIcon = new NotifyIcon();
            this.trayIcon.Text = "MonitorSwitcher";
            this.trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); // new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            this.trayIcon.ContextMenu = this.trayMenu;
            this.trayIcon.Visible = true;

            this.RefreshTimer_Tick(this, null);

            if (this.serverThread != null)
            {
                this.serverThread.Start();
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

                            string name = new string(dbi.dbcc_name);

                            try
                            {
                                if (name.Contains("USB#VID_046D&PID_C046") || name.Contains("USB#VID_1307&PID_0165") || name.Contains("USB#VID_0B0E&PID_0312"))
                                {
                                    this.msg.SetInputSourceToDefault();
                                }
                            }
                            catch
                            {
                                // Ignore
                            }
                        }

                        break;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Visible = false; // Hide form window.
            this.ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);

            UsbDeviceNotification.RegisterUsbDeviceNotification(this.Handle);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                this.trayIcon.Dispose();
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

        private void OnVolumeDown(object sender, EventArgs e)
        {
            int volume = this.msg.GetVolume();

            if (volume < 5)
            {
                volume = 0;
            }
            else
            {
                volume = volume - 5;
            }

            this.msg.SetVolume((byte)volume);
        }

        private void OnVolumeUp(object sender, EventArgs e)
        {
            int volume = this.msg.GetVolume();

            if (volume < 95)
            {
                volume = 100;
            }
            else
            {
                volume = volume + 5;
            }

            this.msg.SetVolume((byte)volume);
        }

        private void StartServer()
        {
            /* IPAddress[] IPS = Dns.GetHostAddresses(Dns.GetHostName());

             foreach (IPAddress ip in IPS)
             {
                 if (ip.AddressFamily == AddressFamily.InterNetwork)
                 {

                     Console.WriteLine("IP address: " + ip);
                 }
             } 
             */

            IPAddress localAddr = GetLocalIPAddress();

            this.listener = new TcpListener(IPAddress.Any, 11000);

            this.serverRunning = true;

            this.listener.Start();

            while (this.serverRunning)
            {
                try
                {
                    TcpClient client = this.listener.AcceptTcpClient();

                    Thread t = new Thread(new ParameterizedThreadStart(this.HandleClient));

                    t.Start(client);
                }
                catch (ThreadInterruptedException)
                {
                    this.serverRunning = false;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return;
                }
            }
        }

        private delegate int SendMessageMethod(byte[] msgData, out byte[] msgReport);


        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;

            bool clientConnected = true;

            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[250];

            SendMessageMethod sendMessageMethod = this.SendMessageThreadSafe;

            while (clientConnected && this.serverRunning)
            {
                try
                {
                    int noBytes = stream.Read(buffer, 0, buffer.Length);

                    try
                    {
                        mutexProcess.WaitOne();

                        Console.WriteLine("Read Start: " + client.Client.RemoteEndPoint.ToString());

                        if (noBytes > 0)
                        {
                            byte[] msgData = new byte[noBytes];

                            System.Buffer.BlockCopy(buffer, 0, msgData, 0, noBytes);

                            byte[] msgReport;

                            int status = sendMessageMethod(msgData, out msgReport);

                            ////int status = comPort.SendMessage(msgData, out msgReport);
                            Console.WriteLine(string.Join(", ", msgReport));
                            buffer[0] = (byte)status;

                            System.Buffer.BlockCopy(msgReport, 0, buffer, 1, msgReport.Length);

                            stream.Write(buffer, 0, msgReport.Length + 1);
                        }
                        else
                        {
                            clientConnected = false;
                        }
                        Console.WriteLine("Read Done: " + client.Client.RemoteEndPoint.ToString());
                    }
                    finally
                    {
                        mutexProcess.ReleaseMutex();
                    }
                }
                catch (Exception)
                {
                    clientConnected = false;
                }
            }
        }

        private int SendMessageThreadSafe(byte[] msgData, out byte[] msgReport)
        {
            return this.msg.RemoteSendMessage(msgData, out msgReport);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                mutexProcess.WaitOne();

                this.msg.UpdateContextMenu(this.trayMenu);

               // this.trayMenu.MenuItems["Volume Up"].Text = "Volume Up (" + this.msg.GetVolume() + ")";
            }
            finally
            {
                mutexProcess.ReleaseMutex();
            }
        }


        private void OnOff(object sender, EventArgs e)
        {
            this.msg.SetPowerState(BDM4065Messages.PowerState.Off);
        }

        private void OnExit(object sender, EventArgs e)
        {
            if (this.listener != null)
            {
                this.listener.Stop();
            }

            if (this.serverRunning)
            {
                this.serverRunning = false;

                this.serverThread.Interrupt();

                if (!this.serverThread.Join(2000))
                {
                    this.serverThread.Abort();
                }
            }

            Application.Exit();
        }
    }
}
