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
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using FluorineFx.AMF3;
using FluorineFx.Configuration;
using FluorineFx.Exceptions;
using FluorineFx.Reflection.Lightweight;
using log4net;

namespace FluorineFx.IO.Bytecode.Lightweight
{
	delegate object CreateInstanceInvoker();
    delegate object ReadDataInvoker(AMFReader reader, ClassDefinition classDefinition);

	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
	/// </summary>
	class AMF0ReflectionOptimizer : IReflectionOptimizer
	{
        private static readonly ILog log = LogManager.GetLogger(typeof(AMF0ReflectionOptimizer));
		private CreateInstanceInvoker _createInstanceMethod;
		private ReadDataInvoker _readDataMethod;
#if !(MONO) && !(NET_2_0) && !(NET_3_5) && !(SILVERLIGHT)
        PermissionSet _ps;
#endif

        public AMF0ReflectionOptimizer(Type type, AMFReader reader, object instance)
		{
            _createInstanceMethod = CreateCreateInstanceMethod(type);
#if !(MONO) && !(NET_2_0) && !(NET_3_5) && !(SILVERLIGHT)
            _ps = new PermissionSet(PermissionState.None);
            _ps.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
#endif
            _readDataMethod = CreateReadDataMethod(type, reader, instance);
        }

        private CreateInstanceInvoker CreateCreateInstanceMethod(Type type)
        {
            var method = new DynamicMethod(string.Empty, typeof(object), null, type, true);
            var il = method.GetILGenerator();

            var constructor = type.GetConstructor(EmitHelper.AnyVisibilityInstance, null, CallingConventions.HasThis, Type.EmptyTypes, null);
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);
            return (CreateInstanceInvoker)method.CreateDelegate(typeof(CreateInstanceInvoker));
        }

        protected virtual ReadDataInvoker CreateReadDataMethod(Type type, AMFReader reader, object instance)
        {
#if !(MONO) && !(NET_2_0) && !(NET_3_5) && !(SILVERLIGHT)
            var canSkipChecks = _ps.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
#else
            bool canSkipChecks = SecurityManager.IsGranted(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
#endif

            var method = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(AMFReader), typeof(ClassDefinition) }, GetType(), canSkipChecks);
            var il = method.GetILGenerator();

            var instanceLocal = il.DeclareLocal(type);//[0] instance
            var typeCodeLocal = il.DeclareLocal(typeof(byte));//[1] uint8 typeCode
            var keyLocal = il.DeclareLocal(typeof(string));//[2] string key
            var objTmp = il.DeclareLocal(typeof(object));//[3] temp object store
            var intTmp1 = il.DeclareLocal(typeof(int));//[4] temp int store, length
            var intTmp2 = il.DeclareLocal(typeof(int));//[5] temp int store, index
            var objTmp2 = il.DeclareLocal(typeof(object));//[6] temp object store
            var typeTmp = il.DeclareLocal(typeof(Type));//[7] temp Type store

            var emit = new EmitHelper(il);
            var typeConstructor = type.GetConstructor(EmitHelper.AnyVisibilityInstance, null, CallingConventions.HasThis, Type.EmptyTypes, null);
            var miAddReference = typeof(AMFReader).GetMethod("AddReference");
            var miReadString = typeof(AMFReader).GetMethod("ReadString");
            var miReadByte = typeof(AMFReader).GetMethod("ReadByte");
            emit
                //object instance = new object();
                .newobj(typeConstructor) //Create the new instance and push the object reference onto the evaluation stack
                .stloc_0 //Pop from the top of the evaluation stack and store it in a the local variable list at index 0
                //reader.AddReference(instance);
                .ldarg_0 //Push the argument indexed at 1 onto the evaluation stack 'reader'
                .ldloc_0 //Loads the local variable at index 0 onto the evaluation stack 'instance'
                .callvirt(miAddReference) //Arguments are popped from the stack, the method call is performed, return value is pushed onto the stack
                //typeCode = 0;
                .ldc_i4_0 //Push the integer value of 0 onto the evaluation stack as an int32
                .stloc_1 //Pop and store it in a the local variable list at index 1
                //string key = null;
                .ldnull //Push a null reference onto the evaluation stack
                .stloc_2 //Pop and store it in a the local variable list at index 2 'key'
                .end()
            ;

