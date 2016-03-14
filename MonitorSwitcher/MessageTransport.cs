namespace MonitorSwitcher
{
    public interface IMessageTransport
    {
        int SendMessage(byte[] msgData, out byte[] msgReport);
    }
}
