using IOCPSocket.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IOCPSocket
{
    /// <summary>
    /// 一个客户端
    /// </summary>
    public class IOCPClient
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
        public delegate void ReceiveHandler(AsyncUserToken UserToken, byte[] data);
        /// <summary>
        /// 接收数据的时间
        /// </summary>
        public event ReceiveHandler OnReceive;
        /// <summary>
        /// 开始的委托
        /// </summary>
        public delegate void StartHandler(AsyncUserToken UserToken);
        /// <summary>
        /// 开始的事件
        /// </summary>
        public event StartHandler OnStart;
        /// <summary>
        /// 关闭事件的委托
        /// </summary>
        public delegate void CloseHandler(AsyncUserToken UserToken);
        /// <summary>
        /// 关闭事件
        /// </summary>
        public event CloseHandler OnClose;
        /// <summary>
        /// 异常错误事件
        /// </summary>
        public delegate void ErrHandler(AsyncUserToken UserToken, Exception e);
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
        private AsyncUserToken userToken;
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
            _clientSock.Connect(ServerEndPoint);
            userToken = new AsyncUserToken();
            userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            userToken.ConnectSocket = _clientSock;
            userToken.ConnectTime = DateTime.Now;
            userToken.UpdateTime = DateTime.Now;
            userToken.RemoteAddress = userToken.ConnectSocket.RemoteEndPoint;
            userToken.IPAddress = ((IPEndPoint)(userToken.ConnectSocket.RemoteEndPoint)).Address;
            userToken.temp["client"] = this;
            try
            {
                IsRuning = true;
                if (OnStart != null)
                {
                    OnStart(userToken);
                }
                ProcessReceiveCore(userToken.ReceiveEventArgs);
            }
            catch (Exception e) { IsRuning = false; if (OnErr != null) { OnErr(userToken, e); } }
        }
        public void ReConnServer()
        {
            //直接断开
            try
            {
                _clientSock.Close();//关闭客户端的socket
            }
            catch (Exception) { }
            ConnServer();
            Console.WriteLine("已重连");
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public int Seed(byte[] data)
        {
            try
            {
                return _clientSock.Send(data);
            }
            catch (Exception ex)
            {
                IsRuning = false;
                if (OnErr != null)
                {
                    OnErr(userToken, ex);
                }
            }
            return -1;
        }
        /// <summary>
        /// 当socket 上的发送或接收被完成时，调用此函数
        /// </summary>
        /// <param name="sender">激发事件的对象</param>
        /// <param name="e">与发送或接收完成操作相关联的socketAsyncEventArg对象</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    // ProcessReceiveCore(e);
                    break;
                default:
                    //CloseClientSocket(userToken, $"OnIOCompleted:{e.LastOperation}");
                    break;
            }
        }
        private void ProcessReceiveCore(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            Socket socket = userToken.ConnectSocket;
            if (!socket.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }
        /// <summary>
        /// 已经收到消息
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            Socket socket = userToken.ConnectSocket;
            if (socket != null && socket.Connected && userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                userToken.Receive();
                if (socket.Available == 0)
                {
                    if (OnReceive != null)
                    {
                        var datas = userToken.ReceiveBuffer.ToArray();
                        Task.Run(() =>
                        {
                            this.OnReceive(userToken, datas);
                        });
                    }
                    userToken.ReceiveBuffer.Clear();
                }
                ProcessReceiveCore(e);
            }
            else
            {
                CloseClientSocket(userToken, "接收状态异常");
            }
        }
        /// <summary>
        /// 关闭已经出问题的客户端
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(AsyncUserToken userToken, string msg)
        {
            Console.WriteLine(msg);
            try
            {
                userToken.ConnectSocket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            //直接断开
            try
            {
                userToken.ConnectSocket.Close();//关闭客户端的socket
            }
            catch (Exception) { }

            IsRuning = false;

            if (OnClose != null)
            {
                OnClose(userToken);
            }
            try
            {
                //释放对象自己的数据
                // userToken.Dispose();
            }
            catch (Exception) { }
        }
    }
}