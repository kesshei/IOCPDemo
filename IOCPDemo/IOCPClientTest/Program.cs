using IOCPSocket;
using System;
using System.Text;

namespace IOCPClientTest
{
    class Program
    {
        static IOCPClient client;
        static void Main(string[] args)
        {
            using (client = new IOCPClient("127.0.0.1", 9999))
            {
                Console.Title = "蓝创精英团队 IOCP Client Demo";
                client.OnReceive += new IOCPClient.ReceiveHandler(ReceiveHandler);
                client.OnClose += new IOCPClient.CloseHandler(CloseHandler);
                client.OnErr += new IOCPClient.ErrHandler(ErrHandler);
                client.OnSeeded += new IOCPClient.SeededHandler(SeededHandler);
                client.OnStart += new IOCPClient.StartHandler(StartHandler);
                client.ConnServer();
                client.Seed(Encoding.UTF8.GetBytes("客户端发送的信息"));
                client.Receive();

                Console.ReadLine();
            }
            Console.ReadLine();
        }
        public static void ReceiveHandler(byte[] data)
        {
            Console.WriteLine("服务端的信息:" + Encoding.UTF8.GetString(data));
        }
        public static void CloseHandler()
        {
            Console.WriteLine("服务已经关闭");
        }
        /// </summary>
        public static void ErrHandler(Exception e)
        {
            Console.WriteLine("发生了异常!" + e.ToString());
        }
        public static void SeededHandler()
        {
            Console.WriteLine("刚才的消息发送成功!");
        }
        public static void StartHandler()
        {
            Console.WriteLine("连接服务器成功:" + client.ServerEndPoint.ToString());
        }
    }
}
