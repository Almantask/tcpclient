using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TcpServerBaseLibrary.Interfaces;

namespace TcpServerBaseLibrary.ServerObjects_Sync
{
    public class TCPServer_Sync : ITCPServer
    {
        private readonly int _MaxConnectionsAllowed;

        private readonly int _Listeningport;

        private TcpListener _tcplistener;

        private TCPServerState _serverState = TCPServerState.Listening;

        private List<IWorkingTCPConnection> TCPConnections = new List<IWorkingTCPConnection>();

        //Dependency objects
        private ILogger _Logger;

        private readonly Dictionary<int, IMessageManager> _Parsers;


        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="port"></param>
        /// <param name="parsers">Message managers specified for each message type</param>
        /// <param name="allowed">Max connections allowed</param>
        public TCPServer_Sync(ILogger logger, int port, Dictionary<int, IMessageManager> parsers, int allowed)
        {

            if (allowed < 1)
            {
                throw new ArgumentException("Max allowed connections must be 1 or more");
            }

            _Logger = logger;
            _Listeningport = port;
            _Parsers = parsers;
            _MaxConnectionsAllowed = allowed;
        }



        #endregion


        /// <summary>
        /// Starts the server to process incoming connection requests and incoming data, which is processed using the provided parsers
        /// This method enters a (infinite) loop
        /// </summary>
        public void Start()
        {

            _Logger.Info("Server started!");

            
            //TODO: Make mthod for seraching for IP
            _tcplistener = new TcpListener(IPAddress.Any, _Listeningport);

            //Starts the listener
            this._tcplistener.Start();


            //Main loop to keep the server going
            while (true)
            {

                if (_serverState != TCPServerState.ConnectionThresholdReached)
                {

                    if (_tcplistener.Pending())
                    {

                        var newconnection = this._tcplistener.AcceptTcpClient();

                        SetupNewConnection(newconnection);
                    }
                }



                //Check for closed down connections before we try to use it
                if (TCPConnections.Any(x => x.IsDisposed == true))
                {
                    //Remove any closed connections from TCPConnections              
                    RemoveClosedConnections(TCPConnections.Where(x => x.IsDisposed == true).ToList());
                }
                

                //Loops through available connections and let them run their methods according to their state
                foreach (var connection in TCPConnections)
                {
                    connection.ExecuteState(100);
                }
            }
        }

        public void Stop()
        {
            //TODO Add a mechanism to stop the server (Cancellationstoken), or IsDisposed
        
        }

        private void SetupNewConnection(TcpClient newconnection)
        {

            WorkingTCPConnection_Sync newconn = new WorkingTCPConnection_Sync(newconnection.Client, _Logger);

            _Logger.Info($"Connection made with {newconn.WorkSocket.RemoteEndPoint.ToString()}");

            TCPConnections.Add(newconn);

            //_Logger.LogMessage($"Connection count : {TCPConnections.Count}");

            //Register eventhandlers
            newconn.CompleteDataReceived += OnCompleteDataReceived;

            CheckAndChangeStateAgainstConnectionCount();
        }

        private void CheckAndChangeStateAgainstConnectionCount()
        {
            if (TCPConnections.Count >= _MaxConnectionsAllowed)
            {
                _serverState = TCPServerState.ConnectionThresholdReached;

                _Logger.Debug("Connection threshold reached");
                _Logger.Debug("Stopping listener");

                _tcplistener.Stop();
                return;
            }

            if (TCPConnections.Count < _MaxConnectionsAllowed)
            {
                if (_serverState != TCPServerState.Listening)
                {

                    _serverState = TCPServerState.Listening;

                    _Logger.Debug("Starting listener again");
                    _tcplistener.Start();
                    return;
                }
            }
        }

        private void RemoveClosedConnections(List<IWorkingTCPConnection> list)
        {

            foreach (var item in list)
            {
                item.CompleteDataReceived -= this.OnCompleteDataReceived;
                TCPConnections.Remove(item);
                _Logger.Debug("Connection removed");
            }

            CheckAndChangeStateAgainstConnectionCount();
        }

        private void OnCompleteDataReceived(MessageObject obj)
        {
            try
            {
                _Parsers[obj.MessageHeader.MessageTypeIdentifier].HandleMessage(obj);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
        }
    }
}
