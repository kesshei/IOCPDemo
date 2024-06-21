using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCPSocket.Server
{
    /// <summary>
    /// AsyncUserToken 对象池 （固定缓存+动态缓存 设计）
    /// 注：为了解决异常情况下，对象不够用的情况，特增加自己new的方式，以应对突发情况。
    /// </summary>
    public class AsyncUserTokenPool : IDisposable
    {
        /// <summary>
        /// 创建 UserToken的方式
        /// </summary>
        Func<AsyncUserToken> NewAsyncUserToken { get; set; }
        /// <summary>
        /// 后进先出的集合
        /// </summary>
        Stack<AsyncUserToken> m_pool;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">设置对象池的容量</param>
        public AsyncUserTokenPool(int capacity, Func<AsyncUserToken> newAsyncUserToken)
        {
            m_pool = new Stack<AsyncUserToken>(capacity);
            NewAsyncUserToken = newAsyncUserToken;
        }
        public AsyncUserToken New()
        {
            return NewAsyncUserToken();
        }
        /// <summary>
        /// 压入一个数据
        /// </summary>
        /// <param name="item"></param>
        public void Push(AsyncUserToken item)
        {
            if (item != null)
            {
                lock (m_pool)
                {
                    m_pool.Push(item);
                }
            }
        }
        /// <summary>
        /// 弹出一个
        /// </summary>
        /// <returns></returns>
        public AsyncUserToken Pop()
        {
            lock (m_pool)
            {
                if (m_pool.Any())
                {
                    return m_pool.Pop();
                }
                else
                {
                    return New();
                }
            }
        }
        /// <summary>
        /// 获取总和数
        /// </summary>
        public int Count
        {
            get { return m_pool.Count; }
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            m_pool.Clear();
        }
    }
}
