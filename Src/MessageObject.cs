using System.Net.Sockets;

namespace TcpServerBaseLibrary
{
    public class MessageObject
    {
        public readonly Socket UsedConnection;

        public readonly ApplicationProtocolHeader MessageHeader;

        public readonly byte[] CompleteData;

        public MessageObject(Socket sock, ApplicationProtocolHeader head, byte[] data)
        {
            UsedConnection = sock;
            MessageHeader = head;
            CompleteData = data;
        }
    }
}