			var key = reader.ReadString();
            for (var typeCode = reader.ReadByte(); typeCode != AMF0TypeCode.EndOfObject; typeCode = reader.ReadByte())
            {
                emit
                    .ldarg_0
                    .callvirt(miReadString)
                    .stloc_2
                    .ldarg_0
                    .callvirt(miReadByte)
                    .stloc_1
                    .end()
                ;

                var value = reader.ReadData(typeCode);
                reader.SetMember(instance, key, value);

                var memberInfos = type.GetMember(key);
                if (memberInfos != null && memberInfos.Length > 0)
                    GeneratePropertySet(emit, typeCode, memberInfos[0]);
                else
                {
                    //Log this error (do not throw exception), otherwise our current AMF stream becomes unreliable
                    log.Warn(String.Format(Resources.Optimizer_Warning));
                    var msg = String.Format(Resources.Reflection_MemberNotFound, string.Format("{0}.{1}", type.FullName, key));
                    log.Warn(msg);
                    //reader.ReadAMF3Data(typeCode);
                    emit
                        .ldarg_0 //Push 'reader'
                        .ldloc_1 //Push 'typeCode'
                        .callvirt(typeof(AMFReader).GetMethod("ReadData", new[] { typeof(byte) }))
                        .pop
                        .end()
                    ;
                }

                key = reader.ReadString();
            }
            var labelExit = emit.DefineLabel();
            var exceptionConstructor = typeof(UnexpectedAMF).GetConstructor(EmitHelper.AnyVisibilityInstance, null, CallingConventions.HasThis, Type.EmptyTypes, null);
            //key = reader.ReadString();
            emit
                .ldarg_0 //Push 'reader'
                .callvirt(miReadString)
                .stloc_2 //Pop 'key'
                //typeCode = reader.ReadByte();
                .ldarg_0 //Push 'reader'
                .callvirt(miReadByte)
                .stloc_1 //Pop 'typeCode'
                .ldloc_1
                .ldc_i4_s(AMF0TypeCode.EndOfObject)
                .ceq
                .brtrue_s(labelExit)
                //if( typeCode != AMF0TypeCode.EndOfObject ) throw new UnexpectedAMF();
                .newobj(exceptionConstructor)
                .@throw
                .end()
            ;
            emit
                .MarkLabel(labelExit)
                //return instance;
                .ldloc_0 //Load the local variable at index 0 onto the evaluation stack
                .ret() //Return
            ;

