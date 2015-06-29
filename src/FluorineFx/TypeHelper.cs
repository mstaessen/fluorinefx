/*
	FluorineFx open source library 
	Copyright (C) 2007 Zoltan Csibi, zoltan@TheSilentGroup.com, FluorineFx.com 
	
	This library is free software; you can redistribute it and/or
	modify it under the terms of the GNU Lesser General Public
	License as published by the Free Software Foundation; either
	version 2.1 of the License, or (at your option) any later version.
	
	This library is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
	Lesser General Public License for more details.
	
	You should have received a copy of the GNU Lesser General Public
	License along with this library; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using FluorineFx.Configuration;
using FluorineFx.Util;
using Convert = System.Convert;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using log4net;
using System.Xml.Linq;

namespace FluorineFx
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class TypeHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (TypeHelper));

        static TypeHelper()
        {
            DefaultSByteNullValue = (sbyte) GetNullValue(typeof (sbyte));
            DefaultInt16NullValue = (short) GetNullValue(typeof (short));
            DefaultInt32NullValue = (int) GetNullValue(typeof (int));
            DefaultInt64NullValue = (long) GetNullValue(typeof (long));
            DefaultByteNullValue = (byte) GetNullValue(typeof (byte));
            DefaultUInt16NullValue = (ushort) GetNullValue(typeof (ushort));
            DefaultUInt32NullValue = (uint) GetNullValue(typeof (uint));
            DefaultUInt64NullValue = (ulong) GetNullValue(typeof (ulong));
            DefaultCharNullValue = (char) GetNullValue(typeof (char));
            DefaultSingleNullValue = (float) GetNullValue(typeof (float));
            DefaultDoubleNullValue = (double) GetNullValue(typeof (double));
            DefaultBooleanNullValue = (bool) GetNullValue(typeof (bool));

            DefaultStringNullValue = (string) GetNullValue(typeof (string));
            DefaultDateTimeNullValue = (DateTime) GetNullValue(typeof (DateTime));
            DefaultDecimalNullValue = (decimal) GetNullValue(typeof (decimal));
            DefaultGuidNullValue = (Guid) GetNullValue(typeof (Guid));
            DefaultXmlReaderNullValue = (XmlReader) GetNullValue(typeof (XmlReader));
            DefaultXmlDocumentNullValue = (XmlDocument) GetNullValue(typeof (XmlDocument));
            _defaultXDocumentNullValue = (XDocument) GetNullValue(typeof (XDocument));
            _defaultXElementNullValue = (XElement) GetNullValue(typeof (XElement));
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns></returns>
        public static Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type Locate(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) {
                return null;
            }
            var assemblies = GetAssemblies();
            return assemblies.Select(assembly => assembly.GetType(typeName, false)).FirstOrDefault(type => type != null);
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="lac"></param>
        /// <returns></returns>
        public static Type LocateInLac(string typeName, string lac)
        {
            if (lac == null)
                return null;
            if (string.IsNullOrEmpty(typeName))
                return null;
            foreach (var file in Directory.GetFiles(lac, "*.dll")) {
                try {
                    Log.Debug(string.Format(Resources.TypeHelper_Probing, file, typeName));
                    var assembly = Assembly.LoadFrom(file);
                    var type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch (Exception ex) {
                    if (Log.IsWarnEnabled) {
                        Log.Warn(string.Format(Resources.TypeHelper_LoadDllFail, file));
                        Log.Warn(ex.Message);
                    }
                }
            }
            return Directory.GetDirectories(lac).Select(dir => LocateInLac(typeName, dir)).FirstOrDefault(type => type != null);
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="lac"></param>
        /// <param name="excludedBaseTypes"></param>
        /// <returns></returns>
        public static Type[] SearchAllTypes(string lac, Hashtable excludedBaseTypes)
        {
            var result = new ArrayList();
            foreach (var file in Directory.GetFiles(lac, "*.dll")) {
                try {
                    var assembly = Assembly.LoadFrom(file);
                    if (assembly == Assembly.GetExecutingAssembly())
                        continue;
                    foreach (var type in assembly.GetTypes()) {
                        if (excludedBaseTypes != null) {
                            if (excludedBaseTypes.ContainsKey(type))
                                continue;
                            if (type.BaseType != null && excludedBaseTypes.ContainsKey(type.BaseType))
                                continue;
                        }
                        result.Add(type);
                    }
                } catch (Exception ex) {
                    if (Log.IsWarnEnabled) {
                        Log.Warn(string.Format(Resources.TypeHelper_LoadDllFail, file));
                        Log.Warn(ex.Message);
                    }
                }
            }
            return (Type[]) result.ToArray(typeof (Type));
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static bool SkipMethod(MethodInfo methodInfo)
        {
            return methodInfo.ReturnType == typeof (IAsyncResult) || methodInfo.GetParameters().Any(parameterInfo => parameterInfo.ParameterType == typeof (IAsyncResult));
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetDescription(Type type)
        {
            var attribute = ReflectionUtils.GetAttribute(typeof (DescriptionAttribute), type, false);
            return attribute != null ? ((DescriptionAttribute) attribute).Description : null;
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static string GetDescription(MethodInfo methodInfo)
        {
            var attribute = ReflectionUtils.GetAttribute(typeof (DescriptionAttribute), methodInfo, false);
            return attribute != null ? ((DescriptionAttribute) attribute).Description : null;
        }

        public static void NarrowValues(object[] values, ParameterInfo[] parameterInfos)
        {
            //Narrow down convertibe types (double for example)
            for (var i = 0; values != null && i < values.Length; i++) {
                var value = values[i];
                values[i] = ChangeType(value, parameterInfos[i].ParameterType);
            }
        }

        internal static object GetNullValue(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (type.IsValueType) {
                /* Not supported
                if (type.IsEnum)
                    return GetEnumNullValue(type);
                */
                if (type.IsPrimitive) {
                    if (type == typeof (int)) return 0;
                    if (type == typeof (double)) return (double) 0;
                    if (type == typeof (short)) return (short) 0;
                    if (type == typeof (bool)) return false;
                    if (type == typeof (sbyte)) return (sbyte) 0;
                    if (type == typeof (long)) return (long) 0;
                    if (type == typeof (byte)) return (byte) 0;
                    if (type == typeof (ushort)) return (ushort) 0;
                    if (type == typeof (uint)) return (uint) 0;
                    if (type == typeof (ulong)) return (ulong) 0;
                    if (type == typeof (float)) return (float) 0;
                    if (type == typeof (char)) return new char();
                } else {
                    if (type == typeof (DateTime)) return DateTime.MinValue;
                    if (type == typeof (decimal)) return 0m;
                    if (type == typeof (Guid)) return Guid.Empty;

#if !SILVERLIGHT
                    if (type == typeof (SqlInt32)) return SqlInt32.Null;
                    if (type == typeof (SqlString)) return SqlString.Null;
                    if (type == typeof (SqlBoolean)) return SqlBoolean.Null;
                    if (type == typeof (SqlByte)) return SqlByte.Null;
                    if (type == typeof (SqlDateTime)) return SqlDateTime.Null;
                    if (type == typeof (SqlDecimal)) return SqlDecimal.Null;
                    if (type == typeof (SqlDouble)) return SqlDouble.Null;
                    if (type == typeof (SqlGuid)) return SqlGuid.Null;
                    if (type == typeof (SqlInt16)) return SqlInt16.Null;
                    if (type == typeof (SqlInt64)) return SqlInt64.Null;
                    if (type == typeof (SqlMoney)) return SqlMoney.Null;
                    if (type == typeof (SqlSingle)) return SqlSingle.Null;
                    if (type == typeof (SqlBinary)) return SqlBinary.Null;
#endif
                }
            } else {
                if (type == typeof (string)) return null; // string.Empty;
                if (type == typeof (DBNull)) return DBNull.Value;
            }
            return null;
        }

        internal static object CreateInstance(Type type)
        {
            //Is this a generic type definition?
            if (ReflectionUtils.IsGenericType(type)) {
                var genericTypeDefinition = ReflectionUtils.GetGenericTypeDefinition(type);
                // Get the generic type parameters or type arguments.
                var typeParameters = ReflectionUtils.GetGenericArguments(type);

                // Construct an array of type arguments to substitute for 
                // the type parameters of the generic class.
                // The array must contain the correct number of types, in 
                // the same order that they appear in the type parameter 
                // list.
                // Construct the type Dictionary<String, Example>.
                var constructed = ReflectionUtils.MakeGenericType(genericTypeDefinition, typeParameters);
                var obj = Activator.CreateInstance(constructed);
                if (obj == null) {
#if !SILVERLIGHT
                    if (Log != null && Log.IsErrorEnabled) {
                        var msg = string.Format("Could not instantiate the generic type {0}.", type.FullName);
                        Log.Error(msg);
                    }
#endif
                }
                return obj;
            }
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Detects the MONO runtime.
        /// </summary>
        public static bool IsMono
        {
            get { return (typeof (object).Assembly.GetType("System.MonoType") != null); }
        }

        private class Location
        {
            private readonly string _path;
            private readonly string _description;

            public Location(string path, string description)
            {
                _path = path;
                _description = description;
            }

            public string Path
            {
                get { return _path; }
            }

            public string Description
            {
                get { return _description; }
            }
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns>Lac locations used for assembly probing by FluorineFx.</returns>
        public static string[] GetLacLocations()
        {
            return new string[0];
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool GetTypeIsAccessible(Type type)
        {
            if (type == null)
                return false;
            if (type.Assembly == typeof (TypeHelper).Assembly)
                return false;
            return true;
        }

        /// <summary>
        /// Returns the underlying type argument of the specified type.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> instance. </param>
        /// <returns><list>
        /// <item>The type argument of the type parameter,
        /// if the type parameter is a closed generic nullable type.</item>
        /// <item>The underlying Type of enumType, if the type parameter is an enum type.</item>
        /// <item>Otherwise, the type itself.</item>
        /// </list>
        /// </returns>
        public static Type GetUnderlyingType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (ReflectionUtils.IsNullable(type))
                type = type.GetGenericArguments()[0];
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);
            return type;
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetCSharpName(Type type)
        {
            var dimensions = 0;
            while (type.IsArray) {
                type = type.GetElementType();
                dimensions++;
            }
            var sb = new StringBuilder();
            sb.Append(type.Namespace);
            sb.Append(".");

            var parameters = Type.EmptyTypes;
            if (ReflectionUtils.IsGenericType(type)) {
                if (ReflectionUtils.GetGenericArguments(type) != null)
                    parameters = ReflectionUtils.GetGenericArguments(type);
            }
            GetCSharpName(type, parameters, 0, sb);
            for (var i = 0; i < dimensions; i++) {
                sb.Append("[]");
            }
            return sb.ToString();
        }

        private static int GetCSharpName(Type type, Type[] parameters, int index, StringBuilder sb)
        {
            if (type.DeclaringType != null && type.DeclaringType != type) {
                index = GetCSharpName(type.DeclaringType, parameters, index, sb);
                sb.Append(".");
            }
            var name = type.Name;
            var length = name.IndexOf('`');
            if (length < 0)
                length = name.IndexOf('!');
            if (length > 0) {
                sb.Append(name.Substring(0, length));
                sb.Append("<");
                var paramCount = int.Parse(name.Substring(length + 1), CultureInfo.InvariantCulture) + index;
                while (index < paramCount) {
                    sb.Append(GetCSharpName(parameters[index]));
                    if (index < (paramCount - 1)) {
                        sb.Append(",");
                    }
                    index++;
                }
                sb.Append(">");
                return index;
            }
            sb.Append(name);
            return index;
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static bool IsAssignable(object obj, Type targetType)
        {
            return IsAssignable(obj, targetType, ReflectionUtils.IsNullable(targetType));
        }

        private static bool IsAssignable(object obj, Type targetType, bool isNullable)
        {
            if (obj != null && targetType.IsAssignableFrom(obj.GetType()))
                return true; //targetType can be assigned from an instance of the obj's Type
            if (isNullable && obj == null)
                return true; //null is assignable to a nullable type
            if (targetType.IsArray) {
                if (null == obj)
                    return true;
                var srcType = obj.GetType();

                if (srcType == targetType)
                    return true;

                if (srcType.IsArray) {
                    var srcElementType = srcType.GetElementType();
                    var dstElementType = targetType.GetElementType();

                    if (srcElementType.IsArray != dstElementType.IsArray
                        || (srcElementType.IsArray &&
                            srcElementType.GetArrayRank() != dstElementType.GetArrayRank())) {
                        return false;
                    }

                    var srcArray = (Array) obj;
                    var rank = srcArray.Rank;
                    if (rank == 1 && 0 == srcArray.GetLowerBound(0)) {
                        var arrayLength = srcArray.Length;
                        // Int32 is assignable from UInt32, SByte from Byte and so on.
                        if (dstElementType.IsAssignableFrom(srcElementType))
                            return true;
                        //This is a costly operation
                        for (var i = 0; i < arrayLength; ++i)
                            if (!IsAssignable(srcArray.GetValue(i), dstElementType))
                                return false;
                    } else {
                        //This is a costly operation
                        var arrayLength = 1;
                        var dimensions = new int[rank];
                        var indices = new int[rank];
                        var lbounds = new int[rank];

                        for (var i = 0; i < rank; ++i) {
                            arrayLength *= (dimensions[i] = srcArray.GetLength(i));
                            lbounds[i] = srcArray.GetLowerBound(i);
                        }

                        for (var i = 0; i < arrayLength; ++i) {
                            var index = i;
                            for (var j = rank - 1; j >= 0; --j) {
                                indices[j] = index % dimensions[j] + lbounds[j];
                                index /= dimensions[j];
                            }
                            if (!IsAssignable(srcArray.GetValue(indices), dstElementType))
                                return false;
                        }
                    }
                    return true;
                }
            } else if (targetType.IsEnum) {
                try {
                    if (obj != null) {
                        Enum.Parse(targetType, obj.ToString(), true);
                        return true;
                    }
                    return false;
                } catch (ArgumentException) {
                    return false;
                }
            }

            if (obj != null) {
                var typeConverter = ReflectionUtils.GetTypeConverter(obj); //TypeDescriptor.GetConverter(obj);
                if (typeConverter != null && typeConverter.CanConvertTo(targetType))
                    return true;
                typeConverter = ReflectionUtils.GetTypeConverter(targetType);
                    // TypeDescriptor.GetConverter(targetType);
                if (typeConverter != null && typeConverter.CanConvertFrom(obj.GetType()))
                    return true;

                //Collections
                if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.Generic.ICollection`1")
                    && obj is IList) {
                    //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                    var typeParameters = ReflectionUtils.GetGenericArguments(targetType);
                    if (typeParameters != null && typeParameters.Length == 1) {
                        //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                        var typeGenericICollection = targetType.GetInterface(
                            "System.Collections.Generic.ICollection`1", true);
                        return typeGenericICollection != null;
                    }
                    return false;
                }
                if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.IList") && obj is IList) {
                    return true;
                }

                if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.Generic.IDictionary`2")
                    && obj is IDictionary) {
                    var typeParameters = ReflectionUtils.GetGenericArguments(targetType);
                    if (typeParameters != null && typeParameters.Length == 2) {
                        //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                        var typeGenericIDictionary = targetType.GetInterface(
                            "System.Collections.Generic.IDictionary`2", true);
                        return typeGenericIDictionary != null;
                    }
                    return false;
                }
                if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.IDictionary")
                    && obj is IDictionary) {
                    return true;
                }
            } else {
#if !SILVERLIGHT
                if (targetType is INullable)
                    return true;
#endif
                if (targetType.IsValueType) {
                    if (FluorineConfiguration.Instance.AcceptNullValueTypes) {
                        // Any value-type that is not explicitly initialized with a value will 
                        // contain the default value for that object type.
                        return true;
                    }
                    return false;
                }
                return true;
            }

            try {
                if (isNullable) {
                    switch (Type.GetTypeCode(GetUnderlyingType(targetType))) {
                        case TypeCode.Char:
                            return CanConvertToNullableChar(obj);
                    }
                    if (typeof (Guid) == targetType) return CanConvertToNullableGuid(obj);
                }
                switch (Type.GetTypeCode(targetType)) {
                    case TypeCode.Char:
                        return CanConvertToChar(obj);
                }
                if (typeof (Guid) == targetType) return CanConvertToGuid(obj);
            } catch (InvalidCastException) {}

