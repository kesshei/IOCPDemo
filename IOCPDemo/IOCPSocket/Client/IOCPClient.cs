using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace IOCPSocket
{
    /// <summary>
    /// 一个客户端
    /// </summary>
    public class IOCPClient : IDisposable
    {
        /// <summary>
        /// 客户端socket
        /// </summary>
        private Socket _clientSock;
        /// <summary>
        /// 服务端 ip信息
        /// </summary>
        public IPEndPoint ServerEndPoint;
        /// <summary>
        /// 接收的委托
        /// </summary>
        /// <param name="e"></param>
        public delegate void ReceiveHandler(byte[] data);
        /// <summary>
        /// 接收数据的时间
        /// </summary>
        public event ReceiveHandler OnReceive;
        /// <summary>
        /// 开始的委托
        /// </summary>
        public delegate void StartHandler();
        /// <summary>
        /// 开始的事件
        /// </summary>
        public event StartHandler OnStart;
        /// <summary>
        /// 关闭事件的委托
        /// </summary>
        public delegate void CloseHandler();
        /// <summary>
        /// 关闭事件
        /// </summary>
        public event CloseHandler OnClose;
        /// <summary>
        /// 发送事件的委托
        /// </summary>
        public delegate void SeededHandler();
        /// <summary>
        /// 发送完毕后的事件
        /// </summary>
        public event SeededHandler OnSeeded;
        /// <summary>
        /// 异常错误事件
        /// </summary>
        public delegate void ErrHandler(Exception e);
        /// <summary>
        /// 异常事件
        /// </summary>
        public event ErrHandler OnErr;
        /// <summary>
        /// 是否在运行
        /// </summary>
        public bool IsRuning = false;
        private byte[] buffer;
        private List<byte> bufferData;
        /// <summary>
        /// 客户端socket
        /// </summary>
        /// <param name="serverIp">服务端ip</param>
        /// <param name="serverPort">服务端端口</param>
        /// <param name="encoding">编码</param>
        public IOCPClient(string serverIp, int serverPort, int bufferSize = 1024)
        {
            _clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            buffer = new byte[bufferSize];
            bufferData = new List<byte>();
        }
        /// <summary>
        /// 开始连接服务器
        /// </summary>
        public void ConnServer()
        {
            _clientSock.BeginConnect(ServerEndPoint, new AsyncCallback(ConnectCallback), IsRuning);
        }
        /// <summary>
        /// 异步连接的回调
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _clientSock.EndConnect(ar);
                IsRuning = true;
                if (OnStart != null)
                {
                    OnStart();
                }
            }
            catch (Exception e) { IsRuning = false; if (OnErr != null) { OnErr(e); } }
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public void Seed(byte[] data)
        {
            _clientSock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);

        }
        //发送数据，以防发送少了。。
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                if (_clientSock.Connected)
                {
                    _clientSock.EndSend(ar);
                    if (OnSeeded != null) { OnSeeded(); }
                }
            }
            catch (Exception e) { if (OnErr != null) { OnErr(e); } }
        }
        /// <summary>
        /// 接收
        /// </summary>
        public void Receive()
        {
            if (_clientSock.Connected)
            {
                _clientSock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
        }
        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (_clientSock.Connected)
                {
                    int count = _clientSock.EndReceive(ar);
                    for (int i = 0; i < count; i++)
                    {
                        bufferData.Add(buffer[i]);
                    }
                    if (this.OnReceive != null)
                    {
                        if (_clientSock.Available == 0)
                        {
                            if (bufferData.Count == 0)
                            {
                                this.Dispose();
                                return;
                            }
                            this.OnReceive(bufferData.ToArray());
                            bufferData.Clear();
                        }
                    }
                    this.Receive();
                }
            }
            catch (Exception e) { if (OnErr != null) { OnErr(e); } }
        }
        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Dispose()
        {
            IsRuning = false;
            _clientSock.Shutdown(SocketShutdown.Send);
            _clientSock.Close();
            if (OnClose != null)
            {
                OnClose();
            }
        }
    }
}