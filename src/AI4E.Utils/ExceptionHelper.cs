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
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AI4E.Utils
{
    public sealed class ExceptionHelper
    {
        public static void HandleExceptions(Action action, ILogger? logger = null)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
#pragma warning disable CA1031
            catch (Exception exc)
#pragma warning restore CA1031
            {
                if (logger != null)
                {
                    logger.LogError(exc, "An exception occured unexpectedly.");
                }
                else
                {
                    Debug.WriteLine("An exception occured unexpectedly.");
                    Debug.WriteLine(exc.ToString());
                }
            }
        }

        public static T HandleExceptions<T>(Func<T> func, ILogger? logger = null, T defaultValue = default)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            try
            {
                return func();
            }
#pragma warning disable CA1031
            catch (Exception exc)
#pragma warning restore CA1031
            {
                LogException(exc, logger);
            }

            return defaultValue;
        }

        public static void LogException(Exception exc, ILogger? logger = null)
        {
            if (exc is null)
                throw new ArgumentNullException(nameof(exc));

            if (logger != null)
            {
                logger.LogError(exc, "An exception occured unexpectedly");
            }
            else
            {
                Debug.WriteLine("An exception occured unexpectedly.");
                Debug.WriteLine(exc.ToString());
            }
        }
    }
}