#if !SILVERLIGHT && !NET_2_0
            if (typeof (XDocument) == targetType && obj is XmlDocument) return true;
            if (typeof (XElement) == targetType && obj is XmlDocument) return true;
#endif

            return false;
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type targetType)
        {
            return ConvertChangeType(value, targetType, ReflectionUtils.IsNullable(targetType));
        }

        private static T Cast<T>(object obj) where T : class
        {
            return obj as T;
        }

        private static object ConvertChangeType(object value, Type targetType, bool isNullable)
        {
            if (targetType.IsArray) {
                if (null == value)
                    return null;

                var srcType = value.GetType();

                if (srcType == targetType)
                    return value;

                if (srcType.IsArray) {
                    var srcElementType = srcType.GetElementType();
                    var dstElementType = targetType.GetElementType();

                    if (srcElementType.IsArray != dstElementType.IsArray
                        || (srcElementType.IsArray &&
                            srcElementType.GetArrayRank() != dstElementType.GetArrayRank())) {
                        throw new InvalidCastException(
                            string.Format("Can not convert array of type '{0}' to array of '{1}'.", srcType.FullName,
                                targetType.FullName));
                    }

                    var srcArray = (Array) value;
                    Array dstArray;

                    var rank = srcArray.Rank;

                    if (rank == 1 && 0 == srcArray.GetLowerBound(0)) {
                        var arrayLength = srcArray.Length;

                        dstArray = Array.CreateInstance(dstElementType, arrayLength);

                        // Int32 is assignable from UInt32, SByte from Byte and so on.
                        //
                        if (dstElementType.IsAssignableFrom(srcElementType))
                            Array.Copy(srcArray, dstArray, arrayLength);
                        else
                            for (var i = 0; i < arrayLength; ++i)
                                dstArray.SetValue(ConvertChangeType(srcArray.GetValue(i), dstElementType, isNullable), i);
                    } else {
#if !SILVERLIGHT
                        var arrayLength = 1;
                        var dimensions = new int[rank];
                        var indices = new int[rank];
                        var lbounds = new int[rank];

                        for (var i = 0; i < rank; ++i) {
                            arrayLength *= (dimensions[i] = srcArray.GetLength(i));
                            lbounds[i] = srcArray.GetLowerBound(i);
                        }

                        dstArray = Array.CreateInstance(dstElementType, dimensions, lbounds);
                        for (var i = 0; i < arrayLength; ++i) {
                            var index = i;
                            for (var j = rank - 1; j >= 0; --j) {
                                indices[j] = index % dimensions[j] + lbounds[j];
                                index /= dimensions[j];
                            }

                            dstArray.SetValue(
                                ConvertChangeType(srcArray.GetValue(indices), dstElementType, isNullable), indices);
                        }
#else
                        throw new InvalidCastException();
#endif
                    }

                    return dstArray;
                }
            } else if (targetType.IsEnum) {
                try {
                    return Enum.Parse(targetType, value.ToString(), true);
                } catch (ArgumentException ex) {
                    throw new InvalidCastException(string.Format(Resources.TypeHelper_ConversionFail), ex);
                }
            }

            if (isNullable) {
                switch (Type.GetTypeCode(GetUnderlyingType(targetType))) {
                    case TypeCode.Boolean:
                        return ConvertToNullableBoolean(value);
                    case TypeCode.Byte:
                        return ConvertToNullableByte(value);
                    case TypeCode.Char:
                        return ConvertToNullableChar(value);
                    case TypeCode.DateTime:
                        return ConvertToNullableDateTime(value);
                    case TypeCode.Decimal:
                        return ConvertToNullableDecimal(value);
                    case TypeCode.Double:
                        return ConvertToNullableDouble(value);
                    case TypeCode.Int16:
                        return ConvertToNullableInt16(value);
                    case TypeCode.Int32:
                        return ConvertToNullableInt32(value);
                    case TypeCode.Int64:
                        return ConvertToNullableInt64(value);
                    case TypeCode.SByte:
                        return ConvertToNullableSByte(value);
                    case TypeCode.Single:
                        return ConvertToNullableSingle(value);
                    case TypeCode.UInt16:
                        return ConvertToNullableUInt16(value);
                    case TypeCode.UInt32:
                        return ConvertToNullableUInt32(value);
                    case TypeCode.UInt64:
                        return ConvertToNullableUInt64(value);
                }
                if (typeof (Guid) == GetUnderlyingType(targetType)) return ConvertToNullableGuid(value);
            }

            switch (Type.GetTypeCode(targetType)) {
                case TypeCode.Boolean:
                    return ConvertToBoolean(value);
                case TypeCode.Byte:
                    return ConvertToByte(value);
                case TypeCode.Char:
                    return ConvertToChar(value);
                case TypeCode.DateTime:
                    return ConvertToDateTime(value);
                case TypeCode.Decimal:
                    return ConvertToDecimal(value);
                case TypeCode.Double:
                    return ConvertToDouble(value);
                case TypeCode.Int16:
                    return ConvertToInt16(value);
                case TypeCode.Int32:
                    return ConvertToInt32(value);
                case TypeCode.Int64:
                    return ConvertToInt64(value);
                case TypeCode.SByte:
                    return ConvertToSByte(value);
                case TypeCode.Single:
                    return ConvertToSingle(value);
                case TypeCode.String:
                    return ConvertToString(value);
                case TypeCode.UInt16:
                    return ConvertToUInt16(value);
                case TypeCode.UInt32:
                    return ConvertToUInt32(value);
                case TypeCode.UInt64:
                    return ConvertToUInt64(value);
            }

            if (typeof (Guid) == targetType) return ConvertToGuid(value);
#if !SILVERLIGHT
            if (typeof (XmlDocument) == targetType) return ConvertToXmlDocument(value);
#endif
#if !SILVERLIGHT && !NET_2_0
            if (typeof (XDocument) == targetType) return ConvertToXDocument(value);
            if (typeof (XElement) == targetType) return ConvertToXElement(value);
#endif
            if (typeof (byte[]) == targetType) return ConvertToByteArray(value);
            if (typeof (char[]) == targetType) return ConvertToCharArray(value);

#if !SILVERLIGHT
            if (typeof (SqlInt32) == targetType) return ConvertToSqlInt32(value);
            if (typeof (SqlString) == targetType) return ConvertToSqlString(value);
            if (typeof (SqlDecimal) == targetType) return ConvertToSqlDecimal(value);
            if (typeof (SqlDateTime) == targetType) return ConvertToSqlDateTime(value);
            if (typeof (SqlBoolean) == targetType) return ConvertToSqlBoolean(value);
            if (typeof (SqlMoney) == targetType) return ConvertToSqlMoney(value);
            if (typeof (SqlGuid) == targetType) return ConvertToSqlGuid(value);
            if (typeof (SqlDouble) == targetType) return ConvertToSqlDouble(value);
            if (typeof (SqlByte) == targetType) return ConvertToSqlByte(value);
            if (typeof (SqlInt16) == targetType) return ConvertToSqlInt16(value);
            if (typeof (SqlInt64) == targetType) return ConvertToSqlInt64(value);
            if (typeof (SqlSingle) == targetType) return ConvertToSqlSingle(value);
            if (typeof (SqlBinary) == targetType) return ConvertToSqlBinary(value);
#endif
            if (value == null)
                return null;
            //Check whether the target Type can be assigned from the value's Type
            if (targetType.IsAssignableFrom(value.GetType()))
                return value; //Skip further adapting

            //Try to convert using a type converter
            var typeConverter = ReflectionUtils.GetTypeConverter(targetType);
                // TypeDescriptor.GetConverter(targetType);
            if (typeConverter != null && typeConverter.CanConvertFrom(value.GetType()))
                return typeConverter.ConvertFrom(value);
            //Custom type converters handled here (for example ByteArray)
            typeConverter = ReflectionUtils.GetTypeConverter(value); // TypeDescriptor.GetConverter(value);
            if (typeConverter != null && typeConverter.CanConvertTo(targetType))
                return typeConverter.ConvertTo(value, targetType);

            if (targetType.IsInterface) {
                var castMethod =
                    typeof (TypeHelper).GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(targetType);
                var castedObject = castMethod.Invoke(null, new object[] {value});
                if (castedObject != null)
                    return castedObject;
            }
            //Collections
            if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.Generic.ICollection`1")
                && value is IList) {
                object obj = null;
                if (CollectionUtils.IsListType(targetType))
                    obj = CollectionUtils.CreateList(targetType);
                if (obj == null)
                    obj = CreateInstance(targetType);
                if (obj != null) {
                    //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                    var typeParameters = ReflectionUtils.GetGenericArguments(targetType);
                    if (typeParameters != null && typeParameters.Length == 1) {
                        //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                        var typeGenericICollection = targetType.GetInterface(
                            "System.Collections.Generic.ICollection`1", true);
                        var miAddCollection = typeGenericICollection.GetMethod("Add");
                        var source = value as IList;
                        for (var i = 0; i < (value as IList).Count; i++)
                            miAddCollection.Invoke(obj, new object[] {ChangeType(source[i], typeParameters[0])});
                    } else {
#if !SILVERLIGHT
                        if (Log.IsErrorEnabled)
                            Log.Error(string.Format("{0} type arguments of the generic type {1} expecting 1.",
                                typeParameters.Length, targetType.FullName));
#endif
                    }
                    return obj;
                }
            }
            if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.IList") && value is IList) {
                var obj = CreateInstance(targetType);
                if (obj != null) {
                    var source = value as IList;
                    var destination = obj as IList;
                    for (var i = 0; i < source.Count; i++)
                        destination.Add(source[i]);
                    return obj;
                }
            }
            if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.Generic.IDictionary`2")
                && value is IDictionary) {
                var obj = CreateInstance(targetType);
                if (obj != null) {
                    var source = value as IDictionary;
                    var typeParameters = ReflectionUtils.GetGenericArguments(targetType);
                    if (typeParameters != null && typeParameters.Length == 2) {
                        //For generic interfaces, the name parameter is the mangled name, ending with a grave accent (`) and the number of type parameters
                        var typeGenericIDictionary = targetType.GetInterface(
                            "System.Collections.Generic.IDictionary`2", true);
                        var miAddCollection = typeGenericIDictionary.GetMethod("Add");
                        var dictionary = value as IDictionary;
                        foreach (DictionaryEntry entry in dictionary) {
                            miAddCollection.Invoke(obj, new object[] {
                                ChangeType(entry.Key, typeParameters[0]),
                                ChangeType(entry.Value, typeParameters[1])
                            });
                        }
                    } else {
#if !SILVERLIGHT
                        if (Log.IsErrorEnabled)
                            Log.Error(string.Format("{0} type arguments of the generic type {1} expecting 1.",
                                typeParameters.Length, targetType.FullName));
#endif
                    }
                    return obj;
                }
            }

            if (ReflectionUtils.ImplementsInterface(targetType, "System.Collections.IDictionary")
                && value is IDictionary) {
                var obj = CreateInstance(targetType);
                if (obj != null) {
                    var source = value as IDictionary;
                    var destination = obj as IDictionary;
                    foreach (DictionaryEntry entry in source)
                        destination.Add(entry.Key, entry.Value);
                    return obj;
                }
            }

            return Convert.ChangeType(value, targetType, null);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 8-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 8-bit signed integer.</returns>
        
        public static sbyte? ConvertToNullableSByte(object value)
        {
            if (value is sbyte) return (sbyte?) value;
            if (value == null) return null;
            return Util.Convert.ToNullableSByte(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 16-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 16-bit signed integer.</returns>
        public static short? ConvertToNullableInt16(object value)
        {
            if (value is short) return (short?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableInt16(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 32-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 32-bit signed integer.</returns>
        public static int? ConvertToNullableInt32(object value)
        {
            if (value is int) return (int?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableInt32(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 64-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 64-bit signed integer.</returns>
        public static long? ConvertToNullableInt64(object value)
        {
            if (value is long) return (long?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableInt64(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 8-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 8-bit unsigned integer.</returns>
        public static byte? ConvertToNullableByte(object value)
        {
            if (value is byte) return (byte?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableByte(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 16-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 16-bit unsigned integer.</returns>
        
        public static ushort? ConvertToNullableUInt16(object value)
        {
            if (value is ushort) return (ushort?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableUInt16(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 32-bit unsigned integer.</returns>
        
        public static uint? ConvertToNullableUInt32(object value)
        {
            if (value is uint) return (uint?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableUInt32(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable 64-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable 64-bit unsigned integer.</returns>
        
        public static ulong? ConvertToNullableUInt64(object value)
        {
            if (value is ulong) return (ulong?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableUInt64(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable Unicode character.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable Unicode character.</returns>
        public static char? ConvertToNullableChar(object value)
        {
            if (value is char) return (char?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableChar(value);
        }

        /// <summary>
        /// Checks whether the specified Object can be converted to a nullable Unicode character.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>true if the specified Object can be converted to a nullable Unicode character, false otherwise.</returns>
        public static bool CanConvertToNullableChar(object value)
        {
            if (value is char) return true;
            if (value == null) return true;
            return Util.Convert.CanConvertToNullableChar(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable double-precision floating point number.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable double-precision floating point number.</returns>
        public static double? ConvertToNullableDouble(object value)
        {
            if (value is double) return (double?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableDouble(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable single-precision floating point number.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable single-precision floating point number.</returns>
        public static float? ConvertToNullableSingle(object value)
        {
            if (value is float) return (float?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableSingle(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent to a nullable Boolean value.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable Boolean value.</returns>
        public static bool? ConvertToNullableBoolean(object value)
        {
            if (value is bool) return (bool?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableBoolean(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable DateTime.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable DateTime.</returns>
        public static DateTime? ConvertToNullableDateTime(object value)
        {
            if (value is DateTime) return (DateTime?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableDateTime(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable Decimal.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable Decimal.</returns>
        public static decimal? ConvertToNullableDecimal(object value)
        {
            if (value is decimal) return (decimal?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableDecimal(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent nullable Guid.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent nullable Guid.</returns>
        public static Guid? ConvertToNullableGuid(object value)
        {
            if (value is Guid) return (Guid?) value;
            if (value == null) return null;

            return Util.Convert.ToNullableGuid(value);
        }

        /// <summary>
        /// Checks whether the specified Object can be converted to a nullable Guid.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>true if the specified Object can be converted to a nullable Guid, false otherwise.</returns>
        public static bool CanConvertToNullableGuid(object value)
        {
            if (value is Guid) return true;
            if (value == null) return true;
            return Util.Convert.CanConvertToNullableGuid(value);
        }

        private static readonly sbyte DefaultSByteNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 8-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 8-bit signed integer.</returns>
        
        public static sbyte ConvertToSByte(object value)
        {
            return
                value is sbyte
                    ? (sbyte) value
                    : value == null
                        ? DefaultSByteNullValue
                        : Util.Convert.ToSByte(value);
        }

        private static readonly short DefaultInt16NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 16-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 16-bit signed integer.</returns>
        public static short ConvertToInt16(object value)
        {
            return
                value is short
                    ? (short) value
                    : value == null
                        ? DefaultInt16NullValue
                        : Util.Convert.ToInt16(value);
        }

        private static readonly int DefaultInt32NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 32-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 32-bit signed integer.</returns>
        public static int ConvertToInt32(object value)
        {
            return
                value is int
                    ? (int) value
                    : value == null
                        ? DefaultInt32NullValue
                        : Util.Convert.ToInt32(value);
        }

        private static readonly long DefaultInt64NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 64-bit signed integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 64-bit signed integer.</returns>
        public static long ConvertToInt64(object value)
        {
            return
                value is long
                    ? (long) value
                    : value == null
                        ? DefaultInt64NullValue
                        : Util.Convert.ToInt64(value);
        }

        private static readonly byte DefaultByteNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 8-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 8-bit unsigned integer.</returns>
        public static byte ConvertToByte(object value)
        {
            return
                value is byte
                    ? (byte) value
                    : value == null
                        ? DefaultByteNullValue
                        : Util.Convert.ToByte(value);
        }

        private static readonly ushort DefaultUInt16NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 16-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 16-bit unsigned integer.</returns>
        
        public static ushort ConvertToUInt16(object value)
        {
            return
                value is ushort
                    ? (ushort) value
                    : value == null
                        ? DefaultUInt16NullValue
                        : Util.Convert.ToUInt16(value);
        }

        private static readonly uint DefaultUInt32NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 32-bit unsigned integer.</returns>
        
        public static uint ConvertToUInt32(object value)
        {
            return
                value is uint
                    ? (uint) value
                    : value == null
                        ? DefaultUInt32NullValue
                        : Util.Convert.ToUInt32(value);
        }

        private static readonly ulong DefaultUInt64NullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent 64-bit unsigned integer.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent 64-bit unsigned integer.</returns>
        
        public static ulong ConvertToUInt64(object value)
        {
            return
                value is ulong
                    ? (ulong) value
                    : value == null
                        ? DefaultUInt64NullValue
                        : Util.Convert.ToUInt64(value);
        }

        private static readonly char DefaultCharNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent Unicode character.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent Unicode character.</returns>
        public static char ConvertToChar(object value)
        {
            return
                value is char
                    ? (char) value
                    : value == null
                        ? DefaultCharNullValue
                        : Util.Convert.ToChar(value);
        }

        /// <summary>
        /// Checks whether the specified Object can be converted to a Unicode character.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>true if the specified Object can be converted to a Unicode character, false otherwise.</returns>
        public static bool CanConvertToChar(object value)
        {
            return
                value is char
                    ? true
                    : value == null
                        ? true
                        : Util.Convert.CanConvertToChar(value);
        }

        private static readonly float DefaultSingleNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent single-precision floating point number.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent single-precision floating point number.</returns>
        public static float ConvertToSingle(object value)
        {
            return
                value is float
                    ? (float) value
                    : value == null
                        ? DefaultSingleNullValue
                        : Util.Convert.ToSingle(value);
        }

        private static readonly double DefaultDoubleNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent double-precision floating point number.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent double-precision floating point number.</returns>
        public static double ConvertToDouble(object value)
        {
            return
                value is double
                    ? (double) value
                    : value == null
                        ? DefaultDoubleNullValue
                        : Util.Convert.ToDouble(value);
        }

        private static readonly bool DefaultBooleanNullValue;

        /// <summary>
        /// Checks whether the specified Object can be converted to a Boolean value.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent Boolean value.</returns>
        public static bool ConvertToBoolean(object value)
        {
            return
                value is bool
                    ? (bool) value
                    : value == null
                        ? DefaultBooleanNullValue
                        : Util.Convert.ToBoolean(value);
        }

        private static readonly string DefaultStringNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent String.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent String.</returns>
        public static string ConvertToString(object value)
        {
            return
                value is string
                    ? (string) value
                    : value == null
                        ? DefaultStringNullValue
                        : Util.Convert.ToString(value);
        }

        private static readonly DateTime DefaultDateTimeNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent DateTime.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent DateTime.</returns>
        public static DateTime ConvertToDateTime(object value)
        {
            return
                value is DateTime
                    ? (DateTime) value
                    : value == null
                        ? DefaultDateTimeNullValue
                        : Util.Convert.ToDateTime(value);
        }

        private static readonly decimal DefaultDecimalNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent Decimal.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent Decimal.</returns>
        public static decimal ConvertToDecimal(object value)
        {
            return
                value is decimal
                    ? (decimal) value
                    : value == null
                        ? DefaultDecimalNullValue
                        : Util.Convert.ToDecimal(value);
        }

        private static readonly Guid DefaultGuidNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent Guid.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent Guid.</returns>
        public static Guid ConvertToGuid(object value)
        {
            return
                value is Guid
                    ? (Guid) value
                    : value == null
                        ? DefaultGuidNullValue
                        : Util.Convert.ToGuid(value);
        }

        /// <summary>
        /// Checks whether the specified Object can be converted to a Guid.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>true if the specified Object can be converted to a Guid, false otherwise.</returns>
        public static bool CanConvertToGuid(object value)
        {
            return
                value is Guid
                    ? true
                    : value == null
                        ? true
                        : Util.Convert.CanConvertToGuid(value);
        }

        private static readonly XmlReader DefaultXmlReaderNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent XmlReader.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent XmlReader.</returns>
        public static XmlReader ConvertToXmlReader(object value)
        {
            return
                value is XmlReader
                    ? (XmlReader) value
                    : value == null
                        ? DefaultXmlReaderNullValue
                        : Util.Convert.ToXmlReader(value);
        }

#if !SILVERLIGHT
        private static readonly XmlDocument DefaultXmlDocumentNullValue;

        /// <summary>
        /// Converts the value of the specified Object to its equivalent XmlDocument.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent XmlDocument.</returns>
        public static XmlDocument ConvertToXmlDocument(object value)
        {
            return
                value is XmlDocument
                    ? (XmlDocument) value
                    : value == null
                        ? DefaultXmlDocumentNullValue
                        : Util.Convert.ToXmlDocument(value);
        }
#endif
#if !NET_2_0
        private static XDocument _defaultXDocumentNullValue;

        public static XDocument ConvertToXDocument(object value)
        {
            return
                value is XDocument
                    ? (XDocument) value
                    : value == null
                        ? _defaultXDocumentNullValue
                        : Util.Convert.ToXDocument(value);
        }

        private static XElement _defaultXElementNullValue;

        public static XElement ConvertToXElement(object value)
        {
            return
                value is XElement
                    ? (XElement) value
                    : value == null
                        ? _defaultXElementNullValue
                        : Util.Convert.ToXElement(value);
        }

#endif

        /// <summary>
        /// Converts the value of the specified Object to a byte array.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The byte array.</returns>
        public static byte[] ConvertToByteArray(object value)
        {
            return
                value is byte[]
                    ? (byte[]) value
                    : value == null
                        ? null
                        : Util.Convert.ToByteArray(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to a character array.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The character array.</returns>
        public static char[] ConvertToCharArray(object value)
        {
            return
                value is char[]
                    ? (char[]) value
                    : value == null
                        ? null
                        : Util.Convert.ToCharArray(value);
        }

#if !SILVERLIGHT
        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlByte.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlByte.</returns>
        public static SqlByte ConvertToSqlByte(object value)
        {
            return
                value == null
                    ? SqlByte.Null
                    : value is SqlByte
                        ? (SqlByte) value
                        : Util.Convert.ToSqlByte(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlInt16.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlInt16.</returns>
        public static SqlInt16 ConvertToSqlInt16(object value)
        {
            return
                value == null
                    ? SqlInt16.Null
                    : value is SqlInt16
                        ? (SqlInt16) value
                        : Util.Convert.ToSqlInt16(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlInt32.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlInt32.</returns>
        public static SqlInt32 ConvertToSqlInt32(object value)
        {
            return
                value == null
                    ? SqlInt32.Null
                    : value is SqlInt32
                        ? (SqlInt32) value
                        : Util.Convert.ToSqlInt32(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlInt64.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlInt64.</returns>
        public static SqlInt64 ConvertToSqlInt64(object value)
        {
            return
                value == null
                    ? SqlInt64.Null
                    : value is SqlInt64
                        ? (SqlInt64) value
                        : Util.Convert.ToSqlInt64(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlSingle.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlSingle.</returns>
        public static SqlSingle ConvertToSqlSingle(object value)
        {
            return
                value == null
                    ? SqlSingle.Null
                    : value is SqlSingle
                        ? (SqlSingle) value
                        : Util.Convert.ToSqlSingle(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlBoolean.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlBoolean.</returns>
        public static SqlBoolean ConvertToSqlBoolean(object value)
        {
            return
                value == null
                    ? SqlBoolean.Null
                    : value is SqlBoolean
                        ? (SqlBoolean) value
                        : Util.Convert.ToSqlBoolean(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlDouble.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlDouble.</returns>
        public static SqlDouble ConvertToSqlDouble(object value)
        {
            return
                value == null
                    ? SqlDouble.Null
                    : value is SqlDouble
                        ? (SqlDouble) value
                        : Util.Convert.ToSqlDouble(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlDateTime.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlDateTime.</returns>
        public static SqlDateTime ConvertToSqlDateTime(object value)
        {
            return
                value == null
                    ? SqlDateTime.Null
                    : value is SqlDateTime
                        ? (SqlDateTime) value
                        : Util.Convert.ToSqlDateTime(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlDecimal.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlDecimal.</returns>
        public static SqlDecimal ConvertToSqlDecimal(object value)
        {
            return
                value == null
                    ? SqlDecimal.Null
                    : value is SqlDecimal
                        ? (SqlDecimal) value
                        : value is SqlMoney
                            ? ((SqlMoney) value).ToSqlDecimal()
                            : Util.Convert.ToSqlDecimal(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlMoney.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlMoney.</returns>
        public static SqlMoney ConvertToSqlMoney(object value)
        {
            return
                value == null
                    ? SqlMoney.Null
                    : value is SqlMoney
                        ? (SqlMoney) value
                        : value is SqlDecimal
                            ? ((SqlDecimal) value).ToSqlMoney()
                            : Util.Convert.ToSqlMoney(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlString.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlString.</returns>
        public static SqlString ConvertToSqlString(object value)
        {
            return
                value == null
                    ? SqlString.Null
                    : value is SqlString
                        ? (SqlString) value
                        : Util.Convert.ToSqlString(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlBinary.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlBinary.</returns>
        public static SqlBinary ConvertToSqlBinary(object value)
        {
            return
                value == null
                    ? SqlBinary.Null
                    : value is SqlBinary
                        ? (SqlBinary) value
                        : Util.Convert.ToSqlBinary(value);
        }

        /// <summary>
        /// Converts the value of the specified Object to its equivalent SqlGuid.
        /// </summary>
        /// <param name="value">An Object.</param>
        /// <returns>The equivalent SqlGuid.</returns>
        public static SqlGuid ConvertToSqlGuid(object value)
        {
            return
                value == null
                    ? SqlGuid.Null
                    : value is SqlGuid
                        ? (SqlGuid) value
                        : Util.Convert.ToSqlGuid(value);
        }
#endif

#if !SILVERLIGHT
        /// <summary>
        /// Converts the specified DataTable to its equivalent ASObject.
        /// </summary>
        /// <param name="dataTable">A DataTable.</param>
        /// <param name="stronglyTyped">Indicates whether the ASObject is strongly typed (AS2 Recordset class).</param>
        /// <returns>The equivalent ASObject.</returns>
        public static ASObject ConvertDataTableToASO(DataTable dataTable, bool stronglyTyped)
        {
            if (dataTable.ExtendedProperties.Contains("DynamicPage"))
                return ConvertPageableDataTableToASO(dataTable, stronglyTyped);
            var recordset = new ASObject();
            if (stronglyTyped)
                recordset.Alias = "RecordSet";

            var asObject = new ASObject();
            if (dataTable.ExtendedProperties["TotalCount"] != null)
                asObject["totalCount"] = (int) dataTable.ExtendedProperties["TotalCount"];
            else
                asObject["totalCount"] = dataTable.Rows.Count;

            if (dataTable.ExtendedProperties["Service"] != null)
                asObject["serviceName"] = "rs://" + dataTable.ExtendedProperties["Service"];
            else
                asObject["serviceName"] = "FluorineFx.PageableResult";
            asObject["version"] = 1;
            asObject["cursor"] = 1;
            if (dataTable.ExtendedProperties["RecordsetId"] != null)
                asObject["id"] = dataTable.ExtendedProperties["RecordsetId"] as string;
            else
                asObject["id"] = null;
            var columnNames = new string[dataTable.Columns.Count];
            for (var i = 0; i < dataTable.Columns.Count; i++) {
                columnNames[i] = dataTable.Columns[i].ColumnName;
            }
            asObject["columnNames"] = columnNames;
            var rows = new object[dataTable.Rows.Count];
            for (var i = 0; i < dataTable.Rows.Count; i++) {
                rows[i] = dataTable.Rows[i].ItemArray;
            }
            asObject["initialData"] = rows;

            recordset["serverInfo"] = asObject;
            return recordset;
        }

        /// <summary>
        /// Converts the specified DataTable to its equivalent ASObject (pageable RecordSet).
        /// </summary>
        /// <param name="dataTable">A DataTable.</param>
        /// <param name="stronglyTyped">Indicates whether the ASObject is strongly typed (AS2 RecordSetPage class).</param>
        /// <returns>The equivalent ASObject.</returns>
        public static ASObject ConvertPageableDataTableToASO(DataTable dataTable, bool stronglyTyped)
        {
            var recordSetPage = new ASObject();
            if (stronglyTyped)
                recordSetPage.Alias = "RecordSetPage";
            recordSetPage["Cursor"] = (int) dataTable.ExtendedProperties["Cursor"]; //pagecursor

            var rows = new ArrayList();
            for (var i = 0; i < dataTable.Rows.Count; i++) {
                rows.Add(dataTable.Rows[i].ItemArray);
            }
            recordSetPage["Page"] = rows;
            ;
            return recordSetPage;
        }

        /// <summary>
        /// Converts the specified DataSet to its equivalent ASObject.
        /// </summary>
        /// <param name="dataSet">A DataSet.</param>
        /// <param name="stronglyTyped">Indicates whether the ASObject is strongly typed (property values of the root ASObject will be AS2 RecordSet objects).</param>
        /// <returns>The equivalent ASObject.</returns>
        public static ASObject ConvertDataSetToASO(DataSet dataSet, bool stronglyTyped)
        {
            var asDataSet = new ASObject();
            if (stronglyTyped)
                asDataSet.Alias = "DataSet";
            var dataTableCollection = dataSet.Tables;
            foreach (DataTable dataTable in dataTableCollection) {
                asDataSet[dataTable.TableName] = ConvertDataTableToASO(dataTable, stronglyTyped);
            }
            return asDataSet;
        }
#endif
    }
}