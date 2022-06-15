using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCPSocket.Server
{
    /// <summary>
    /// AsyncUserToken 对象池 （固定缓存设计）
    /// </summary>
    public class AsyncUserTokenPool : IDisposable
    {
        /// <summary>
        /// 后进先出的集合
        /// </summary>
        Stack<AsyncUserToken> m_pool;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">设置对象池的容量</param>
        public AsyncUserTokenPool(int capacity)
        {
            m_pool = new Stack<AsyncUserToken>(capacity);
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
                return m_pool.Pop();
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
            do
            {
                this.Pop();
            }
            while (this.Count > 0);
        }
    }
}
