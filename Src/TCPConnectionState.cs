namespace TcpServerBaseLibrary
{
    internal enum TCPConnectionState
    {
        ReceivingHeader,
        ReceivingMessageData,
        ReceiveOperationStarted
    }
}
