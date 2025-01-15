using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BoxPrint.Communication
{
    /// <summary>
    /// 비동기 방식의 다중 클라이언트 서버 
    /// </summary>
    public class AsyncSocketServer
    {
        private const int backLog = 100;
        public bool IsSocketBounded
        {
            get
            {
                return listener != null && listener.IsBound;
            }
        }

        private object SocketIDLock = new object();
        private object ListLock = new object();
        private int LastSocketID = 1;

        protected List<AsyncSocketClient> clientList;

        protected AsyncSocketClient _LastConnectedClient;
        public AsyncSocketClient LastConnectedClient
        {
            get
            {
                return _LastConnectedClient;
            }
            protected set
            {
                _LastConnectedClient = value;
            }
        }

        private Socket listener;
        protected string ServerName = string.Empty;
        protected string ServerIP = string.Empty;
        protected int ServerPort = 0;
        protected Exception SockException = null;
        protected bool bExitMonitor = false;

        // Event Handler
        public delegate void ClientDataRcvHandler(int CID, byte[] packet, int packetSize);
        public event ClientDataRcvHandler OnClientDataRcvd;

        public delegate void ClientAddedHandler(AsyncSocketClient client);
        public event ClientAddedHandler OnClientAdded;

        public delegate void ClientRemovedHandler(AsyncSocketClient client);
        public event ClientRemovedHandler OnClientRemoved;

        public event AsyncSocketErrorEventHandler OnServerErrorOccurred;

        public event AsyncSocketErrorEventHandler OnClientErrorOccurred;
        public event AsyncSocketConnectEventHandler OnClientConneted;
        public event AsyncSocketCloseEventHandler OnClientClosed;
        public event AsyncSocketSendEventHandler OnClientSent;
        //public event AsyncSocketReceiveEventHandler OnClientReceived;
        public event AsyncSocketAcceptEventHandler OnClientAccepted;

        public AsyncSocketServer()
        {
            clientList = new List<AsyncSocketClient>();
            Thread th = new Thread(new ThreadStart(MonitoringClientConnetion));
            th.Name = "Client Monitor";
            th.IsBackground = true;
            th.Start();
        }

        public void StopMonitoringClient()
        {
            bExitMonitor = true;
        }
        public void MonitoringClientConnetion()
        {
            while (!bExitMonitor)
            {
                try
                {
                    lock (ListLock)
                    {
                        foreach (var client in clientList)
                        {
                            if (client.SocketConnected)
                            {
                                ;
                            }
                            else
                            {
                                client.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                Thread.Sleep(1000);
            }
        }
        public List<AsyncSocketClient> GetClientList()
        {
            return clientList;
        }

        public void StartListen()
        {
            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, ServerPort));
                listener.Listen(backLog);

                StartAccept();
            }
            catch (System.Exception e)
            {
                ProcessServerError();
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(0, e);
                OnServerErrorOccurred?.Invoke(this, eev);
            }
        }
        public void StopListen()
        {
            try
            {
                if (listener != null)
                {
                    if (listener.IsBound)
                    {
                        listener.Close(100);
                        listener = null;
                    }

                    ProcessServerError();
                }
            }
            catch (System.Exception e)
            {
                ProcessServerError();
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(0, e);
                OnServerErrorOccurred?.Invoke(this, eev);
            }
        }
        private void StartAccept()
        {
            try
            {
                listener.BeginAccept(new AsyncCallback(OnListenCallBack), listener);
            }
            catch (System.Exception e)
            {
                ProcessServerError();
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(0, e);
                OnServerErrorOccurred?.Invoke(this, eev);
            }
        }

        /// <summary>
        /// Client의 비동기 접속을 처리한다.
        /// </summary>
        /// <param name="ar"></param>
        private void OnListenCallBack(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket worker = listener.EndAccept(ar);
                // Client를 Accept 했다고 Event를 발생시킨다.
                AsyncSocketAcceptEventArgs aev = new AsyncSocketAcceptEventArgs(worker);

                AsyncSocketClient ASC = new AsyncSocketClient(GetNextClientSocketID());
                //ASC.SetKeepAlive(worker, true, 1000, 200);
                ASC.Worker = worker;

                ASC.ReceiveTimeout = 5000;
                ASC.SendTimeout = 5000;

                ASC.OnConnet += new AsyncSocketConnectEventHandler(OnConnect);
                ASC.OnClose += new AsyncSocketCloseEventHandler(OnClose);
                ASC.OnError += new AsyncSocketErrorEventHandler(OnError);
                ASC.OnSend += new AsyncSocketSendEventHandler(OnSend);
                ASC.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);

                AddClientList(ASC);
                // 데이터 수신을 대기한다.
                ASC.Receive();
                ASC.SocketState = eSocketState.Connected;
                // 다시 새로운 클라이언트의 접속을 기다린다.
                StartAccept();

                OnClientAccepted?.Invoke(this, aev);
            }
            catch (ObjectDisposedException ode)
            {
                //소켓이 해제된 상태 접속 종료로 판단.
                _ = ode;
            }
            catch (System.Exception e)
            {
                ProcessServerError();
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(0, e);
                OnServerErrorOccurred?.Invoke(this, eev);
            }
        }

        public void InitServer(string name, string IP, int port)
        {
            this.ServerName = name;
            this.ServerIP = IP;
            this.ServerPort = port;
            StartListen();
        }

        private int GetNextClientSocketID()
        {
            lock (SocketIDLock)
            {
                LastSocketID++;
                if (LastSocketID >= 1000)
                {
                    LastSocketID = 1;
                }
                return LastSocketID;
            }
        }
        private void RemoveClientList(int CID)
        {
            lock (ListLock)
            {
                AsyncSocketClient targetClient = clientList.Where(c => c.ID == CID).FirstOrDefault();
                if (targetClient != null)
                {
                    clientList.Remove(targetClient);
                    OnClientRemoved?.Invoke(targetClient);
                }
            }
        }
        public AsyncSocketClient GetClientSocket(int CID)
        {
            lock (ListLock)
            {
                AsyncSocketClient targetClient = clientList.Where(c => c.ID == CID).FirstOrDefault();
                return targetClient;
            }
        }
        private void AddClientList(AsyncSocketClient client)
        {
            lock (ListLock)
            {
                clientList.Add(client);
                LastConnectedClient = client;
                OnClientAdded?.Invoke(client);
            }
        }
        protected void ProcessServerError()
        {
            lock (ListLock)
            {
                for (int i = clientList.Count; 0 < i; i--)
                {
                    clientList[i - 1].Close();
                    clientList.Remove(clientList.ElementAt(i - 1));
                }
            }
        }
        #region Client Socket private 메서드들
        protected virtual void OnConnect(object sender, AsyncSocketConnectionEventArgs e)
        {
            OnClientConneted?.Invoke(this, e);
        }
        protected virtual void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            RemoveClientList(e.ID);
            OnClientClosed?.Invoke(this, e);
        }
        protected virtual void OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            OnClientSent?.Invoke(this, e);
        }
        protected virtual void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            OnClientDataRcvd?.Invoke(e.ID, e.ReceiveData, e.ReceiveBytes);
        }
        protected virtual void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            RemoveClientList(e.ID);
            OnClientErrorOccurred?.Invoke(this, e);
        }

        #endregion
    }
}
