/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 - 2019-2019 Andreas Truetschel and contributors.
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AI4E.Utils.Async;

namespace AI4E.Proxying
{
    internal interface IProxy : IAsyncDisposable
    {
        object LocalInstance { get; }

        Type ObjectType { get; }
        int Id { get; }

        void Register(ProxyHost host, int proxyId, Action unregisterAction);
    }

    public interface IProxy<TRemote> : IAsyncDisposable
        where TRemote : class
    {
        TRemote LocalInstance { get; }

        Type ObjectType { get; }
        int Id { get; }

        Task ExecuteAsync(Expression<Action<TRemote>> expression);
        Task ExecuteAsync(Expression<Func<TRemote, Task>> expression);

        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, TResult>> expression);
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, Task<TResult>>> expression);
    }
}