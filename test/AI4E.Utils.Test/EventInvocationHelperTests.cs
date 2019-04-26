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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class EventInvocationHelperTests
    {
        [TestMethod]
        public void InvokeTest()
        {
            var actionInvoked = new bool[3];

            Action @delegate = null;

            void Action1()
            {
                actionInvoked[0] = true;
            }

            void Action2()
            {
                actionInvoked[1] = true;
            }

            void Action3()
            {
                actionInvoked[2] = true;
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            EventInvocationHelper.InvokeAll(@delegate, d => d());

            Assert.IsTrue(actionInvoked.All());
        }

        [TestMethod]
        public void InvokeExceptionTest()
        {
            var actionInvoked = new bool[3];

            Action @delegate = null;

            void Action1()
            {
                actionInvoked[0] = true;
            }

            void Action2()
            {
                actionInvoked[1] = true;
                throw new CustomException();
            }

            void Action3()
            {
                actionInvoked[2] = true;
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            Assert.ThrowsException<CustomException>(() =>
            {
                EventInvocationHelper.InvokeAll(@delegate, d => d());
            });

            Assert.IsTrue(actionInvoked.All());
        }

        [TestMethod]
        public void InvokeMultipleExceptionTest()
        {
            var actionInvoked = new bool[3];

            Action @delegate = null;

            void Action1()
            {
                actionInvoked[0] = true;
            }

            void Action2()
            {
                actionInvoked[1] = true;
                throw new CustomException();
            }

            void Action3()
            {
                actionInvoked[2] = true;
                throw new CustomException2();
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            var exception = Assert.ThrowsException<AggregateException>(() =>
            {
                EventInvocationHelper.InvokeAll(@delegate, d => d());
            });

            Assert.IsTrue(actionInvoked.All());
            Assert.AreEqual(2, exception.InnerExceptions.Count());
            Assert.IsTrue(exception.InnerExceptions.Any(p => p is CustomException));
            Assert.IsTrue(exception.InnerExceptions.Any(p => p is CustomException2));
        }

        [TestMethod]
        public async Task InvokeAsyncTest()
        {
            var actionInvoked = new bool[3];

            Func<ValueTask> @delegate = null;

            ValueTask Action1()
            {
                actionInvoked[0] = true;
                return default;
            }

            ValueTask Action2()
            {
                actionInvoked[1] = true;
                return default;
            }

            ValueTask Action3()
            {
                actionInvoked[2] = true;
                return default;
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            await EventInvocationHelper.InvokeAllAsync(@delegate, d => d());

            Assert.IsTrue(actionInvoked.All());
        }

        [TestMethod]
        public async Task InvokeAsyncExceptionTest()
        {
            var actionInvoked = new bool[3];

            Func<ValueTask> @delegate = null;

            ValueTask Action1()
            {
                actionInvoked[0] = true;
                return default;
            }

            ValueTask Action2()
            {
                actionInvoked[1] = true;
                throw new CustomException();
            }

            ValueTask Action3()
            {
                actionInvoked[2] = true;
                return default;
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await EventInvocationHelper.InvokeAllAsync(@delegate, d => d());
            });

            Assert.IsTrue(actionInvoked.All());
        }

        [TestMethod]
        public async Task InvokeAsyncMultipleExceptionTest()
        {
            var actionInvoked = new bool[3];

            Func<ValueTask> @delegate = null;

            ValueTask Action1()
            {
                actionInvoked[0] = true;
                return default;
            }

            ValueTask Action2()
            {
                actionInvoked[1] = true;
                throw new CustomException();
            }

#pragma warning disable CS1998
            async ValueTask Action3()
#pragma warning restore CS1998
            {
                actionInvoked[2] = true;
                throw new CustomException2();
            }

            @delegate += Action1;
            @delegate += Action2;
            @delegate += Action3;

            var exception = await Assert.ThrowsExceptionAsync<AggregateException>(async () =>
            {
                await EventInvocationHelper.InvokeAllAsync(@delegate, d => d());
            });

            Assert.IsTrue(actionInvoked.All());
            Assert.AreEqual(2, exception.InnerExceptions.Count());
            Assert.IsTrue(exception.InnerExceptions.Any(p => p is CustomException));
            Assert.IsTrue(exception.InnerExceptions.Any(p => p is CustomException2));
        }

        private sealed class CustomException : Exception { }
        private sealed class CustomException2 : Exception { }
    }
}
