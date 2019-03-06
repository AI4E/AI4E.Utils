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
using System.Linq.Expressions;
using Microsoft.Extensions.ObjectPool;
using static System.Diagnostics.Debug;

namespace AI4E.Utils
{
    public sealed class ParameterExpressionReplacer
    {
        private static readonly ObjectPool<ReplacerExpressionVisitor> _pool;

        static ParameterExpressionReplacer()
        {
            _pool = new DefaultObjectPool<ReplacerExpressionVisitor>(new DefaultPooledObjectPolicy<ReplacerExpressionVisitor>());
        }

        public static Expression ReplaceParameter(Expression expression, ParameterExpression parameter, Expression replacement)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            using (_pool.Get(out var replaceExpressionVisitor))
            {
                replaceExpressionVisitor.SetExpressions(parameter, replacement);
                return replaceExpressionVisitor.Visit(expression);
            }
        }

        private sealed class ReplacerExpressionVisitor : ExpressionVisitor
        {
            private ParameterExpression _parameterExpression;
            private Expression _replacement;

            public void SetExpressions(ParameterExpression parameterExpression, Expression replacement)
            {
                Assert(parameterExpression != null);
                Assert(replacement != null);
                Assert(parameterExpression.Type.IsAssignableFrom(replacement.Type));

                _parameterExpression = parameterExpression;
                _replacement = replacement;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _parameterExpression)
                {
                    return _replacement;
                }

                return base.VisitParameter(node);
            }
        }
    }
}
