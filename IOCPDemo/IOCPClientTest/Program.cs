using IOCPSocket;
using IOCPSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCPClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "蓝创精英团队 IOCP Client Demo";
            var list = new ConcurrentDictionary<long, IOCPClient>();
            Task.Run(async () =>
            {
                while (true)
                {
                    var temp = new ConcurrentBag<int>();
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    await Parallel.ForEachAsync(list.Values, async (client, b) =>
                    {
                        try
                        {
                            var result = client.Seed(Test);
                            temp.Add(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"异常退出:{ex.Message}");
                        }
                        await Task.CompletedTask;
                    });
                    stopwatch.Stop();
                    Console.WriteLine($"{DateTime.Now} 发送:{temp.Count} 有效:{temp.Count(t => t > 0)} 耗时:{stopwatch.ElapsedMilliseconds}");
                    Thread.Sleep(1000);
                }
            });
            Parallel.For(0, 10000, (a) =>
            {
                var client = new IOCPClient("127.0.0.1", 9999);
                client.OnReceive += new IOCPClient.ReceiveHandler(ReceiveHandler);
                client.OnClose += new IOCPClient.CloseHandler(CloseHandler);
                client.OnErr += new IOCPClient.ErrHandler(ErrHandler);
                client.OnStart += new IOCPClient.StartHandler(StartHandler);
                client.ConnServer();
                list.TryAdd(a, client);
            });

            Console.ReadLine();
        }
        static byte[] Test = Encoding.UTF8.GetBytes("客户端发送的信息");
        public static void ReceiveHandler(AsyncUserToken userToken, byte[] data)
        {
            //try
            //{
            //    //Thread.Sleep(1000);
            //    userToken.ConnectSocket.Send(Test);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"异常退出:{ex.Message}");
            //}
            //Console.WriteLine("服务端的信息:" + Encoding.UTF8.GetString(data));
        }
        public static void CloseHandler(AsyncUserToken userToken)
        {
            var client = userToken.temp["client"] as IOCPClient;
            client?.ReConnServer();
            Console.WriteLine("服务已经关闭");
        }
        /// </summary>
        public static void ErrHandler(AsyncUserToken userToken, Exception e)
        {
            var client = userToken.temp["client"] as IOCPClient;
            client?.ReConnServer();
            Console.WriteLine("发生了异常!" + e.ToString());
        }
        public static void StartHandler(AsyncUserToken userToken)
        {
            //  Console.WriteLine("连接服务器成功:" + userToken.RemoteAddress);
        }
    }
}
