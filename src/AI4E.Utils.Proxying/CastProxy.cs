/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 Andreas Truetschel and contributors.
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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying
{
    internal sealed class CastProxy<TRemote, TCast> : IProxy<TCast>
        where TRemote : class
        where TCast : class
    {
        public CastProxy(Proxy<TRemote> original)
        {
            Original = original;
        }

        public TCast LocalInstance => Original.IsRemoteProxy ? null : (TCast)(object)Original.LocalInstance;

        object IProxy.LocalInstance => LocalInstance;

        public ValueTask<Type> GetObjectTypeAsync(CancellationToken cancellation)
        {
            return Original.GetObjectTypeAsync(cancellation);
        }

        public Type RemoteType => typeof(TCast);

        public int Id => Original.Id;

        internal Proxy<TRemote> Original { get; }

        private Expression<TDelegate> ConvertExpression<TDelegate>(LambdaExpression expression)
            where TDelegate : Delegate
        {
            var parameter = expression.Parameters.First();
            var body = expression.Body;

            var newParameter = Expression.Parameter(typeof(TRemote));
            var newBody = ParameterExpressionReplacer.ReplaceParameter(body, parameter, newParameter);
            return Expression.Lambda<TDelegate>(newBody, newParameter);
        }

        public Task ExecuteAsync(Expression<Action<TCast>> expression)
        {
            return Original.ExecuteAsync(ConvertExpression<Action<TRemote>>(expression));
        }

        public Task ExecuteAsync(Expression<Func<TCast, Task>> expression)
        {
            return Original.ExecuteAsync(ConvertExpression<Func<TRemote, Task>>(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression<Func<TCast, TResult>> expression)
        {
            return Original.ExecuteAsync(ConvertExpression<Func<TRemote, TResult>>(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression<Func<TCast, Task<TResult>>> expression)
        {
            return Original.ExecuteAsync(ConvertExpression<Func<TRemote, Task<TResult>>>(expression));
        }

        public IProxy<T> Cast<T>() where T : class
        {
            return Original.Cast<T>();
        }

        public void Dispose()
        {
            Original.Dispose();
        }

        public Task DisposeAsync()
        {
            return Original.DisposeAsync();
        }

        public Task Disposal => Original.Disposal;

        public TCast AsTransparentProxy()
        {
            return Original.AsTransparentProxy<TCast>();
        }
    }
}
