using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IOCPSocket.Server
{
    /// <summary>
    /// 用户对象
    /// 一个socket的seed和receive 分别用一个SocketAsyncEventArgs
    /// </summary>
    public class AsyncUserToken
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bufferSize">缓存的长度</param>
        public AsyncUserToken(int bufferSize)
        {
            ConnectSocket = null;
            ReceiveEventArgs = new SocketAsyncEventArgs() { UserToken = this };
            AsyncReceiveBuffer = new byte[bufferSize];
            ReceiveEventArgs.SetBuffer(AsyncReceiveBuffer, 0, AsyncReceiveBuffer.Length);//设置接收缓冲区
            ReceiveBuffer = new List<byte>();
            temp = new Dictionary<string, object>();
        }
        /// <summary>
        /// 接收数据的SocketAsyncEventArgs
        /// </summary>
        public SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        /// <summary>
        /// 连接的Socket对象
        /// </summary>
        public Socket ConnectSocket { get; set; }
        /// <summary>
        /// 连接的时间
        /// </summary>
        public DateTime ConnectTime { get; set; }
        /// <summary>
        /// 数据最近接收更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
        /// <summary>
        /// 远程地址
        /// </summary>
        public EndPoint RemoteAddress { get; set; }
        /// <summary>
        /// 客户端IP地址
        /// </summary>
        public IPAddress IPAddress { get; set; }
        /// <summary>
        /// 接收数据的缓冲区
        /// </summary>
        public byte[] AsyncReceiveBuffer { get; set; }
        /// <summary>
        /// 动态接收数据
        /// </summary>
        public List<byte> ReceiveBuffer { get; set; }
        /// <summary>
        /// 是否接受完数据
        /// </summary>
        public bool IsReceiveOk { get; set; }
        /// <summary>
        /// 临时数据
        /// </summary>
        public Dictionary<string, Object> temp;
        /// <summary>
        /// 内部写一个接收的方法
        /// </summary>
        /// <returns></returns>
        public void Receive()
        {
            //把数据清楚完后，再接收新的数据
            //if (ConnectSocket.Available == 0 && IsReceiveOk == false)
            //{
            //    IsReceiveOk = true;
            //}
            //else if (ConnectSocket.Available == 0 && IsReceiveOk == true)
            //{
            //    ReceiveBuffer.Clear();
            //    IsReceiveOk = false;
            //}
            for (int i = 0; i < ReceiveEventArgs.BytesTransferred; i++)
            {
                ReceiveBuffer.Add(ReceiveEventArgs.Buffer[i]);
            }
            this.UpdateTime = DateTime.Now;
        }
    }
}
