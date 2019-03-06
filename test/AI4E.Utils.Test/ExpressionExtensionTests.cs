using System.Linq.Expressions;
using AI4E.Utils.TestTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class ExpressionExtensionTests
    {
        [TestMethod]
        public void EvaluateConstantExpressionTest()
        {
            var constantExpression = Expression.Constant(15, typeof(int));
            var value = constantExpression.Evaluate();

            Assert.AreEqual(15, value);
        }

        [TestMethod]
        public void EvaluateFieldOfConstantExpressionTest()
        {
            var testInstance = new ExpressionTestClass { _stringField = "123" };
            var instanceExpression = Expression.Constant(testInstance, typeof(ExpressionTestClass));
            var expression = Expression.MakeMemberAccess(instanceExpression, typeof(ExpressionTestClass).GetField(nameof(testInstance._stringField)));
            var value = expression.Evaluate();

            Assert.AreEqual("123", value);
        }

        [TestMethod]
        public void EvaluateComplexExpressionTest()
        {
            var testInstance = new ExpressionTestClass { _stringField = "123" };
            var instanceExpression = Expression.Constant(testInstance, typeof(ExpressionTestClass));
            var callExpression = Expression.Call(instanceExpression, typeof(ExpressionTestClass).GetMethod("GetStringValue"));
            var value = callExpression.Evaluate();

            Assert.AreEqual("123xyz", value);
        }
    }
}
