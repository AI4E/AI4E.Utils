﻿/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018 Andreas Truetschel and contributors.
 * 
 * AI4E is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU Lesser General Public License as   
 * published by the Free Software Foundation, version 3.
 *
 * AI4E is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System.Runtime.CompilerServices;

namespace AI4E.Utils.Async
{
    [AsyncMethodBuilder(typeof(AsyncCovariantAwaitableMethodBuilder<>))]
    public interface ICovariantAwaitable<out TResult>
    {
        bool IsCompleted { get; }
        bool IsCompletedSuccessfully { get; }
        bool IsFaulted { get; }
        bool IsCanceled { get; }
        TResult Result { get; }
        ICovariantAwaiter<TResult> GetAwaiter();
        ICovariantAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext);
    }

    public interface ICovariantAwaiter<out TResult> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        bool IsCompletedSuccessfully { get; }
        bool IsFaulted { get; }
        bool IsCanceled { get; }
        TResult GetResult();
    }
}