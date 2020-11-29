// 
// Copyright (c) 2004-2019 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NLog.LiteDB.Extensions
{
    public static class AsyncHelper
    {
        /// Pulled from Nlog.Common.AsyncHelpers.cs


        /// <summary>
        /// Disposes the Timer, and waits for it to leave the Timer-callback-method
        /// </summary>
        /// <param name="timer">The Timer object to dispose</param>
        /// <param name="timeout">Timeout to wait (TimeSpan.Zero means dispose without wating)</param>
        /// <returns>Timer disposed within timeout (true/false)</returns>
        internal static bool WaitForDispose(this Timer timer, TimeSpan timeout)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (timeout != TimeSpan.Zero)
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                if (timer.Dispose(waitHandle) && !waitHandle.WaitOne((int)timeout.TotalMilliseconds))
                {
                    return false;   // Return without waiting for timer, and without closing waitHandle (Avoid ObjectDisposedException)
                }

                waitHandle.Close();
            }
            else
            {
                timer.Dispose();
            }
            return true;
        }
    }
}

