namespace MonitorSwitcher
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Net.Sockets;
    using System.IO.Ports;

    public class SysTrayApp : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private TcpListener listener;
        private BDM4065_SerialPort comPort;
        private BDM4065Messages msg;

        public SysTrayApp()
        {
            comPort = new BDM4065_SerialPort();

            msg = new BDM4065Messages(comPort);

            Timer refreshTimer = new Timer();
            refreshTimer.Interval = 10000;
            refreshTimer.Tick += refreshTimer_Tick;

            //listener = new TcpListener(11000);

            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add("DP", OnInputSourceDP);
            trayMenu.MenuItems.Add("miniDP", OnInputSourceMiniDP);
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

            refreshTimer.Start();
        }

        void refreshTimer_Tick(object sender, EventArgs e)
        {
            BDM4065Messages.InputSourceNumber currentSource = msg.GetCurrentSource();

            MenuItem[] menuItems;
            
            menuItems = trayMenu.MenuItems.Find("DP", false);

            if (menuItems.Length>0)
            {
                menuItems[0].Checked = (currentSource == BDM4065Messages.InputSourceNumber.DP);
            }

            menuItems = trayMenu.MenuItems.Find("MiniDP", false);

            if (menuItems.Length > 0)
            {
                menuItems[0].Checked = (currentSource == BDM4065Messages.InputSourceNumber.miniDP);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnInputSourceDP(object sender, EventArgs e)
        {
        }

        private void OnInputSourceMiniDP(object sender, EventArgs e)
        {
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
    }
}
