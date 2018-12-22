namespace TcpServerBaseLibrary
{
    internal enum TCPServerState
    {
        Listening,
        AcceptConnectionRequestOperationStarted,

        ConnectionThresholdReached            
    }
}
