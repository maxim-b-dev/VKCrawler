﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace VKCrawler
{
    public class PauseTokenSource
    {
        private volatile TaskCompletionSource<bool> m_paused;
        internal static readonly Task s_completedTask = Task.FromResult(true);
        public PauseToken Token { get { return new PauseToken(this); } }
        public bool IsPaused
        {
            get { return m_paused != null; }
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(
                        ref m_paused, new TaskCompletionSource<bool>(), null);
                }
                else
                {
                    while (true)
                    {
                        var tcs = m_paused;
                        if (tcs == null) return;
                        if (Interlocked.CompareExchange(ref m_paused, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }
        internal Task WaitWhilePausedAsync()
        {
            var cur = m_paused;
            return cur != null ? cur.Task : s_completedTask;
        }
    }
    public struct PauseToken
    {
        private readonly PauseTokenSource m_source;
        internal PauseToken(PauseTokenSource source) { m_source = source; }

        public bool IsPaused { get { return m_source != null && m_source.IsPaused; } }

        public Task WaitWhilePausedAsync()
        {
            return IsPaused ?
                m_source.WaitWhilePausedAsync() :
                PauseTokenSource.s_completedTask;
        } 

    }
}
