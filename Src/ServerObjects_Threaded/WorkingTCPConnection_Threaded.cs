using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServerBaseLibrary.ServerObjects_Threaded
{
    //TODO
    //Implement dispose
    //TODO: Sanity check for large packages. If the expected data is huge, receive in chunks.

    /// <summary>
    /// Class for handling any individual open tcp connection, also responsible for receiving complete data-packages 
    /// </summary>
    internal class WorkingTCPConnection_Threaded : IWorkingTCPConnection
    {
        private byte[] _ReceiveBuffer;
        private byte[] _ProtocolPrefixBuffer = new byte[8];

        private int _BytesRead;

        public Socket WorkSocket { get; set; }

        public bool IsDisposed => throw new NotImplementedException();

        private ILogger _Logger;

        private TCPConnectionState _ConnectionState;

        public WorkingTCPConnection_Threaded(Socket client, ILogger logger)
        {
            WorkSocket = client;
            _Logger = logger;

            _ConnectionState = TCPConnectionState.ReceivingHeader;
            //StartListen();
        }

        public async Task StartListen()
        {

            await Task.Run(() =>
            {

                _Logger.Debug($"New connection \"StartListenOnNewThread\" after Task.Run is running thread id : {Thread.CurrentThread.ManagedThreadId}");


                while (true)
                {

                    if (_ConnectionState == TCPConnectionState.ReceivingHeader)
                    {
                        _Logger.Debug("Preparing to read header data async");

                        _ConnectionState = TCPConnectionState.ReceiveOperationStarted;

                        this.WorkSocket.BeginReceive(
                        _ProtocolPrefixBuffer, 0, _ProtocolPrefixBuffer.Length - _BytesRead, SocketFlags.None,
                        new AsyncCallback(OnBytesReceived), this);

                    }

                    if (_ConnectionState == TCPConnectionState.ReceivingMessageData)
                    {

                        _ConnectionState = TCPConnectionState.ReceiveOperationStarted;

                        ListenForMessage();
                    }
                };
            });
        }


        #region TestMethodsIgnore
        public async Task StartListenWithBlockingCalls()
        {

            _Logger.Debug($"New connection \"StartListenBlocking\" before Task.Run is running thread id : {Thread.CurrentThread.ManagedThreadId}");


            await Task.Run(() =>
            {

                _Logger.Debug($"New connection \"StartListenBlocking\" after Task.Run is running thread id : {Thread.CurrentThread.ManagedThreadId}");


                while (true)
                {

                    if (_ConnectionState == TCPConnectionState.ReceivingHeader)
                    {
                        _Logger.Debug("Preparing to read header data async");

                        ReceiveSync();

                    }

                    if (_ConnectionState == TCPConnectionState.ReceivingMessageData)
                    {

                        ListenForMessage();
                    }
                };
            });
        }

        private void ReceiveSync()
        {

            _Logger.Debug($"ReceiveSync is running thread id : {Thread.CurrentThread.ManagedThreadId}");


            try
            {
                int received = this.WorkSocket.Receive(_ProtocolPrefixBuffer, 0, _ProtocolPrefixBuffer.Length - _BytesRead, SocketFlags.None);

                _Logger.Debug($"received {received} bytes");

                //Append to field to keep track of bytes received between read attempts
                _BytesRead += received;

                // If no bytes were received, the connection is closed
                if (received <= 0)
                {
                    this.WorkSocket.Close();

                    NotifyConnectionClosed();

                    return;
                }

                if (_BytesRead == 8)
                {

                    _ConnectionState = TCPConnectionState.ReceivingMessageData;

                    return;

                }
                else if (_BytesRead < 8)
                {
                    //We havn't gotten the whole header, wait for more data to come in
                    _Logger.Debug("Waiting for rest of header data");

                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion


        protected void OnBytesReceived(IAsyncResult result)
        {

            _Logger.Debug($"OnBytesReceived is running thread id : {Thread.CurrentThread.ManagedThreadId}");


            try
            {

                // End the data receiving that the socket has done and get
                // the number of bytes read.
                int received = this.WorkSocket.EndReceive(result);

                _Logger.Debug($"received {received} bytes");

                //Append to filed to keep track of bytes received between read attempts
                _BytesRead += received;

                // If no bytes were received, the connection is closed
                if (received <= 0)
                {
                    this.WorkSocket.Close();

                    NotifyConnectionClosed();

                    return;
                }

                if (_BytesRead == 8)
                {
                    //We have gotten the protocol prefix data, switch state to receiving message data
                    _ConnectionState = TCPConnectionState.ReceivingMessageData;

                    return;
                    
                }
                else if (_BytesRead < 8)
                {
                    //We havn't gotten the whole header, wait for more data to come in
                    _Logger.Debug("Waiting for rest of header data");

                }
            }
            catch (Exception)
            {
                NotifyConnectionClosed();
                //throw;
            }
        }

        private void SendAcknowledgment(ApplicationProtocolHeader head)
        {
            _Logger.Debug("Sending acknowledgment");

            WorkSocket.Send(head.WrapHeaderData()); 
        }

        private void PrepareBuffer(int size)
        {
            this._ReceiveBuffer = new byte[size];
        }


        //Sends an acknowledgment to client and prepares to receive expected data synchronously
        private void ListenForMessage()
        {

            ApplicationProtocolHeader header = new ApplicationProtocolHeader(_ProtocolPrefixBuffer);


            _Logger.Info($"Header received, expecting Messagetype" +
                $" : {header.MessageTypeIdentifier.ToString()}, with lenght {header.Lenght}");

            //Reset bytes read, as we will now start to receive the rest of the data
            _BytesRead = 0;

            //Sends acknowledgment to client that we are ready to receive the message
            SendAcknowledgment(header);

            //Set buffer to appropriate size
            PrepareBuffer(header.Lenght);

            try
            {
                while (this._BytesRead < _ReceiveBuffer.Length)
                {
                    _BytesRead += this.WorkSocket.Receive(this._ReceiveBuffer, _BytesRead, _ReceiveBuffer.Length, SocketFlags.None);
                }

                Console.WriteLine("Complete message received");

                MessageObject dataobj = new MessageObject(this.WorkSocket, header, _ReceiveBuffer);

                //Pass dataobject to whoever listens for it
                NotifyCompleteDataReceived(dataobj);

                //Reset bytes read as we prepare to read the next header
                this._BytesRead = 0;
                _ProtocolPrefixBuffer = new byte[8];

                _ConnectionState = TCPConnectionState.ReceivingHeader;

                return;
            }
            catch (Exception)
            {
                NotifyConnectionClosed();
                //throw;
            }
        }

        

        public event Action<MessageObject> CompleteDataReceived;

        public event Action<IWorkingTCPConnection> ConnectionClosedEvent;

        private void NotifyCompleteDataReceived(MessageObject Data)
        {
            this.CompleteDataReceived?.Invoke(Data);

        }

        private void NotifyConnectionClosed()
        {
            _Logger.Info("Connection was shut down");
            ConnectionClosedEvent?.Invoke(this);
        }

        public void ExecuteState()
        {
            throw new NotImplementedException();
        }

        public void ExecuteState(int ms)
        {
            throw new NotImplementedException();
        }
    }
}
