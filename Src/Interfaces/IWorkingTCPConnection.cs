using System;
using System.Net.Sockets;

namespace TcpServerBaseLibrary
{
    internal interface IWorkingTCPConnection
    {
        /// <summary>
        /// The socket used for communication
        /// </summary>
        Socket WorkSocket { get; }

        /// <summary>
        /// Flag indicating that this object is no longer usable, and trying to use it will throw an exception
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Resmes communications according to internal state
        /// </summary>
        /// <param name="ms">Time in microseconds allowed to wait for </param>
        void ExecuteState(int ms);

        /// <summary>
        /// Raised when the server has received a complete message
        /// </summary>
        event Action<MessageObject> CompleteDataReceived;
    }
}