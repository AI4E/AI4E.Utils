using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AI4E.Utils
{
    public static class ObjectInspector
    {
        #region Fields

        private static ConcurrentDictionary<Type, bool> _isStructTypeToDeepCopy = new ConcurrentDictionary<Type, bool>();

        // We cache the delegates for perf reasons.
#pragma warning disable HAA0603 // Delegate allocation from a method group
        private static readonly Func<Type, bool> _isStructTypeToDeepCopyFactory = IsStructWhichNeedsDeepCopyFactory;
        private static readonly Func<Type, bool> _isClassOtherThanString = TypeExtension.IsClassOtherThanString;
#pragma warning restore HAA0603 // Delegate allocation from a method group

        #endregion

        public static FieldInfo[] GetFieldsToCopy(this Type type)
        {
            return GetAllRelevantFields(type, forceAllFields: false);
        }

        public static bool IsTypeToDeepCopy(this Type type)
        {
            return type.IsClassOtherThanString() || IsStructWhichNeedsDeepCopy(type);
        }

        private static FieldInfo[] GetAllRelevantFields(Type type, bool forceAllFields)
        {
            var fieldsList = new List<FieldInfo>();

            for (var typeCache = type; typeCache != null; typeCache = typeCache.BaseType)
            {
                var fields = typeCache.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).AsEnumerable();

                if (!forceAllFields)
                {
                    fields = fields.Where(field => IsTypeToDeepCopy(field.FieldType));
                }

                fieldsList.AddRange(fields);
            }

            return fieldsList.ToArray();
        }

        private static FieldInfo[] GetAllFields(Type type)
        {
            return GetAllRelevantFields(type, forceAllFields: true);
        }

        private static bool IsStructWhichNeedsDeepCopy(Type type)
        {
            // The following structure ensures that multiple threads can use the dictionary
            // even while dictionary is locked and being updated by other thread.
            // That is why we do not modify the old dictionary instance but
            // we replace it with a new instance everytime.

            return _isStructTypeToDeepCopy.GetOrAdd(type, _isStructTypeToDeepCopyFactory);
        }

        private static bool IsStructWhichNeedsDeepCopyFactory(Type type)
        {
            return IsStructOtherThanBasicValueTypes(type) && HasInItsHierarchyFieldsWithClasses(type);
        }

        private static bool IsStructOtherThanBasicValueTypes(Type type)
        {
            return type.IsValueType &&
                  !type.IsPrimitive &&
                  !type.IsEnum &&
                   type != typeof(decimal);
        }

        private static bool HasInItsHierarchyFieldsWithClasses(Type type, HashSet<Type> alreadyCheckedTypes = null)
        {
            alreadyCheckedTypes = alreadyCheckedTypes ?? new HashSet<Type>();

            alreadyCheckedTypes.Add(type);

            var allFields = GetAllFields(type);
            var allFieldTypes = allFields.Select(f => f.FieldType).Distinct().ToList();
            var hasFieldsWithClasses = allFieldTypes.Any(_isClassOtherThanString);

            if (hasFieldsWithClasses)
            {
                return true;
            }

            foreach (var typeToCheck in allFieldTypes)
            {
                if (!IsStructOtherThanBasicValueTypes(typeToCheck) ||
                   alreadyCheckedTypes.Contains(typeToCheck))
                {
                    continue;
                }

                if (HasInItsHierarchyFieldsWithClasses(typeToCheck, alreadyCheckedTypes))
                {
                    return true;
                }
            }

            return false;
        }
    }
}