            return (ReadDataInvoker)method.CreateDelegate(typeof(ReadDataInvoker));
        }

        protected bool DoTypeCheck()
        {
            return FluorineConfiguration.Instance.OptimizerSettings.TypeCheck;
        }

        private void GeneratePropertySet(EmitHelper emit, int typeCode, MemberInfo memberInfo)
        {
            Type memberType = null;
            if (memberInfo.MemberType == MemberTypes.Property)
            {
                var propertyInfo = memberInfo.DeclaringType.GetProperty(memberInfo.Name);
                memberType = propertyInfo.PropertyType;
            }
            if (memberInfo is FieldInfo)
            {
                var fieldInfo = memberInfo.DeclaringType.GetField(memberInfo.Name);
                memberType = fieldInfo.FieldType;
            }
            if (memberType == null)
                throw new ArgumentNullException(memberInfo.Name);

            //The primitive types are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, Char, Double, Single
            //We handle here Decimal types too
            if (memberType.IsPrimitive || memberType == typeof(decimal))
            {
                var primitiveTypeCode = Type.GetTypeCode(memberType);
                switch (primitiveTypeCode)
                {
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        {
                            var labelNotNumber = emit.ILGenerator.DefineLabel();
                            var labelExit = emit.ILGenerator.DefineLabel();

                            //if( typeCode == AMF0TypeCode.Number )
                            emit
                                .ldloc_1 //Push 'typeCode'
                                .ldc_i4(AMF0TypeCode.Number)
                                .ceq
                                .brfalse_s(labelNotNumber)
                                //instance.{0} = ({1})reader.ReadDouble();
                                .ldloc_0 //Push 'instance'
                                .ldarg_0 //Push 'reader'
                                .callvirt(typeof(AMFReader).GetMethod("ReadDouble"))
                                .GeneratePrimitiveCast(primitiveTypeCode)
                                .GenerateSetMember(memberInfo)
                                .br_s(labelExit)
                                .MarkLabel(labelNotNumber)
                                .GenerateThrowUnexpectedAMFException(memberInfo)
                                .MarkLabel(labelExit)
                                //.nop
                                .end()
                            ;
                        }
                        break;
                    case TypeCode.Boolean:
                        {
                            var labelNotBoolean = emit.ILGenerator.DefineLabel();
                            var labelExit = emit.ILGenerator.DefineLabel();

                            //if( typeCode == AMF0TypeCode.Boolean )
                            emit
                                .ldloc_1 //Push 'typeCode'
                                .ldc_i4(AMF0TypeCode.Boolean)
                                .ceq
                                .brfalse_s(labelNotBoolean)
                                //instance.{0} = ({1})reader.ReadBoolean();
                                .ldloc_0 //Push 'instance'
                                .ldarg_0 //Push 'reader'
                                .callvirt(typeof(AMFReader).GetMethod("ReadBoolean"))
                                .GeneratePrimitiveCast(primitiveTypeCode)
                                .GenerateSetMember(memberInfo)
                                .br_s(labelExit)
                                .MarkLabel(labelNotBoolean)
                                .GenerateThrowUnexpectedAMFException(memberInfo)
                                .MarkLabel(labelExit)
                                .end()
                            ;
                        }
                        break;
                    case TypeCode.Char:
                        {
                            {
                                var labelNotString = emit.ILGenerator.DefineLabel();
                                var labelExit = emit.ILGenerator.DefineLabel();
                                //if( typeCode == AMF0TypeCode.String )
                                emit
                                    .ldloc_1 //Push 'typeCode'
                                    .ldc_i4(AMF0TypeCode.String)
                                    .ceq
                                    .brfalse_s(labelNotString)
                                    //instance.member = reader.ReadString()[0];
                                    .ldarg_0 //Push 'reader'
                                    .callvirt(typeof(AMFReader).GetMethod("ReadString"))
                                    .stloc_2
                                    .ldloc_2 //Push 'key'
                                    .brfalse_s(labelNotString) // Branch if 'key' is null
                                    .ldloc_2 //Push strTmp
                                    .ldsfld(typeof(string).GetField("Empty"))
                                    .call(typeof(string).GetMethod("op_Inequality", new[] { typeof(string), typeof(string) }))
                                    .brfalse_s(labelNotString)
                                    .ldloc_0 //Push 'instance'
                                    .ldloc_2 //Push 'key'
                                    .ldc_i4_0 //Push char index 0
                                    .callvirt(typeof(string).GetMethod("get_Chars", new[] { typeof(Int32) }))
                                    .GenerateSetMember(memberInfo)
                                    .br_s(labelExit)
                                    .MarkLabel(labelNotString)
                                    .GenerateThrowUnexpectedAMFException(memberInfo)
                                    .MarkLabel(labelExit)
                                    .end()
                                ;
                            }
                        }
                        break;
                }
                return;
            }
            if (memberType.IsEnum)
            {
                var labelNotStringOrNumber = emit.ILGenerator.DefineLabel();
                    var labelExit = emit.ILGenerator.DefineLabel();
                    var labelReadDouble = emit.ILGenerator.DefineLabel();
                    //if( typeCode == AMF0TypeCode.String || typeCode == AMF0TypeCode.Number )
                    emit
                        .ldloc_1 //Push 'typeCode'
                        .brfalse_s(labelReadDouble) //Branch if 0 (AMF0TypeCode.Number)
                        .ldloc_1 //Push 'typeCode'
                        .ldc_i4(AMF0TypeCode.String)
                        .ceq
                        .brfalse_s(labelNotStringOrNumber)
                        //we have a string
                        .ldloc_0 //Push 'instance'
                        .ldtoken(memberType)
                        .call(typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }))
                        .ldarg_0 //Push 'reader'
                        .callvirt(typeof(AMFReader).GetMethod("ReadString"))
                        .ldc_i4_1
                        .call(typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) }))
                        .unbox_any(memberType)
                        .GenerateSetMember(memberInfo)
                        .br_s(labelExit)
                        .MarkLabel(labelReadDouble)
                        //we have a number
                        .ldloc_0 //Push 'instance'
                        .ldtoken(memberType)
                        .call(typeof(Type).GetMethod("GetTypeFromHandle"))
                        .ldarg_0 //Push 'reader'
                        .callvirt(typeof(AMFReader).GetMethod("ReadDouble"))
                        .conv_i4
                        .call(typeof(Enum).GetMethod("ToObject", new[] { typeof(Type), typeof(Int32) }))
                        .unbox_any(memberType)
                        .GenerateSetMember(memberInfo)
                        .br_s(labelExit)
                        .MarkLabel(labelNotStringOrNumber)
                        .GenerateThrowUnexpectedAMFException(memberInfo)
                        .MarkLabel(labelExit)
                        .end()
                    ;
                    return;
            }
            if (memberType == typeof(DateTime))
            {
                var labelNotDate = emit.ILGenerator.DefineLabel();
                var labelExit = emit.ILGenerator.DefineLabel();
                //if( typeCode == AMF0TypeCode.DateTime )
                emit
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.DateTime)
                    .ceq
                    .brfalse_s(labelNotDate)
                    .ldloc_0 //Push 'instance'
                    .ldarg_0 //Push 'reader'
                    .callvirt(typeof(AMFReader).GetMethod("ReadDateTime"))
                    .GenerateSetMember(memberInfo)
                    .br_s(labelExit)
                    .MarkLabel(labelNotDate)
                    .GenerateThrowUnexpectedAMFException(memberInfo)
                    .MarkLabel(labelExit)
                    .end()
                ;
                return;
            }
            if (memberType == typeof(string))
            {
                var labelNotStringOrNull = emit.ILGenerator.DefineLabel();
                var labelSetNull = emit.ILGenerator.DefineLabel();
                var labelExit = emit.ILGenerator.DefineLabel();
                var labelReadString = emit.ILGenerator.DefineLabel();
                var labelReadLongString = emit.ILGenerator.DefineLabel();
                emit
                    //if( typeCode == AMF0TypeCode.String || typeCode == AMF0TypeCode.LongString || typeCode == AMF0TypeCode.Null || typeCode == AMF0TypeCode.Undefined )
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.String)
                    .ceq
                    .brtrue_s(labelReadString)
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.LongString)
                    .ceq
                    .brtrue_s(labelReadLongString)
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.Null)
                    .ceq
                    .brtrue_s(labelSetNull)
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.Undefined)
                    .ceq
                    .brtrue_s(labelSetNull)
                    .br_s(labelNotStringOrNull)
                    .MarkLabel(labelReadString)
                    .ldloc_0 //Push 'instance'
                    .ldarg_0 //Push 'reader'
                    .callvirt(typeof(AMFReader).GetMethod("ReadString"))
                    .GenerateSetMember(memberInfo)
                    .br_s(labelExit)
                    .MarkLabel(labelReadLongString)
                    .ldloc_0 //Push 'instance'
                    .ldarg_0 //Push 'reader'
                    .callvirt(typeof(AMFReader).GetMethod("ReadLongString"))
                    .GenerateSetMember(memberInfo)
                    .br_s(labelExit)
                    .MarkLabel(labelSetNull)
                    .ldloc_0 //Push 'instance'
                    .ldc_i4_0
                    .GenerateSetMember(memberInfo)
                    .br_s(labelExit)
                    .MarkLabel(labelNotStringOrNull)
                    .GenerateThrowUnexpectedAMFException(memberInfo)
                    .MarkLabel(labelExit)
                    .end()
                ;
                return;
            }
            if (memberType == typeof(Guid))
            {
                var labelNotString = emit.ILGenerator.DefineLabel();
                var labelExit = emit.ILGenerator.DefineLabel();
                emit
                    //if( typeCode == AMF0TypeCode.String )
                    .ldloc_1 //Push 'typeCode'
                    .ldc_i4(AMF0TypeCode.String)
                    .ceq
                    .brfalse_s(labelNotString)
                    .ldloc_0 //Push 'instance'
                    .ldarg_0 //Push 'reader'
                    .callvirt(typeof(AMFReader).GetMethod("ReadString"))
                    .newobj(typeof(Guid).GetConstructor(EmitHelper.AnyVisibilityInstance, null, CallingConventions.HasThis, new[] { typeof(string) }, null))
                    .GenerateSetMember(memberInfo)
                    .br_s(labelExit)
                    .MarkLabel(labelNotString)
                    .GenerateThrowUnexpectedAMFException(memberInfo)
                    .MarkLabel(labelExit)
                    .end()
                ;
                return;
            }
            if (memberType.IsValueType)
            {
                //structs are not handled
                throw new FluorineException("Struct value types are not supported");
            }

            //instance.member = (type)TypeHelper.ChangeType(reader.ReadData(typeCode), typeof(member));
            emit
                .ldloc_0 //Push 'instance'
                .ldarg_0 //Push 'reader'
                .ldloc_1 //Push 'typeCode'
                .callvirt(typeof(AMFReader).GetMethod("ReadData", new[] { typeof(byte) }))
                .ldtoken(memberType)
                .call(typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }))
                .call(typeof(TypeHelper).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }))
                .CastFromObject(memberType)
                .GenerateSetMember(memberInfo)
                .end()
            ;
        }

	    public object CreateInstance()
		{
			return _createInstanceMethod();
		}

        public virtual object ReadData(AMFReader reader, ClassDefinition classDefinition)
        {
            return _readDataMethod(reader, classDefinition);
        }
	}
}
