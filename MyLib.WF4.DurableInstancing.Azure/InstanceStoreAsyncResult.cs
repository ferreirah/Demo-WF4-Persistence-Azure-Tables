using System;
using System.Threading;

namespace MyLib.WF4.DurableInstancing
{
    /// <summary>
    /// IAsyncResult sp�cifique permetant de faire remonter le r�sultat d'une commande
    /// </summary>
    internal class InstanceStoreAsyncResult : IAsyncResult
    {
        private readonly Object m_AsyncState;
        private readonly WaitHandle m_AsyncWaitHandle;
        private readonly Boolean m_Value;

        public InstanceStoreAsyncResult(IAsyncResult result, Boolean value)
        {
            m_AsyncState = result.AsyncState;
            m_AsyncWaitHandle = result.AsyncWaitHandle;
            m_Value = value;
        }

        public Object AsyncState { get { return m_AsyncState; } }
        public WaitHandle AsyncWaitHandle { get { return m_AsyncWaitHandle; } }
        public Boolean IsCompleted { get { return true; } }
        public Boolean CompletedSynchronously { get { return false; } }
        public Boolean Value { get { return m_Value; } }
    }
}
