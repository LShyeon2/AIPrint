using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BoxPrint.Communication
{
    /// <summary>
    /// 비동기 클라이언트 소켓
    /// </summary>
    public class AsyncSocketClient : AsyncSocket, INotifyPropertyChanged
    {
        eSocketState _SocketState = eSocketState.Disconnected;
        public eSocketState SocketState
        {
            get { return _SocketState; }
            set
            {
                _SocketState = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SocketState"));
            }
        }
        public string IPAddress
        {
            get
            {
                try
                {
                    if (worker != null)
                    {
                        return worker.RemoteEndPoint.ToString().Split(':')[0];
                    }
                    return "";
                }
                catch
                {
                    return "";
                }
            }
        }
        public string Port
        {
            get
            {
                try
                {
                    if (worker != null)
                    {
                        return worker.RemoteEndPoint.ToString().Split(':')[1];
                    }
                    return "";
                }
                catch
                {
                    return "";
                }
            }
        }
        public string ClientID
        {
            get
            {
                return _ID.ToString();
            }
        }
        ManualResetEvent MRE;

        public event PropertyChangedEventHandler PropertyChanged;


        public AsyncSocketClient(int id)
            : base(id)
        {
        }

        private Socket Connection
        {
            get { return this.worker; }
        }
        public bool SocketConnected
        {
            get
            {
                //https://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
                if (Connection != null && Connection.Connected)
                {
                    bool part1 = Connection.Poll(500, SelectMode.SelectRead);
                    bool part2 = Connection.Available == 0;
                    if ((part1 && part2) || !Connection.Connected)
                        return false;
                    else
                        return true;
                }
                return false;
            }
        }
        /// <summary>
        /// 연결을 시도한다.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Connect(string hostAddress, int port)
        {
            try
            {
                MRE = new ManualResetEvent(false);
                IPAddress[] ips = Dns.GetHostAddresses(hostAddress);
                IPEndPoint remoteEP = new IPEndPoint(ips[0], port);
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //SetKeepAlive(client,true, 1000, 200);
                client.BeginConnect(remoteEP, new AsyncCallback(OnConnectCallback), client);
                if (!MRE.WaitOne(3000))
                {
                    return false;
                }
            }
            catch (System.Exception e)
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);

                return false;
            }

            return true;

        }

        public bool Connect(string hostAddress, string port)
        {
            int Portaddress = -1;
            if (string.IsNullOrEmpty(hostAddress) || string.IsNullOrEmpty(port))
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, new Exception("호스트 주소 또는 포트번호 인자가 비어있거나 Null입니다."));
                ErrorOccurred(eev);
                return false;
            }
            if (int.TryParse(port, out Portaddress))
            {
                return Connect(hostAddress, Portaddress);
            }
            else
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, new Exception("포트번호 파싱에 실패하였습니다."));
                ErrorOccurred(eev);
                return false;
            }
        }

        /// <summary>
        /// 연결 요청 처리 콜백 함수
        /// </summary>
        /// <param name="ar"></param>
        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                // 보류 중인 연결을 완성한다.
                client.EndConnect(ar);
                worker = client;
                ReceiveTimeout = 5000; // Send - Receive에 대해 5초 타임아웃 지정
                SendTimeout = 5000;

                SocketState = eSocketState.Connected;

                // 연결에 성공하였다면, 데이터 수신을 대기한다.
                Receive();

                // 연결 성공 이벤트를 날린다.
                AsyncSocketConnectionEventArgs cev = new AsyncSocketConnectionEventArgs(this.ID, client.RemoteEndPoint.ToString());

                Connected(cev);
                MRE.Set();
            }
            catch (System.Exception e)
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }

        /// <summary>
        /// 데이터 수신을 비동기적으로 처리
        /// </summary>
        public void Receive()
        {
            try
            {
                Array.Clear(Buffer, 0, buffer.Length);
                Worker.BeginReceive(Buffer, 0, BufferSize, 0, new AsyncCallback(OnReceiveCallBack), this);
            }
            catch (ObjectDisposedException)
            {
                //소켓이 해제된 상태 접속 종료로 판단.
            }
            catch (System.Exception e)
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }

        /// <summary>
        /// 데이터 수신 처리 콜백 함수
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                AsyncSocket so = (AsyncSocket)ar.AsyncState;
                bool con = so.Worker.Connected;
                SocketError socketErrorCode;
                int bytesRead = so.Worker.EndReceive(ar, out socketErrorCode);
                if (socketErrorCode == SocketError.Success)
                {
                    AsyncSocketReceiveEventArgs rev = new AsyncSocketReceiveEventArgs(this.ID, bytesRead, so.Buffer);

                    // 데이터 수신 이벤트를 처리한다.
                    if (bytesRead > 0)
                    {
                        Received(rev);
                    }
                    else if (bytesRead == 0)
                    {
                        if (!SocketConnected) //넘어온 데이터가 없다면 상태 체크해본다.
                        {
                            Close();
                            //AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, new SocketException((int)socketErrorCode));
                            //ErrorOccurred(eev);
                        }
                        System.Threading.Thread.Sleep(16); //CPU 부하 감소를 위해 수신 데이터가 없으면 잠시 대기
                    }
                    // 다음 읽을 데이터를 처리한다.
                    Receive();
                }
                else
                {
                    Close();

                    AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, new SocketException((int)socketErrorCode));

                    ErrorOccurred(eev);
                }
            }
            catch (ObjectDisposedException ode)
            {
                //소켓이 해제된 상태 접속 종료로 판단.
                _ = ode;
            }
            catch (System.Exception e)
            {
                Close();
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }

        /// <summary>
        /// 데이터 송신을 비동기적으로 처리
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool Send(byte[] buffer, int size = 0)
        {
            try
            {
                Socket client = worker;
                if (size > 0)
                {
                    if (size > buffer.Length)
                    {
                        size = buffer.Length;
                    }
                    client.BeginSend(buffer, 0, size, 0, new AsyncCallback(OnSendCallBack), client);
                }
                else
                {
                    client.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(OnSendCallBack), client);
                }
            }
            catch (Exception e)
            {
                Close(); //전송시 에러발생하면 연결 해제.
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);
                ErrorOccurred(eev);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 데이터 송신 처리 콜백 함수
        /// </summary>
        /// <param name="ar"></param>
        private void OnSendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int bytesWritten = client.EndSend(ar);

                AsyncSocketSendEventArgs sev = new AsyncSocketSendEventArgs(this.ID, bytesWritten);

                Sent(sev);
            }
            catch (System.Exception e)
            {
                Close(); //전송시 에러발생하면 연결 해제.
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }

        /// <summary>
        /// 소켓 연결을 비동기적으로 종료
        /// </summary>
        public void Close()
        {
            try
            {
                SocketState = eSocketState.Disconnected;
                Socket client = worker;
                if (client != null)
                {
                    string remoteAddress = client.RemoteEndPoint.ToString();
                    client.Shutdown(SocketShutdown.Both);
                    client.BeginDisconnect(false, new AsyncCallback(OnCloseCallBack), client);
                }
            }
            catch (System.Exception e)
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }
        /// <summary>
        /// 일정 주기로 KeepAlive 패킷을 송신해서 연결 상태를 점검
        /// </summary>
        /// <param name="on"></param>
        /// <param name="keepAliveTime"></param>
        /// <param name="keepAliveInterval"></param>
        public void SetKeepAlive(Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = sizeof(UInt32);

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(inOptionValues, size * 2);

            socket?.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 소켓 연결 종료를 처리하는 콜백 함수
        /// </summary>
        /// <param name="ar"></param>
        private void OnCloseCallBack(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                string EndPoint = client.RemoteEndPoint.ToString();
                client.EndDisconnect(ar);
                client.Close();

                AsyncSocketConnectionEventArgs cev = new AsyncSocketConnectionEventArgs(this.ID, EndPoint);

                Closed(cev);
            }
            catch (System.Exception e)
            {
                AsyncSocketErrorEventArgs eev = new AsyncSocketErrorEventArgs(this.ID, e);

                ErrorOccurred(eev);
            }
        }

    } // end of class AsyncSocketClient
}
