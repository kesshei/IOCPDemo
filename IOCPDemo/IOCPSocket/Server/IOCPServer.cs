using IOCPSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCPSocket
{
    /// <summary>
    /// 异步通讯类高级并发基类
    /// 异步接收，同步发送
    /// </summary>
    public class IOCPServer : IDisposable
    {
        /// <summary>
        /// 服务端负责监听的socket
        /// </summary>
        private Socket ListenerSocket { get; set; }
        /// <summary>
        /// 最大连接数
        /// </summary>
        private int MaxConnectNumber { get; set; }
        /// <summary>
        /// 最大接收字符数
        /// </summary>
        private int RevBufferSize { get; set; }
        /// <summary>
        /// 本地地址
        /// </summary>
        public IPEndPoint ServerlocaPoint { get; set; }
        /// <summary>
        /// 是否在运行
        /// </summary>
        public bool IsRunning = false;
        /// <summary>
        /// 是否释放了
        /// </summary>
        private bool disposed;
        /// <summary>
        /// 基于这个事件的委托
        /// </summary>
        /// <param name="UserToken"></param>
        public delegate void ReceiveHandler(AsyncUserToken UserToken);
        /// <summary>
        /// 一个接收的事件
        /// </summary>
        public event ReceiveHandler OnReceive;
        /// <summary>
        /// 新用户的委托
        /// </summary>
        /// <param name="UserToken"></param>
        public delegate void newAcceptHandler(AsyncUserToken UserToken);
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event newAcceptHandler OnNewAccept;
        /// <summary>
        /// 新用户的委托
        /// </summary>
        /// <param name="UserToken"></param>
        public delegate void newQuitHandler(AsyncUserToken UserToken);
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event newQuitHandler OnQuit;
        /// <summary>
        /// 开始服务的委托
        /// </summary>
        public delegate void ServerStart();
        /// <summary>
        /// 开始服务的事件
        /// </summary>
        public event ServerStart OnStart;
        /// <summary>
        /// 发送信息完成后的委托
        /// </summary>
        /// <param name="successorfalse"></param>
        public delegate void SendCompletedHandler(AsyncUserToken UserToken, int SeedLength);
        /// <summary>
        /// 发送信息完成后的事件
        /// </summary>
        public event SendCompletedHandler OnSended;
        /// <summary>
        /// 客户端列表
        /// </summary>
        public ConcurrentDictionary<string, AsyncUserToken> clients;
        /// <summary>
        /// 对象池
        /// </summary>
        private AsyncUserTokenPool _userTokenPool;
        /// <summary>
        /// 消息超时时间 默认一分钟
        /// </summary>
        public int MsgTimeOut { get; set; } = 1 * 60;
        /// <summary>
        /// 异步socket TCP服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大的客户端数量</param>
        public IOCPServer(int listenPort, int maxClient, int msgTimeOut = 1 * 60) : this(IPAddress.Any, listenPort, maxClient, msgTimeOut)
        { }
        /// <summary>
        /// 异步socket TCP服务器
        /// </summary>
        /// <param name="localIpaddress">监听的ip地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大的客户端数量</param>
        /// <param name="BufferSize">缓存的buffer</param>
        public IOCPServer(IPAddress localIpaddress, int listenPort, int maxClient, int msgTimeOut = 1 * 60, int BufferSize = 1024)
        {
            IsRunning = true;//服务状态变成  已在运行
            this.MsgTimeOut = msgTimeOut;
            disposed = false;
            clients = new ConcurrentDictionary<string, AsyncUserToken>();
            RevBufferSize = BufferSize;
            MaxConnectNumber = maxClient;
            ServerlocaPoint = new IPEndPoint(localIpaddress, listenPort);
            ListenerSocket = new Socket(localIpaddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _userTokenPool = new AsyncUserTokenPool(MaxConnectNumber, () =>
            {
                var userToken = new AsyncUserToken(RevBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                return userToken;
            });

            for (int i = 0; i < MaxConnectNumber; i++)
            {
                _userTokenPool.Push(_userTokenPool.New());
            }
        }
        /// <summary>
        /// 开始监听
        /// </summary>
        public void Start()
        {
            ListenerSocket.Bind(ServerlocaPoint);
            //开始监听
            ListenerSocket.Listen(MaxConnectNumber);
            //投递第一个接受的请求
            //一个异步socket事件
            StartAccept();
            if (OnStart != null)
            {
                OnStart();
            }
            _ = Task.Factory.StartNew(() =>
            {
                while (IsRunning)
                {
                    foreach (var item in clients.Values)
                    {
                        if ((DateTime.Now - item.UpdateTime).TotalSeconds > MsgTimeOut)
                        {
                            try
                            {
                                CloseClientSocket(item);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    SpinWait.SpinUntil(() => !IsRunning, 5 * 1000);
                }
            }, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// 开始接收新的异步请求
        /// </summary>
        /// <param name="Args"></param>
        private void StartAccept(SocketAsyncEventArgs Args = null)
        {
            if (Args == null)
            {
                Args = new SocketAsyncEventArgs();
                //接收的事件，要放在另外一个事件里
                Args.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEvent_Completed);
            }
            else
            {
                Args.AcceptSocket = null;
            }
            //如果 挂起，则会触发 OnIOCompleted 事件方法
            //否则，指定  接收的方法
            if (!ListenerSocket.AcceptAsync(Args))
            {
                ProcessAccept(Args);
            }
        }
        /// <summary>
        /// 只处理收到的请求的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        /// <summary>
        /// 当socket 上的发送或接收被完成时，调用此函数
        /// </summary>
        /// <param name="sender">激发事件的对象</param>
        /// <param name="e">与发送或接收完成操作相关联的socketAsyncEventArg对象</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (e.LastOperation == SocketAsyncOperation.Receive && e.SocketError == SocketError.Success)
            {
                ProcessReceive(e);
            }
            else if (e.SocketError == SocketError.ConnectionReset)
            {
                CloseClientSocket(userToken);
            }
        }
        /// <summary>
        /// 监听socket接收处理
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            //如果socket状态正常并且链接也正常
            if (e.SocketError == SocketError.Success && e.AcceptSocket.Connected)
            {
                //获取当前客户端连接的socket
                Socket socket = e.AcceptSocket;
                //获取一个这个对象的 token
                AsyncUserToken userToken = _userTokenPool.Pop();
                userToken.ConnectSocket = socket;
                userToken.ConnectTime = DateTime.Now;
                userToken.UpdateTime = DateTime.Now;
                userToken.RemoteAddress = e.AcceptSocket.RemoteEndPoint;
                userToken.IPAddress = ((IPEndPoint)(e.AcceptSocket.RemoteEndPoint)).Address;
                //更新客户列表
                clients.TryAdd(e.AcceptSocket.RemoteEndPoint.ToString(), userToken);
                if (OnNewAccept != null)
                {
                    OnNewAccept(userToken);
                }
                //开始投递 接收异步请求
                if (!socket.ReceiveAsync(userToken.ReceiveEventArgs))
                {
                    ProcessReceive(userToken.ReceiveEventArgs);
                }
                StartAccept(e);
            }
        }
        /// <summary>
        /// 已经收到消息
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (userToken.ConnectSocket != null && userToken.ConnectSocket.Connected && userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                Socket socket = userToken.ConnectSocket;
                userToken.Receive();
                if (socket.Available == 0)
                {
                    if (OnReceive != null)
                    {
                        OnReceive(userToken);
                    }
                    userToken.ReceiveBuffer.Clear();
                }
                if (!socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }
        /// <summary>
        /// 关闭已经出问题的客户端
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(AsyncUserToken userToken)
        {
            //移除这个客户信息
            if (clients.TryRemove(userToken.RemoteAddress.ToString(), out var _))
            {
                //先通知
                if (OnQuit != null)
                {
                    OnQuit(userToken);
                }
                try
                {
                    userToken.ConnectSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception){ }
                //直接断开
                try
                {
                    userToken.ConnectSocket.Close();//关闭客户端的socket
                }
                catch (Exception){}
                try
                {
                    //释放对象自己的数据
                    userToken.Dispose();
                    //传递参数
                    userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    _userTokenPool.Push(userToken);
                }
                catch (Exception){}
            }
            //开始接收新的请求
            StartAccept();
        }
        /// <summary>
        /// 直接发送数据
        /// </summary>
        /// <param name="token"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public void Seed(AsyncUserToken userToken, byte[] message)
        {
            int num = Seed(userToken.ConnectSocket, message);
            if (OnSended != null)
            {
                OnSended(userToken, num);
            }
        }
        /// <summary>
        /// 直接发送数据k
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public int Seed(Socket socket, byte[] message)
        {
            try
            {
                if (socket != null && socket.Connected != false)
                {
                    return socket.Send(message);
                }
            }
            catch (Exception) { };
            return -1;
        }
        /// <summary>
        /// 资源的释放
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            IsRunning = false;
            if (!this.disposed && disposed)
            {
                _userTokenPool.Dispose();
                //关闭其他客户端的信息
                foreach (var item in clients)
                {
                    try
                    {
                        item.Value.ConnectSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception) { }
                }
                //关闭服务器的信息
                try
                {
                    ListenerSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                //关闭监听的socket
                try
                {
                    ListenerSocket.Close();
                }
                catch (Exception){}
                //清空客户端列表
                clients.Clear();
                this.disposed = true;
            }
        }
    }
}
