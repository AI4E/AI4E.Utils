/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * 
 * MIT License
 * 
 * Copyright (c) 2019 Andreas Truetschel and contributors.
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

/* Based on
* --------------------------------------------------------------------------------------------------------------------
* Fast Deep Copy by Expression Trees 
* https://www.codeproject.com/articles/1111658/fast-deep-copy-by-expression-trees-c-sharp
* 
* MIT License
* 
* Copyright (c) 2014 - 2016 Frantisek Konopecky
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AI4E.Utils
{
    internal static partial class CopyExpressionBuilder
    {
        private static readonly Type _objectType = typeof(object);
        private static readonly Type _objectDictionaryType = typeof(Dictionary<object, object>);
        private static readonly MethodInfo _memberwiseCloneMethod = _objectType.GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo _objectDictionaryTypeIndexerProperty = _objectDictionaryType.GetProperty("Item");

        private static readonly Type FieldInfoType = typeof(FieldInfo);
        private static readonly MethodInfo SetValueMethod = FieldInfoType.GetMethod("SetValue", new[] { _objectType, _objectType });
        private static readonly Type ThisType = typeof(CopyExpressionBuilder);
        private static readonly MethodInfo DeepCopyByExpressionTreeObjMethod = ThisType.GetMethod(nameof(DeepCopyByExpressionTreeObj), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>> _compiledCopyFunctions =
                    new ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>>();

#pragma warning disable HAA0603 // Delegate allocation from a method group
        private static readonly Func<Type, Func<object, Dictionary<object, object>, object>> _createCompiledLambdaCopyFunctionForType = CreateCompiledLambdaCopyFunctionForType;
#pragma warning restore HAA0603 // Delegate allocation from a method group

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        private static readonly ConstantExpression _zeroIntConstantExpression = Expression.Constant(0, typeof(int));
        private static readonly ConstantExpression _oneIntConstantExpression = Expression.Constant(1, typeof(int));
        private static readonly ConstantExpression _twoIntConstantExpression = Expression.Constant(2, typeof(int));

        private static readonly ConstantExpression _trueConstantExpression = Expression.Constant(true, typeof(bool));
        private static readonly ConstantExpression _falseConstantExpression = Expression.Constant(false, typeof(bool));

#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        private static readonly MethodInfo _arrayGetLengthMethod = typeof(Array).GetMethod(nameof(Array.GetLength), BindingFlags.Public | BindingFlags.Instance);

        internal static object DeepCopy(object original)
        {
            return DeepCopyByExpressionTreeObj(original, forceDeepCopy: false, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }

        private static object DeepCopyByExpressionTreeObj(object original, bool forceDeepCopy, Dictionary<object, object> copiedReferencesDict)
        {
            if (original == null)
            {
                return null;
            }

            var type = original.GetType();

            if (type.IsDelegate())
            {
                return null;
            }

            if (!forceDeepCopy && !type.IsTypeToDeepCopy())
            {
                return original;
            }

            if (copiedReferencesDict.TryGetValue(original, out var alreadyCopiedObject))
            {
                return alreadyCopiedObject;
            }

            if (type == _objectType)
            {
                return new object();
            }

            var compiledCopyFunction = GetOrCreateCompiledLambdaCopyFunction(type);
            var copy = compiledCopyFunction(original, copiedReferencesDict);

            return copy;
        }

        private static Func<object, Dictionary<object, object>, object> GetOrCreateCompiledLambdaCopyFunction(Type type)
        {
            // The following structure ensures that multiple threads can use the dictionary
            // even while dictionary is locked and being updated by other thread.
            // That is why we do not modify the old dictionary instance but
            // we replace it with a new instance everytime.

            return _compiledCopyFunctions.GetOrAdd(type, _createCompiledLambdaCopyFunctionForType);
        }

        private static Func<object, Dictionary<object, object>, object> CreateCompiledLambdaCopyFunctionForType(Type type)
        {

            ///// INITIALIZATION OF EXPRESSIONS AND VARIABLES

            InitializeExpressions(type,
                                  out var inputParameter,
                                  out var inputDictionary,
                                  out var outputVariable,
                                  out var boxingVariable,
                                  out var endLabel,
                                  out var variables,
                                  out var expressions);

            ///// RETURN NULL IF ORIGINAL IS NULL

            IfNullThenReturnNullExpression(inputParameter, endLabel, expressions);

            ///// MEMBERWISE CLONE ORIGINAL OBJECT

            MemberwiseCloneInputToOutputExpression(type, inputParameter, outputVariable, expressions);

            ///// STORE COPIED OBJECT TO REFERENCES DICTIONARY

            if (type.IsClassOtherThanString())
            {
                StoreReferencesIntoDictionaryExpression(inputParameter, inputDictionary, outputVariable, expressions);
            }

            ///// COPY ALL NONVALUE OR NONPRIMITIVE FIELDS

            FieldsCopyExpressions(type,
                                  inputParameter,
                                  inputDictionary,
                                  outputVariable,
                                  boxingVariable,
                                  expressions);

            ///// COPY ELEMENTS OF ARRAY

            if (type.IsArray() && type.GetElementType().IsTypeToDeepCopy())
            {
                CreateArrayCopyLoopExpression(type,
                                              inputParameter,
                                              inputDictionary,
                                              outputVariable,
                                              variables,
                                              expressions);
            }

            ///// COMBINE ALL EXPRESSIONS INTO LAMBDA FUNCTION

            var lambda = CombineAllIntoLambdaFunctionExpression(inputParameter, inputDictionary, outputVariable, endLabel, variables, expressions);

            return lambda.Compile();
        }

        private static void InitializeExpressions(Type type,
                                                  out ParameterExpression inputParameter,
                                                  out ParameterExpression inputDictionary,
                                                  out ParameterExpression outputVariable,
                                                  out ParameterExpression boxingVariable,
                                                  out LabelTarget endLabel,
                                                  out List<ParameterExpression> variables,
                                                  out List<Expression> expressions)
        {

            inputParameter = Expression.Parameter(_objectType);

            inputDictionary = Expression.Parameter(_objectDictionaryType);

            outputVariable = Expression.Variable(type);

            boxingVariable = Expression.Variable(_objectType);

            endLabel = Expression.Label();

            variables = new List<ParameterExpression>();

            expressions = new List<Expression>();

            variables.Add(outputVariable);
            variables.Add(boxingVariable);
        }

        private static void IfNullThenReturnNullExpression(ParameterExpression inputParameter, LabelTarget endLabel, List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// if (input == null)
            ///// {
            /////     return null;
            ///// }

            var ifNullThenReturnNullExpression =
                Expression.IfThen(
                    Expression.Equal(
                        inputParameter,
                        Expression.Constant(null, _objectType)),
                    Expression.Return(endLabel));

            expressions.Add(ifNullThenReturnNullExpression);
        }

        private static void MemberwiseCloneInputToOutputExpression(Type type,
                                                                   ParameterExpression inputParameter,
                                                                   ParameterExpression outputVariable,
                                                                   List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// var output = (<type>)input.MemberwiseClone();

            var memberwiseCloneMethodCall = Expression.Call(inputParameter, _memberwiseCloneMethod);
            var convertedCloneResult = Expression.Convert(memberwiseCloneMethodCall, type);
            var memberwiseCloneInputExpression = Expression.Assign(outputVariable, convertedCloneResult);

            expressions.Add(memberwiseCloneInputExpression);
        }



        private static void StoreReferencesIntoDictionaryExpression(ParameterExpression inputParameter,
                                                                    ParameterExpression inputDictionary,
                                                                    ParameterExpression outputVariable,
                                                                    List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// inputDictionary[(Object)input] = (Object)output;

            var propertyAccessArguments = new Expression[] { inputParameter };
            var propertyAccess = Expression.Property(inputDictionary, _objectDictionaryTypeIndexerProperty, propertyAccessArguments);
            var convertedOutput = Expression.Convert(outputVariable, _objectType);
            var storeReferencesExpression = Expression.Assign(propertyAccess, convertedOutput);

            expressions.Add(storeReferencesExpression);
        }

        private static Expression<Func<object, Dictionary<object, object>, object>> CombineAllIntoLambdaFunctionExpression(
            ParameterExpression inputParameter,
            ParameterExpression inputDictionary,
            ParameterExpression outputVariable,
            LabelTarget endLabel,
            List<ParameterExpression> variables,
            List<Expression> expressions)
        {
            expressions.Add(Expression.Label(endLabel));
            expressions.Add(Expression.Convert(outputVariable, _objectType));

            var finalBody = Expression.Block(variables, expressions);
            var parameters = new[] { inputParameter, inputDictionary };
            var lambda = Expression.Lambda<Func<object, Dictionary<object, object>, object>>(finalBody, parameters);
            return lambda;
        }

        private static void CreateArrayCopyLoopExpression(Type type,
                                                          ParameterExpression inputParameter,
                                                          ParameterExpression inputDictionary,
                                                          ParameterExpression outputVariable,
                                                          List<ParameterExpression> variables,
                                                          List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// int i1, i2, ..., in; 
            ///// 
            ///// int length1 = inputarray.GetLength(0); 
            ///// i1 = 0; 
            ///// while (true)
            ///// {
            /////     if (i1 >= length1)
            /////     {
            /////         goto ENDLABELFORLOOP1;
            /////     }
            /////     int length2 = inputarray.GetLength(1); 
            /////     i2 = 0; 
            /////     while (true)
            /////     {
            /////         if (i2 >= length2)
            /////         {
            /////             goto ENDLABELFORLOOP2;
            /////         }
            /////         ...
            /////         ...
            /////         ...
            /////         int lengthn = inputarray.GetLength(n); 
            /////         in = 0; 
            /////         while (true)
            /////         {
            /////             if (in >= lengthn)
            /////             {
            /////                 goto ENDLABELFORLOOPn;
            /////             }
            /////             outputarray[i1, i2, ..., in] 
            /////                 = (<elementType>)DeepCopyByExpressionTreeObj(
            /////                        (Object)inputarray[i1, i2, ..., in])
            /////             in++; 
            /////         }
            /////         ENDLABELFORLOOPn:
            /////         ...
            /////         ...  
            /////         ...
            /////         i2++; 
            /////     }
            /////     ENDLABELFORLOOP2:
            /////     i1++; 
            ///// }
            ///// ENDLABELFORLOOP1:

            var rank = type.GetArrayRank();

            var indices = GenerateIndices(rank);

            variables.AddRange(indices);

            var elementType = type.GetElementType();

            var assignExpression = ArrayFieldToArrayFieldAssignExpression(inputParameter, inputDictionary, outputVariable, elementType, type, indices);

            Expression forExpression = assignExpression;

            for (var dimension = 0; dimension < rank; dimension++)
            {
                var indexVariable = indices[dimension];

                forExpression = LoopIntoLoopExpression(inputParameter, indexVariable, forExpression, dimension);
            }

            expressions.Add(forExpression);
        }

        private static List<ParameterExpression> GenerateIndices(int arrayRank)
        {
            ///// Intended code:
            /////
            ///// int i1, i2, ..., in; 

            var indices = new List<ParameterExpression>();

            for (var i = 0; i < arrayRank; i++)
            {
                var indexVariable = Expression.Variable(typeof(int));

                indices.Add(indexVariable);
            }

            return indices;
        }

        private static BinaryExpression ArrayFieldToArrayFieldAssignExpression(
            ParameterExpression inputParameter,
            ParameterExpression inputDictionary,
            ParameterExpression outputVariable,
            Type elementType,
            Type arrayType,
            List<ParameterExpression> indices)
        {
            ///// Intended code:
            /////
            ///// outputarray[i1, i2, ..., in] 
            /////     = (<elementType>)DeepCopyByExpressionTreeObj(
            /////            (Object)inputarray[i1, i2, ..., in]);

            var indexTo = Expression.ArrayAccess(outputVariable, indices);
            var indexFrom = Expression.ArrayIndex(Expression.Convert(inputParameter, arrayType), indices);
            var forceDeepCopy = elementType != _objectType;
            var forceDeepCopyConstant = forceDeepCopy ? _trueConstantExpression : _falseConstantExpression;
            var deepCopyCall = Expression.Call(DeepCopyByExpressionTreeObjMethod,
                                               Expression.Convert(indexFrom, _objectType),
                                               forceDeepCopyConstant,
                                                inputDictionary);

            var rightSide = Expression.Convert(deepCopyCall, elementType);
            var assignExpression = Expression.Assign(indexTo, rightSide);
            return assignExpression;
        }

        private static BlockExpression LoopIntoLoopExpression(
            ParameterExpression inputParameter,
            ParameterExpression indexVariable,
            Expression loopToEncapsulate,
            int dimension)
        {
            ///// Intended code:
            /////
            ///// int length = inputarray.GetLength(dimension); 
            ///// i = 0; 
            ///// while (true)
            ///// {
            /////     if (i >= length)
            /////     {
            /////         goto ENDLABELFORLOOP;
            /////     }
            /////     loopToEncapsulate;
            /////     i++; 
            ///// }
            ///// ENDLABELFORLOOP:

            var lengthVariable = Expression.Variable(typeof(int));
            var endLabelForThisLoop = Expression.Label();

            // Loop:
            var @break = Expression.Break(endLabelForThisLoop);
            var condition = Expression.GreaterThanOrEqual(indexVariable, lengthVariable);
            var branch = Expression.IfThen(condition, @break);
            var incrementIndexVariable = Expression.PostIncrementAssign(indexVariable);
            var loopExpressions = new[] { branch, loopToEncapsulate, incrementIndexVariable };
            var loopBlock = Expression.Block(Array.Empty<ParameterExpression>(), loopExpressions);
            var newLoop = Expression.Loop(loopBlock, endLabelForThisLoop);

            var lengthAssignment = GetLengthForDimensionExpression(lengthVariable, inputParameter, dimension);
            var indexAssignment = Expression.Assign(indexVariable, _zeroIntConstantExpression);

            var variables = new[] { lengthVariable };
            var expressions = new Expression[] { lengthAssignment, indexAssignment, newLoop };

            return Expression.Block(variables, expressions);
        }

        private static BinaryExpression GetLengthForDimensionExpression(
            ParameterExpression lengthVariable,
            ParameterExpression inputParameter,
            int i)
        {
            ///// Intended code:
            /////
            ///// length = ((Array)input).GetLength(i); 

            ConstantExpression dimensionConstant;

            switch (i)
            {
                case 0:
                    dimensionConstant = _zeroIntConstantExpression;
                    break;

                case 1:
                    dimensionConstant = _oneIntConstantExpression;
                    break;

                case 2:
                    dimensionConstant = _twoIntConstantExpression;
                    break;

                default:
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
                    dimensionConstant = Expression.Constant(i);
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
                    break;

            }

            var convertedInput = Expression.Convert(inputParameter, typeof(Array));
            var arrayGetLengthArguments = new Expression[] { dimensionConstant };
            var arrayGetLengthCall = Expression.Call(convertedInput, _arrayGetLengthMethod, arrayGetLengthArguments);

            return Expression.Assign(lengthVariable, arrayGetLengthCall);
        }

        private static void FieldsCopyExpressions(Type type,
                                                  ParameterExpression inputParameter,
                                                  ParameterExpression inputDictionary,
                                                  ParameterExpression outputVariable,
                                                  ParameterExpression boxingVariable,
                                                  List<Expression> expressions)
        {
            var fields = type.GetFieldsToCopy();

            var readonlyFields = fields.Where(f => f.IsInitOnly).ToList();
            var writableFields = fields.Where(f => !f.IsInitOnly).ToList();

            ///// READONLY FIELDS COPY (with boxing)

            var shouldUseBoxing = readonlyFields.Any();

            if (shouldUseBoxing)
            {
                var boxingExpression = Expression.Assign(boxingVariable, Expression.Convert(outputVariable, _objectType));

                expressions.Add(boxingExpression);
            }

            foreach (var field in readonlyFields)
            {
                if (field.FieldType.IsDelegate())
                {
                    ReadonlyFieldToNullExpression(field, boxingVariable, expressions);
                }
                else
                {
                    ReadonlyFieldCopyExpression(type,
                                                field,
                                                inputParameter,
                                                inputDictionary,
                                                boxingVariable,
                                                expressions);
                }
            }

            if (shouldUseBoxing)
            {
                var unboxingExpression = Expression.Assign(outputVariable, Expression.Convert(boxingVariable, type));

                expressions.Add(unboxingExpression);
            }

            ///// NOT-READONLY FIELDS COPY

            foreach (var field in writableFields)
            {
                if (field.FieldType.IsDelegate())
                {
                    WritableFieldToNullExpression(field, outputVariable, expressions);
                }
                else
                {
                    WritableFieldCopyExpression(type,
                                                field,
                                                inputParameter,
                                                inputDictionary,
                                                outputVariable,
                                                expressions);
                }
            }
        }

        private static void ReadonlyFieldToNullExpression(FieldInfo field, ParameterExpression boxingVariable, List<Expression> expressions)
        {
            // This option must be implemented by Reflection because of the following:
            // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

            ///// Intended code:
            /////
            ///// fieldInfo.SetValue(boxing, <fieldtype>null);

            var fieldToNullExpression =
                    Expression.Call(
                        Expression.Constant(field),
                        SetValueMethod,
                        boxingVariable,
                        Expression.Constant(null, field.FieldType));

            expressions.Add(fieldToNullExpression);
        }

        private static void ReadonlyFieldCopyExpression(Type type,
                                                        FieldInfo field,
                                                        ParameterExpression inputParameter,
                                                        ParameterExpression inputDictionary,
                                                        ParameterExpression boxingVariable,
                                                        List<Expression> expressions)
        {
            // This option must be implemented by Reflection (SetValueMethod) because of the following:
            // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

            ///// Intended code:
            /////
            ///// fieldInfo.SetValue(boxing, DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>))

            var fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);

            var forceDeepCopy = field.FieldType != _objectType;
            var convertedFieldFrom = Expression.Convert(fieldFrom, _objectType);
            var forceDeepCopyConstant = forceDeepCopy ? _trueConstantExpression : _falseConstantExpression;
            var deepCopyCall = Expression.Call(DeepCopyByExpressionTreeObjMethod,
                                               convertedFieldFrom,
                                               forceDeepCopyConstant,
                                               inputDictionary);

            var fieldDeepCopyExpression = Expression.Call(Expression.Constant(field, FieldInfoType), SetValueMethod, boxingVariable, deepCopyCall);

            expressions.Add(fieldDeepCopyExpression);
        }

        private static void WritableFieldToNullExpression(FieldInfo field, ParameterExpression outputVariable, List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// output.<field> = (<type>)null;

            var fieldTo = Expression.Field(outputVariable, field);

            var fieldToNullExpression = Expression.Assign(fieldTo, Expression.Constant(null, field.FieldType));

            expressions.Add(fieldToNullExpression);
        }

        private static void WritableFieldCopyExpression(Type type,
                                                        FieldInfo field,
                                                        ParameterExpression inputParameter,
                                                        ParameterExpression inputDictionary,
                                                        ParameterExpression outputVariable,
                                                        List<Expression> expressions)
        {
            ///// Intended code:
            /////
            ///// output.<field> = (<fieldType>)DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>);

            var fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);
            var fieldType = field.FieldType;
            var fieldTo = Expression.Field(outputVariable, field);
            var forceDeepCopy = field.FieldType != _objectType;
            var convertedFieldFrom = Expression.Convert(fieldFrom, _objectType);
            var forceDeepCopyConstant = forceDeepCopy ? _trueConstantExpression : _falseConstantExpression;
            var deepCopyCall = Expression.Call(DeepCopyByExpressionTreeObjMethod,
                                               convertedFieldFrom,
                                               forceDeepCopyConstant,
                                               inputDictionary);

            var convertedCopy = Expression.Convert(deepCopyCall, fieldType);
            var fieldDeepCopyExpression = Expression.Assign(fieldTo, convertedCopy);

            expressions.Add(fieldDeepCopyExpression);
        }

        private class ReferenceEqualityComparer : EqualityComparer<object>
        {
            public override bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public override int GetHashCode(object obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.GetHashCode();
            }
        }
    }
}
