using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using FluorineFx.Exceptions;
using FluorineFx.Util;

namespace FluorineFx
{
    /// <summary>
    /// Provides a type converter to convert ASObject objects to and from various other representations.
    /// </summary>
    public class ASObjectConverter : TypeConverter
    {
        /// <summary>
        /// Overloaded. Returns whether this converter can convert the object to the specified type.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="destinationType">A Type that represents the type you want to convert to.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType.IsValueType || destinationType.IsEnum)
                return false;
            if (!ReflectionUtils.IsInstantiatableType(destinationType))
                return false;
            return true;
        }

        /// <summary>
        /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to.</param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            var aso = value as ASObject;
            if (!ReflectionUtils.IsInstantiatableType(destinationType))
                return null;

            var instance = TypeHelper.CreateInstance(destinationType);
            if (instance != null) {
                foreach (var memberName in aso.Keys) {
                    var val = aso[memberName];
                    //MemberInfo mi = ReflectionUtils.GetMember(destinationType, key, MemberTypes.Field | MemberTypes.Property);
                    //if (mi != null)
                    //    ReflectionUtils.SetMemberValue(mi, result, aso[key]);

                    PropertyInfo propertyInfo = null;
                    try {
                        propertyInfo = destinationType.GetProperty(memberName);
                    } catch (AmbiguousMatchException) {
                        //To resolve the ambiguity, include BindingFlags.DeclaredOnly to restrict the search to members that are not inherited.
                        propertyInfo = destinationType.GetProperty(memberName,
                            BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.Public
                            | BindingFlags.Instance);
                    }
                    if (propertyInfo != null) {
                        try {
                            val = TypeHelper.ChangeType(val, propertyInfo.PropertyType);
                            if (propertyInfo.CanWrite && propertyInfo.GetSetMethod() != null) {
                                if (propertyInfo.GetIndexParameters() == null
                                    || propertyInfo.GetIndexParameters().Length == 0)
                                    propertyInfo.SetValue(instance, val, null);
                                else {
                                    var msg = String.Format(Resources.Reflection_PropertyIndexFail,
                                        string.Format("{0}.{1}", destinationType.FullName, memberName));
                                    throw new FluorineException(msg);
                                }
                            } else {
                                //string msg = String.Format(Resources.Reflection_PropertyReadOnly, string.Format("{0}.{1}", type.FullName, memberName));
                            }
                        } catch (Exception ex) {
                            var msg = String.Format(Resources.Reflection_PropertySetFail,
                                string.Format("{0}.{1}", destinationType.FullName, memberName), ex.Message);
                            throw new FluorineException(msg);
                        }
                    } else {
                        var fi = destinationType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
                        try {
                            if (fi != null) {
                                val = TypeHelper.ChangeType(val, fi.FieldType);
                                fi.SetValue(instance, val);
                            } else {
                                //string msg = String.Format(Resources.Reflection_MemberNotFound, string.Format("{0}.{1}", destinationType.FullName, memberName));
                            }
                        } catch (Exception ex) {
                            var msg = String.Format(Resources.Reflection_FieldSetFail,
                                string.Format("{0}.{1}", destinationType.FullName, memberName), ex.Message);
                            throw new FluorineException(msg);
                        }
                    }
                }
            }
            return instance;
        }
    }
}