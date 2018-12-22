using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpServerBaseLibrary.ServerObjects_Threaded
{
    public class TCPServer_Threaded : ITCPServer
    {
        public const int MaxConnectionsAllowed = 2;

        public const int listeningport = 6555;

        private TcpListener tcplistener;

        private TCPServerState _serverState = TCPServerState.Listening;

        private List<IWorkingTCPConnection> TCPConnections = new List<IWorkingTCPConnection>();

        private ILogger Logger;

        private int threads;

        public TCPServer_Threaded(ILogger logger)
        {
            this.Logger = logger;

            tcplistener = new TcpListener(IPAddress.Loopback, listeningport);

            Logger.Info("Server started!");

        }

        public void Start()
        {

            AsyncCallback connectionrequestcallback = new AsyncCallback(this.OnConnectRequest);

            this.tcplistener.Start();



            //Main loop to keep the server going
            while (true)
            {

                if (_serverState == TCPServerState.ConnectionThresholdReached)
                {
                    continue;
                }

                if (_serverState != TCPServerState.ConnectionThresholdReached && _serverState != TCPServerState.AcceptConnectionRequestOperationStarted)
                {
                    Logger.Debug($"Starts listening for connection request number: {TCPConnections.Count + 1}");



                    this.tcplistener.BeginAcceptTcpClient(connectionrequestcallback, tcplistener);
                    _serverState = TCPServerState.AcceptConnectionRequestOperationStarted;

                }


                //Just tells us on which thread the main loop is running, and updates us if it should changed (it doesn't, it's always ThreadID 1)
                if (threads != System.Diagnostics.Process.GetCurrentProcess().Threads.Count)
                {

                    threads = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                    Logger.Debug($"Main loop is running thread id : {Thread.CurrentThread.ManagedThreadId}");
                }
            }
        }

        public void Stop()
        {

        }

        private async void OnConnectRequest(IAsyncResult result)
        {

            Logger.Debug($"OnConnectRequest is running thread id : {Thread.CurrentThread.ManagedThreadId}");

            TcpListener sock = (TcpListener)result.AsyncState;

            WorkingTCPConnection_Threaded newConn = new WorkingTCPConnection_Threaded(sock.Server.EndAccept(result), Logger);

            Logger.Info($"Connection made with {newConn.WorkSocket.RemoteEndPoint.ToString()}");

            TCPConnections.Add(newConn);

            newConn.ConnectionClosedEvent += this.OnConnectionClosed;

            if (TCPConnections.Count >= MaxConnectionsAllowed)
            {
                _serverState = TCPServerState.ConnectionThresholdReached;

                Logger.Info("Max allowed connections has been reached");
            }
            else
            {
                //Reset server state to default

                _serverState = TCPServerState.Listening;
            }

            await newConn.StartListen();


        }


        private void OnConnectionClosed(IWorkingTCPConnection connection)
        {
            if (TCPConnections.Contains(connection))
            {
                //connection.ConnectionClosedEvent -= this.OnConnectionClosed;

                TCPConnections.Remove(connection);

                Logger.Info($"Connection closed and removed");
            }

            if (TCPConnections.Count < MaxConnectionsAllowed)
            {
                _serverState = TCPServerState.Listening;
            }
        }
    }
}
