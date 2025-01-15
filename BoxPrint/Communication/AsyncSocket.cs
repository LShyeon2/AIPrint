using System.Net.Sockets;
namespace BoxPrint.Communication
{
    public abstract class AsyncSocket
    {
        public int ID
        {
            get { return this._ID; }
        }
        protected int _ID;
        protected const int BUFFER_SIZE = 4096;

        protected Socket worker;
        protected byte[] buffer;
        public int SendTimeout
        {
            get { return (int)worker.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout); }
            set { worker.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value); }
        }

        public int ReceiveTimeout
        {
            get { return (int)worker.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout); }
            set { worker.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value); }
        }

        public AsyncSocket(int id)
        {
            this._ID = id;
            this.buffer = new byte[BUFFER_SIZE];
        }
        public Socket Worker
        {
            get { return this.worker; }
            set { this.worker = value; }
        }

        public byte[] Buffer
        {
            get { return this.buffer; }
            set { this.buffer = value; }
        }

        public int BufferSize
        {
            get { return BUFFER_SIZE; }
        }

        // Event Handler
        public event AsyncSocketErrorEventHandler OnError;
        public event AsyncSocketConnectEventHandler OnConnet;
        public event AsyncSocketCloseEventHandler OnClose;
        public event AsyncSocketSendEventHandler OnSend;
        public event AsyncSocketReceiveEventHandler OnReceive;
        public event AsyncSocketAcceptEventHandler OnAccept;

        protected virtual void ErrorOccurred(AsyncSocketErrorEventArgs e)
        {
            OnError?.Invoke(this, e);
        }

        protected virtual void Connected(AsyncSocketConnectionEventArgs e)
        {
            OnConnet?.Invoke(this, e);
        }

        protected virtual void Closed(AsyncSocketConnectionEventArgs e)
        {
            OnClose?.Invoke(this, e);
        }

        protected virtual void Sent(AsyncSocketSendEventArgs e)
        {
            OnSend?.Invoke(this, e);
        }

        protected virtual void Received(AsyncSocketReceiveEventArgs e)
        {
            OnReceive?.Invoke(this, e);
        }

        protected virtual void Accepted(AsyncSocketAcceptEventArgs e)
        {
            OnAccept?.Invoke(this, e);
        }
    } // end of class AsyncSocket
}
