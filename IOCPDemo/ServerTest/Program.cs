﻿using IOCPSocket;
using IOCPSocket.Server;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCPServerTest
{
    class Program
    {
        public static IOCPServer server;
        static void Main(string[] args)
        {
            Console.Title = "蓝创精英团队 IOCP Server Demo";
            server = new IOCPServer(9999, 100000, 3);
            server.OnReceive += new IOCPServer.ReceiveHandler(server_OnReceive);
            server.OnNewAccept += new IOCPServer.newAcceptHandler(newAcceptHandler);
            server.OnQuit += new IOCPServer.newQuitHandler(newQuitHandler);
            server.OnStart += new IOCPServer.ServerStart(ServerStart);
            server.Start();
            Console.ReadLine();
        }
        /// <summary>
        /// 收到信息
        /// </summary>
        /// <param name="args"></param>
        public static void server_OnReceive(AsyncUserToken args, byte[] bytes)
        {
            string data = Encoding.UTF8.GetString(bytes);
            var str = $"{data} {DateTime.Now.ToString()}";
            //Console.WriteLine("获取到的数据:" + data + "   " + DateTime.Now.ToString());
            server.Seed(args, Encoding.UTF8.GetBytes(str));
        }
        /// <summary>
        /// 新接入的用户
        /// </summary>
        /// <param name="UserToken"></param>
        public static void newAcceptHandler(AsyncUserToken UserToken)
        {
            //Console.WriteLine("一个新的用户:" + UserToken.RemoteAddress.ToString());
        }
        /// <summary>
        /// 退出用户
        /// </summary>
        /// <param name="UserToken"></param>
        public static void newQuitHandler(AsyncUserToken UserToken)
        {
            //Console.WriteLine("用户:" + UserToken.RemoteAddress.ToString() + "退出连接");
        }
        /// <summary>
        /// 服务启动
        /// </summary>
        public static void ServerStart()
        {
            Console.WriteLine("服务启动 端口:" + 9999);
        }
    }
}
