using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TcpServerBaseLibrary.ServerObjects_Sync
{
    internal class WorkingTCPConnection_Sync : IWorkingTCPConnection
    {
        private byte[] _ReceiveBuffer;
        private byte[] _ProtocolPrefixBuffer = new byte[8];
        private int _BytesRead;

        //Dependency objects
        private ILogger _Logger;

        internal TCPConnectionState _ConnectionState { get; private set; }

        public Socket WorkSocket { get; }


        /// <summary>
        /// Flag indicating that this object is no longer usable, and trying to use it will throw an exception
        /// </summary>
        public bool IsDisposed { get; private set; }

        #region Constructor


        internal WorkingTCPConnection_Sync(Socket client, ILogger logger)
        {
            WorkSocket = client;
            _Logger = logger;

            //Set the initial state to receiving header data
            _ConnectionState = TCPConnectionState.ReceivingHeader;
        }

        #endregion


        #region Communcation methods

        /// <summary>
        /// Execute functions according to the internal state
        /// </summary>
        /// <param name="microseconds"> Time in microseconds allowed to wait for operation </param>
        public void ExecuteState(int microseconds)
        {

            switch (_ConnectionState)
            {
                case TCPConnectionState.ReceivingHeader:

                    //Try catch here is because the previous MessageHandler might have closed the socket already.
                    //flagging it as disposed.

                    try
                    {
                        if (WorkSocket.Poll(microseconds, SelectMode.SelectRead))
                        {
                            ReceiveHeaderData();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        CloseConnectionGracefully();
                        //throw;
                    }

                    break;

                case TCPConnectionState.ReceivingMessageData:


                    try
                    {
                        if (WorkSocket.Poll(microseconds, SelectMode.SelectRead))
                        {
                            ReceiveMessageData();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        CloseConnectionGracefully();
                        //throw;
                    }

                    break;
                default:
                    break;
            }
        }

        private void ReceiveHeaderData()
        {

            try
            {

                // End the data receiving that the socket has done and get
                // the number of bytes read.
                int received = this.WorkSocket.Receive(_ProtocolPrefixBuffer, _BytesRead, _ProtocolPrefixBuffer.Length - _BytesRead, SocketFlags.None);

                _Logger.Debug($"Read {received} bytes header data");

                //Append to field to keep track of bytes received between read attempts
                _BytesRead += received;

                // If no bytes were received, the connection is closed
                if (received <= 0)
                {
                    CloseConnectionGracefully();

                    return;
                }

                if (_BytesRead == 8)
                {

                    //Sends acknowledgment to client that we have read the header and is ready to receive the message
                    SendAcknowledgment(new ApplicationProtocolHeader(_ProtocolPrefixBuffer));

                    _ConnectionState = TCPConnectionState.ReceivingMessageData;

                    return;

                }
                else if (_BytesRead < 8)
                {
                    //We havn't gotten the whole header, wait for more data to come in
                    _Logger.Debug("Waiting for rest of header data");
                }
            }
            catch (Exception e)
            {

                _Logger.Debug("Error receiving header data: ");
                _Logger.Error(e.Message);

                CloseConnectionGracefully();
            }
        }

        private void ReceiveMessageData()
        {
            ApplicationProtocolHeader header = new ApplicationProtocolHeader(_ProtocolPrefixBuffer);

            //Reset bytes read, as we will now start to receive the rest of the data
            _BytesRead = 0;

            //Set buffer to appropriate size
            PrepareBuffer(header.Lenght);

            _Logger.Debug("Starts reading message data");

            try
            {
                while (this._BytesRead < _ReceiveBuffer.Length)
                {
                    _BytesRead += this.WorkSocket.Receive(this._ReceiveBuffer, _BytesRead, _ReceiveBuffer.Length - _BytesRead, SocketFlags.None);
                }

            }
            catch (Exception e)
            {
                _Logger.Debug("Error receiving message data");
                _Logger.Error(e.Message);

                CloseConnectionGracefully();
                return;
            }

            MessageObject dataobj = new MessageObject(this.WorkSocket, header, _ReceiveBuffer);

            //Pass dataobject to whoever listens for it
            //TODO: This may throw if no handler for that message is present


            try
            {
                NotifyCompleteDataReceived(dataobj);

            }
            catch (KeyNotFoundException)
            {
                _Logger.Error("No handler for specified messagetype. Shutting down connection");

                CloseConnectionGracefully();
                return;
            }

            PrepareToReadNextHeader();
        }

        private void PrepareToReadNextHeader()
        {
            this._BytesRead = 0;
            _ProtocolPrefixBuffer = new byte[8];
            _ConnectionState = TCPConnectionState.ReceivingHeader;
        }

        private void SendAcknowledgment(ApplicationProtocolHeader head)
        {
            _Logger.Debug("Sending header acknowledgment");

            try
            {
                WorkSocket.Send(head.WrapHeaderData());
            }
            catch (Exception)
            {
                _Logger.Error("Error sending header acknowledgment");
                CloseConnectionGracefully();
            }
        }

        #endregion

        #region Helper methods


        private void PrepareBuffer(int size)
        {
            this._ReceiveBuffer = new byte[size];
        }

        private void CloseConnectionGracefully()
        {
            _Logger.Info($"Closing connection with : {WorkSocket.RemoteEndPoint.ToString()}");

            this.WorkSocket.Shutdown(SocketShutdown.Both);
            this.WorkSocket.Close();


            this.IsDisposed = true;
        }

        #endregion



        public event Action<MessageObject> CompleteDataReceived;


        private void NotifyCompleteDataReceived(MessageObject Data)
        {
            _Logger.Debug("Complete message received");

            this.CompleteDataReceived?.Invoke(Data);

        }
    }
}
