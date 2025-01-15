using BoxPrint;
using BoxPrint.Log;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//241205 HoN 비동기 방식 변경 (ManualResetEvent -> async/await)
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PLCProtocol.Base
{
    #region ManualResetEvent
    public class StateObject
    {
        public Socket WorkerSocket = null;
        public const int BufferSize = 8192;
        public byte[] Buffer = new byte[BufferSize];
        public int ReceiveCount = 0;
        public byte[] ReceiveData = new byte[BufferSize * 2];
        public int ID;
    }
    public class TCP_ASync
    {
        private ManualResetEvent MR_connectionEvent = new ManualResetEvent(false);
        private ManualResetEvent MR_SendEvent = new ManualResetEvent(false);
        private ManualResetEvent MR_ReceiveEvent = new ManualResetEvent(false);

        private Socket client = null;

        private string _hostAddress;
        private int _port;

        private object _readObject = new object();

        private byte[] _returnData = new byte[8192];

        public TCP_ASync(string ipaddress, int port)
        {
            this._hostAddress = ipaddress;
            this._port = port;

        }
        public Socket Connection
        {
            get { return this.client; }
            set { this.client = value; }
        }
        public bool SocketConnected
        {
            #region 소켓 연결 체크
            get
            {
                #region 기존 TCP 접속 테스트 
                //try
                //{
                //    if (Connection != null && Connection.Connected)
                //    {
                //        bool part1 = Connection.Poll(500, SelectMode.SelectRead);
                //        bool part2 = Connection.Available == 0;
                //        if ((part1 && part2) || !Connection.Connected)
                //            return false;
                //        else
                //            return true;
                //    }


                //    return false;
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.ToString());
                //    return false;
                //}
                #endregion

                try
                {
                    //https://stackoverflow.com/questions/6993295/how-to-determine-if-the-tcp-is-connected-or-not
                    if (Connection != null && Connection.Connected)
                    {
                        /* pear to the documentation on Poll:
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if (Connection.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (Connection.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            #endregion
        }

        public bool Connect()
        {
            try
            {
                IPAddress[] ips = Dns.GetHostAddresses(_hostAddress);

                IPEndPoint remoteEP = new IPEndPoint(ips[0], _port);

                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Console.WriteLine(string.Format("{0} Connecting....", remoteEP));
                MR_connectionEvent.Reset();

                client.BeginConnect(remoteEP, new AsyncCallback(OnConnectCallback), client);
                bool result = MR_connectionEvent.WaitOne(1000);
                if (result)
                {
                    return true;
                }
                else
                {
                    Close();
                    Console.WriteLine("{0} Connect Time Out !!", _hostAddress);
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Error(ex);
                return false;
            }
        }

        public void Close()
        {
            try
            {
                this.client.Close();
            }
            catch (System.Exception ex)
            {
                Error(ex);
            }
        }
        public bool CheckConnection()
        {
            if (client == null)
            {
                return false;
            }
            return this.SocketConnected;
        }
        //public byte[] Read(byte[] sendPacket)
        //{
        //    lock (_readObject)
        //    {
        //        Send(client, sendPacket);

        //        Receive(client);

        //        return _returnData;
        //    }
        //}
        //public byte[] Write(byte[] sendPacket, bool bRecieve = true)
        //{
        //    lock (_readObject)
        //    {
        //        Send(client, sendPacket);

        //        Receive(client);

        //        return _returnData;
        //    }
        //}
        public bool SendPacket(byte[] sendPacket, out byte[] Response)
        {
            lock (_readObject)
            {
                Send(client, sendPacket);

                bool RcvComp = Receive(client);
                if (RcvComp)
                {
                    Response = _returnData;
                }
                else
                {
                    Response = new byte[1];
                }
                return RcvComp;
            }
        }
        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                if (client.Connected == false)
                {
                    return;
                }

                // 보류 중인 연결을 완성한다.
                client.EndConnect(ar);

                MR_connectionEvent.Set();

                Console.WriteLine(string.Format("{0} Connect Complete", client.RemoteEndPoint));
            }
            catch (System.Exception ex)
            {
                Error(ex);
            }
        }
        public bool Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();
                state.WorkerSocket = client;
                MR_ReceiveEvent.Reset();
                client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnReceiveCallBack), state);
                bool recv = MR_ReceiveEvent.WaitOne(1500); //241127 RGJ 500ms-> 1000ms -> 1500ms  변경.0.5 초는 FA망 상황에 따라 보장안될수도 있음. 500ms 추가함 
                if (!recv) //응답 안오면 연결 끊지않고 실패처리 한다.
                {
                    //throw new Exception("Socket 응답 시간 초과");
                    LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "{0}:{1} Socket Response Timeout!", _hostAddress, _port);
                }
                return recv;
            }
            catch (System.Exception ex)
            {
                Error(ex);
                return false;
            }

        }
        private void OnReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.WorkerSocket;

                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    Array.Copy(state.Buffer, 0, state.ReceiveData, state.ReceiveCount, bytesRead);
                    state.ReceiveCount += bytesRead;

                    Array.Clear(state.Buffer, 0, state.Buffer.Length);
                }

                if (client.Available > 0)
                {
                    client.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, new AsyncCallback(OnReceiveCallBack), state);
                }
                else
                {
                    Array.Copy(state.ReceiveData, 0, _returnData, 0, state.ReceiveCount);

                    if (GlobalData.Current.WritePLCRawLog) //임시로그
                    {
                        LogManager.WritePLCLog("Recv =>{0}  {1}", _hostAddress, BitConverter.ToString(_returnData));
                    }

                    state.ReceiveCount = 0;
                    Array.Clear(state.ReceiveData, 0, state.ReceiveData.Length);


                    MR_ReceiveEvent.Set();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Error(ex);
            }
            catch (System.Exception ex)
            {
                Error(ex);
            }
        }
        private void Send(Socket client, byte[] buffer)
        {
            try
            {
                MR_SendEvent.Reset();
                client.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(OnSendCallBack), client);
                MR_SendEvent.WaitOne();
            }
            catch (System.Exception ex)
            {
                Error(ex);
            }
        }
        private void OnSendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int bytesWritten = client.EndSend(ar);

                MR_SendEvent.Set();
            }
            catch (System.Exception ex)
            {
                Error(ex);
            }
        }
        private void Error(Exception ex)
        {
            LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "TCP_ASync Socket Error Occurred  {0}:{1}", _hostAddress, _port);
            LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, ex.ToString());
            Close();
        }

        public byte[] Write(byte[] reqData)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
    //241205 HoN 비동기 방식 변경 (ManualResetEvent -> async/await)
    #region async/await
    //public class TCP_ASync
    //{
    //    #region Variable
    //    /// <summary>
    //    /// Lock
    //    /// </summary>
    //    private readonly object _socketLock = new object();
    //    /// <summary>
    //    /// 소켓 IP
    //    /// </summary>
    //    private string _hostAddress;
    //    /// <summary>
    //    /// 소켓 포트
    //    /// </summary>
    //    private int _port;
    //    /// <summary>
    //    /// 소켓 엔드포인트
    //    /// </summary>
    //    private IPEndPoint remoteEP;
    //    /// <summary>
    //    /// 소켓
    //    /// </summary>
    //    private Socket client = null;
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    private byte[] _returnData = new byte[0];
    //    /// <summary>
    //    /// TimeOut 대기시간
    //    /// </summary>
    //    private const int DefaultTimeout = 3000;
    //    /// <summary>
    //    /// 소켓 연결 체크 대기시간
    //    /// </summary>
    //    private const int PollTimeout = 1000;
    //    /// <summary>
    //    /// 1회 리딩 사이즈
    //    /// </summary>
    //    private const int OnceReadSize = 8192;
    //    /// <summary>
    //    /// 소켓 연결 여부
    //    /// </summary>
    //    public bool SocketConnected
    //    {
    //        get
    //        {
    //            //lock (_socketLock) // 동기화 유지
    //            {
    //                try
    //                {
    //                    if (client == null || !client.Connected)
    //                        return false;

    //                    //연결이 종료된 소켓 상태 확인
    //                    //기존 SocketConnected에 있었던 Receive가 비동기 Receive와 동시처리될경우 문제가 발생할 여지가 1이라도 존재함.
    //                    return !(client.Poll(0, SelectMode.SelectRead) && client.Available == 0);
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine(ex.ToString());
    //                    return false;
    //                }
    //            }
    //        }
    //    }
    //    #endregion
    //    #region Methods
    //    #region Constructor
    //    /// <summary>
    //    /// 생성자
    //    /// </summary>
    //    /// <param name="ipaddress">Socket IP</param>
    //    /// <param name="port">Socket Port</param>
    //    public TCP_ASync(string ipaddress, int port)
    //    {
    //        _hostAddress = ipaddress;
    //        _port = port;

    //        IPAddress[] ips = Dns.GetHostAddresses(_hostAddress);
    //        remoteEP = new IPEndPoint(ips[0], _port);
    //    }
    //    #endregion
    //    #region Socket Async
    //    /// <summary>
    //    /// 비동기 소켓 연결
    //    /// </summary>
    //    public async Task<bool> ConnectAsync(Socket socket, EndPoint rep, int timeoutMilliseconds = DefaultTimeout)
    //    {
    //        try
    //        {
    //            LogManager.WritePLCLog($"{rep} Connect Start");

    //            using (CancellationTokenSource cts = new CancellationTokenSource(timeoutMilliseconds))
    //            {
    //                try
    //                {
    //                    var socketTask = Task.Run(() =>
    //                    {
    //                        socket.Connect(rep);
    //                    }, cts.Token);

    //                    await socketTask.ConfigureAwait(false);

    //                    LogManager.WritePLCLog($"{rep} Connect {(SocketConnected ? "Complete" : "Fail")}");
    //                    return SocketConnected;
    //                }
    //                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    //                {
    //                    LogManager.WritePLCLog($"{rep} Connect Timeout");
    //                    Close();
    //                    return false;
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Error(ex);
    //            Close();
    //            return false;
    //        }
    //    }
    //    /// <summary>
    //    /// 비동기 데이터 쓰기
    //    /// </summary>
    //    private async Task<bool> SendAsync(Socket client, byte[] buffer, int timeoutMilliseconds = DefaultTimeout)
    //    {
    //        try
    //        {
    //            LogManager.WritePLCLog($"\t\tSend Start");

    //            using (CancellationTokenSource cts = new CancellationTokenSource(timeoutMilliseconds))  //지정시간 후 타임아웃
    //            {
    //                using (NetworkStream ns = new NetworkStream(client))
    //                {
    //                    try
    //                    {
    //                        //바이너리 데이터를 바로 기재하기에 NetworkStream단에서 바로 처리
    //                        //JSon, 문자열 바로 기입이 필요하면 using (StreamWriter sw = new StreamWriter(ns))으로 재할당해서 sw.WriteAsync or sw.WriteLineAsync로 사용
    //                        await ns.WriteAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false);
    //                        LogManager.WritePLCLog($"\t\tSend Complete");
    //                        return true;
    //                    }
    //                    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    //                    {
    //                        LogManager.WritePLCLog($"\t\tSend Timeout");
    //                        return false;
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Error(ex);
    //            return false;
    //        }
    //    }
    //    /// <summary>
    //    /// 비동기 데이터 읽기
    //    /// </summary>
    //    private async Task<bool> ReceiveAsync(Socket client, int timeoutMilliseconds = DefaultTimeout)
    //    {
    //        try
    //        {
    //            LogManager.WritePLCLog($"\t\tReceive Start");

    //            List<byte> receiveData = new List<byte>();

    //            using (NetworkStream ns = new NetworkStream(client))
    //            {
    //                while (true)
    //                {
    //                    using (CancellationTokenSource cts = new CancellationTokenSource(timeoutMilliseconds))
    //                    {
    //                        //소켓이 연결되어 있지 않다면 종료
    //                        if (!SocketConnected)
    //                        {
    //                            LogManager.WritePLCLog("\t\tSocket Disconnect.");
    //                            return false;
    //                        }

    //                        byte[] buffer = new byte[OnceReadSize];
    //                        try
    //                        {
    //                            int bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false);

    //                            //0개를 읽어올 수 없음
    //                            if (bytesRead.Equals(0))
    //                            {
    //                                LogManager.WritePLCLog($"\t\tReceive Size is Zero");
    //                                return false;
    //                            }

    //                            receiveData.AddRange(buffer.Take(bytesRead));

    //                            if (!ns.DataAvailable)      //더 읽을 데이터가 없다
    //                            {
    //                                LogManager.WritePLCLog($"\t\tNot Exist More Receive Data");
    //                                break;
    //                            }

    //                            LogManager.WritePLCLog($"\t\tExist More Receive Data");
    //                        }
    //                        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    //                        {
    //                            LogManager.WritePLCLog($"\t\tReceive Timeout");
    //                            return false;
    //                        }
    //                    }
    //                }
    //            }

    //            LogManager.WritePLCLog("\t\tReceive Complete");
    //            _returnData = receiveData.ToArray();
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            Error(ex);
    //            return false;
    //        }
    //    }
    //    #endregion
    //    #region Etc
    //    /// <summary>
    //    /// 에러 발생시 로그 기재용
    //    /// </summary>
    //    /// <param name="ex"></param>
    //    private void Error(Exception ex)
    //    {
    //        LogManager.WritePLCLog("\t\tTCP_ASync Socket Error Occurred {0}:{1}", _hostAddress, _port);
    //        LogManager.WritePLCLog("\t\t" + ex.ToString());

    //        if (client != null && (!SocketConnected || ex is SocketException))
    //        {
    //            Close(); // 연결이 끊어진 경우에만 닫기
    //        }
    //    }
    //    #endregion
    //    #region Public Call
    //    /// <summary>
    //    /// 소켓 연결
    //    /// </summary>
    //    /// <returns></returns>
    //    public bool Connect()
    //    {
    //        try
    //        {
    //            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //            LogManager.WritePLCLog($"Socket {remoteEP} Create!!");
    //            var connectAsync = ConnectAsync(client, remoteEP);

    //            return connectAsync.Result;
    //        }
    //        catch (Exception ex)
    //        {
    //            Error(ex);
    //            return false;
    //        }
    //    }
    //    /// <summary>
    //    /// 소켓 닫기
    //    /// </summary>
    //    public void Close()
    //    {
    //        //lock (_socketLock) // 소켓 동기화
    //        {
    //            try
    //            {
    //                client?.Close();
    //            }
    //            catch (Exception ex)
    //            {
    //                Error(ex);
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 데이터 리딩 호출자
    //    /// </summary>
    //    /// <param name="sendPacket">기재 커맨드</param>
    //    /// <param name="Response">응답</param>
    //    /// <returns></returns>
    //    public bool SendPacket(byte[] sendPacket, out byte[] Response)
    //    {
    //        lock (_socketLock)
    //        {
    //            try
    //            {
    //                LogManager.WritePLCLog($"\tPacket Start");
    //                Response = new byte[0];     //초기값 할당
    //                _returnData = new byte[0];  //초기화
    //                byte[] response = new byte[0];

    //                // 비동기 작업을 별도 스레드에서 실행
    //                var result = Task.Run(async () =>
    //                {
    //                    // 데이터 전송
    //                    if (!await SendAsync(client, sendPacket))
    //                        return false;

    //                    // 데이터 수신
    //                    if (!await ReceiveAsync(client))
    //                        return false;

    //                    response = _returnData;
    //                    return true;
    //                }).Result; // 동기적으로 결과를 기다림

    //                Response = response;
    //                return result; // 성공 여부 반환
    //            }
    //            catch (AggregateException ex) when (ex.InnerException != null)
    //            {
    //                LogManager.WritePLCLog("\tAggregateException: {0}", ex.InnerException?.Message ?? ex.Message);
    //                Response = new byte[0];
    //                return false;
    //            }
    //            catch (Exception ex)
    //            {
    //                LogManager.WritePLCLog("\tError SendPacket: {0}", ex.ToString());
    //                Response = new byte[0];
    //                return false;
    //            }
    //            finally
    //            {
    //                LogManager.WritePLCLog($"\tPacket Complete");
    //            }
    //        }
    //    }
    //    #endregion
    //    #endregion
    //}
    #endregion
}
