using System;
using System.Collections.Generic;
using System.Text;

namespace TcpServerBaseLibrary
{

    public struct ApplicationProtocolHeader
    {
        public int Lenght;

        public int MessageTypeIdentifier;

        /// <summary>
        /// Create a new ApplicationProtocolHeader object with specified values
        /// </summary>
        /// <param name="lenght">Lenght of the message (Excluding header)</param>
        /// <param name="msgtype">Type of message</param>
        public ApplicationProtocolHeader(int lenght, int msgtype)
        {
            Lenght = lenght;
            MessageTypeIdentifier = msgtype;
        }

        /// <summary>
        /// Create a new ApplicationProtocolHeader object constructed from a byte[]
        /// </summary>
        /// <param name="data"></param>
        public ApplicationProtocolHeader(byte[] data)
        {
            this.Lenght = BitConverter.ToInt32(data, 0);
            this.MessageTypeIdentifier = BitConverter.ToInt32(data, 4);
        }

        /// <summary>
        /// Converts the ApplicationProtocolHeader object to a byte[] ready to be sent over a connection
        /// </summary>
        /// <returns></returns>
        public byte[] WrapHeaderData()
        {
            byte[] header = new byte[8];

            BitConverter.GetBytes(Lenght).CopyTo(header, 0);

            BitConverter.GetBytes((int)MessageTypeIdentifier).CopyTo(header, 4);

            return header;
        }
    }
}
