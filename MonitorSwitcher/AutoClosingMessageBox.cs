namespace MonitorSwitcher
{
    using System;
    using System.Windows.Forms;

    public class AutoClosingMessageBox
    {
        private System.Threading.Timer timeoutTimer;
        private string caption;

        public AutoClosingMessageBox(string text, string caption, int timeout)
        {
            this.caption = caption;

            this.timeoutTimer = new System.Threading.Timer(OnTimerElapsed, null, timeout, System.Threading.Timeout.Infinite);

            MessageBox.Show(text, this.caption);
        }

        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }

        private void OnTimerElapsed(object state)
        {
            IntPtr hWnd = FindWindow("#32770", caption); // lpClassName is #32770 for MessageBox
            if (hWnd != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            timeoutTimer.Dispose();
        }

        private const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}