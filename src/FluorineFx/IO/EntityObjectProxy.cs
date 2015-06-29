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

#if !(NET_1_1) && !(NET_2_0) && !(SILVERLIGHT)

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using log4net;
using FluorineFx.AMF3;
using FluorineFx.Configuration;
using FluorineFx.Util;
using FluorineFx.Exceptions;

namespace FluorineFx.IO
{
    class EntityObjectProxy : ObjectProxy
    {
        public override ClassDefinition GetClassDefinition(object instance)
        {
            //EntityObject eo = instance as EntityObject;
            var type = instance.GetType();
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.DeclaredOnly;
            var memberInfos = ReflectionUtils.FindMembers(type, MemberTypes.Property, flags, typeof(System.Runtime.Serialization.DataMemberAttribute));
            var classMemberList = new List<ClassMember>();
            for (var i = 0; i < memberInfos.Length; i++)
            {
                var memberInfo = memberInfos[i];
                var pi = memberInfo as PropertyInfo;
                //Do not serialize EntityReferences
                if (pi.PropertyType.IsSubclassOf(typeof(System.Data.Objects.DataClasses.EntityReference)))
                    continue;
                var attributes = memberInfo.GetCustomAttributes(false);
                var classMember = new ClassMember(memberInfo.Name, flags, memberInfo.MemberType, attributes);
                classMemberList.Add(classMember);
            }
            var customClassName = type.FullName;
            customClassName = FluorineConfiguration.Instance.GetCustomClass(customClassName);
            var classMembers = classMemberList.ToArray();
            var classDefinition = new ClassDefinition(customClassName, classMembers, GetIsExternalizable(instance), GetIsDynamic(instance));
            return classDefinition;
        }
    }
}

#endif
