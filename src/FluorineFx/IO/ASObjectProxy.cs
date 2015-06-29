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
using log4net;
using FluorineFx.AMF3;
using FluorineFx.Exceptions;

namespace FluorineFx.IO
{
    class ASObjectProxy : IObjectProxy
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ASObjectProxy));

        public bool GetIsExternalizable(object instance)
        {
            return false;
        }

        public bool GetIsDynamic(object instance)
        {
            if (instance != null)
            {
                if (instance is ASObject)
                    return (instance as ASObject).IsTypedObject;
                throw new ArgumentException();
            }
            throw new NullReferenceException();
        }

        public ClassDefinition GetClassDefinition(object instance)
        {
            if (instance is ASObject)
            {
                ClassDefinition classDefinition;
                var aso = instance as ASObject;
                if (aso.IsTypedObject)
                {
                    var classMemberList = new ClassMember[aso.Count];
                    var i = 0;
                    foreach (var entry in aso)
                    {
                        var classMember = new ClassMember(entry.Key, BindingFlags.Default, MemberTypes.Custom, null);
                        classMemberList[i] = classMember;
                        i++;
                    }
                    var customClassName = aso.Alias;
                    classDefinition = new ClassDefinition(customClassName, classMemberList, false, false);
                }
                else
                {
                    var customClassName = string.Empty;
                    classDefinition = new ClassDefinition(customClassName, ClassDefinition.EmptyClassMembers, false, true);
                }
                if (Log.IsDebugEnabled)
                    Log.Debug(string.Format("Creating class definition for AS object {0}", aso));
                return classDefinition;
            }
            throw new ArgumentException();
        }

        public object GetValue(object instance, ClassMember member)
        {
            if (instance is ASObject)
            {
                var aso = instance as ASObject;
                if (aso.ContainsKey(member.Name))
                    return aso[member.Name];
                var msg = String.Format(Resources.Reflection_MemberNotFound, string.Format("ASObject[{0}]", member.Name));
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(string.Format("Member {0} not found in AS object {1}", member.Name, aso));
                }
                throw new FluorineException(msg);
            }
            throw new ArgumentException();
        }

        public void SetValue(object instance, ClassMember member, object value)
        {
            if (instance is ASObject)
            {
                var aso = instance as ASObject;
                aso[member.Name] = value;
            }
            throw new ArgumentException();
        }
    }
}
