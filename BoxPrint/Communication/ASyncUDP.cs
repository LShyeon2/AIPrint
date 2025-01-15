using BoxPrint.Log;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BoxPrint.Communication
{
    public class ASyncUDP
    {
        private const int ServerPointNumber = 5432;
        public IPEndPoint localEP;
        public EndPoint remoteEP;
        public Socket udpSocket;
        public byte[] Receive_Data;
        byte[] buffer = new byte[512];
        public bool ConnectedUDP = false;
        public ASyncUDP(string IPadress, string Port)
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Client IP 
            IPAddress Ripadress = IPAddress.Parse(IPadress);
            int port = Convert.ToInt32(Port);
            localEP = new IPEndPoint(IPAddress.Any, 0);
            remoteEP = new IPEndPoint(Ripadress, port);
        }
        public void initial()
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        public bool ConnectUDP()
        {
            try
            {
                udpSocket.Bind(localEP);

                udpSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(MessageCallBack), buffer);

                return ConnectedUDP = true;
            }
            catch (Exception ex)
            {
                initial();
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return ConnectedUDP = false;
            }

        }
        public void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                int size = udpSocket.EndReceiveFrom(aResult, ref remoteEP);

                if (size > 0)
                {
                    byte[] receiveData = new byte[512];
                    receiveData = (byte[])aResult.AsyncState;

                    string receiveMessage = Encoding.UTF8.GetString(receiveData, 0, size);

                    string tempread = "";
                    for (var i = 0; i < size; i++)
                    {
                        if (i == 15 || i == 31 || i == 47 || i == 63 || i == 79 || i == 95 || i == 111 || i == 127 ||
                            i == 139 || i == 155 || i == 171 || i == 187 || i == 203 || i == 219 || i == 235 || i == 251)
                        {
                            tempread = tempread + Convert.ToString(receiveData[i], 16) + "\r\n";
                        }
                        else
                        {
                            tempread = tempread + Convert.ToString(receiveData[i], 16) + " ";

                        }
                    }
                    Receive_Data = receiveData;
                }

                byte[] buffer = new byte[512];
                udpSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(MessageCallBack), buffer);
            }
            catch (Exception e)
            {
                ConnectedUDP = false;
                udpSocket.Close();
                LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
            }
        }

        public byte[] Send_Message(string SendData)
        {
            try
            {
                Byte[] datagram = new byte[SendData.Length / 2];
                string text = "";
                for (var i = 0; i < SendData.Length / 2; i++)
                {
                    datagram[i] = Convert.ToByte(SendData.Substring(i * 2, 2), 16);
                    text += " " + datagram[i];
                }

                udpSocket.SendTo(datagram, datagram.Length, SocketFlags.None, remoteEP);

                return Receive_Data;
            }
            catch (Exception e)
            {
                ConnectedUDP = false;
                udpSocket.Close();
                LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
                return Receive_Data;
            }
            finally
            {
                //udpSocket.Close();
            }
        }
    }
}
