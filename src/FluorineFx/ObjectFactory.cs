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
using System.Security.Permissions;
using FluorineFx.Collections.Generic;
using FluorineFx.Configuration;
using FluorineFx.Reflection;
using FluorineFx.Reflection.Lightweight;
using FluorineFx.Util;

namespace FluorineFx
{
	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
	/// </summary>
	sealed class ObjectFactory
	{
	    private static volatile ObjectFactory instance;
        private static readonly object SyncRoot = new object();

        private readonly CopyOnWriteDictionary<string, Type> typeCache;
        private readonly CopyOnWriteDictionary<Type, ConstructorInvoker> typeConstructorCache;
        private readonly string[] lacLocations;
        private readonly bool reflectionEmitPermission;

	    private ObjectFactory() 
        {
            lacLocations = TypeHelper.GetLacLocations();
            typeCache = new CopyOnWriteDictionary<string, Type>();
            typeConstructorCache = new CopyOnWriteDictionary<Type, ConstructorInvoker>();
            try
            {
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                reflectionEmitPermission = true;
            }
            catch(Exception ex)
            {
                Unreferenced.Parameter(ex);
                reflectionEmitPermission = false;
            }
        }

        public static ObjectFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (SyncRoot) 
                    {
                        if (instance == null)
                            instance = new ObjectFactory();
                    }
                }
                return instance;
            }
        }

		public Type InternalLocate(string typeName)
		{
			if( string.IsNullOrEmpty(typeName) )
				return null;

            var mappedTypeName = FluorineConfiguration.Instance.GetMappedTypeName(typeName);
			//Lookup first in our cache.
            Type type;
            if (!typeCache.TryGetValue(mappedTypeName, out type))
            {
                type = TypeHelper.Locate(mappedTypeName);
                if (type != null)
                    typeCache[mappedTypeName] = type;
                else
                    type = InternalLocateInLac(mappedTypeName); // Locate in the LAC
            }
            return type;
		}

		public Type InternalLocateInLac(string typeName)
		{
			if( string.IsNullOrEmpty(typeName) )
				return null;

            var mappedTypeName = FluorineConfiguration.Instance.GetMappedTypeName(typeName);
            Type type;
            if (!typeCache.TryGetValue(mappedTypeName, out type))
            {
                //Locate in LAC
                foreach (var t in lacLocations) {
                    type = TypeHelper.LocateInLac(mappedTypeName, t);
                    if (type != null)
                    {
                        typeCache[mappedTypeName] = type;
                        return type;
                    }
                }
            }
            return type;
		}

		internal void AddTypeToCache(Type type)
		{
            if (type != null)
                typeCache[type.FullName] = type;
		}

		public bool ContainsType(string typeName)
		{
            if (string.IsNullOrEmpty(typeName))
                return false;
            return typeCache.ContainsKey(typeName);
		}

        public object InternalCreateInstance(Type type)
		{
            return InternalCreateInstance(type, null);
		}

        public object InternalCreateInstance(string typeName)
        {
            return InternalCreateInstance(typeName, null);
        }

        public object InternalCreateInstance(string typeName, object[] args)
        {
            var type = InternalLocate(typeName);
            return InternalCreateInstance(type, args);
        }

        public object InternalCreateInstance(Type type, object[] args)
		{
            if (type != null)
            {
                if (type.IsAbstract && type.IsSealed)
                    return type;

                if (reflectionEmitPermission)
                {
                    ConstructorInvoker invoker;
                    typeConstructorCache.TryGetValue(type, out invoker);
                    if (invoker == null)
                    {
                        invoker = ConstructorExtensions.DelegateForCreateInstance(type, args);
                        typeConstructorCache[type] = invoker;
                    }
                    return invoker(args);
                }
                return Activator.CreateInstance(type, BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, args, null);
            }
			return null;
		}

        static public Type Locate(string type)
        {
            return Instance.InternalLocate(type);
        }

        static public Type LocateInLac(string type)
        {
            return Instance.InternalLocateInLac(type);
        }

        static public object CreateInstance(Type type)
        {
            return Instance.InternalCreateInstance(type);
        }

        static public object CreateInstance(string type)
        {
            return Instance.InternalCreateInstance(type);
        }

        static public object CreateInstance(Type type, object[] args)
        {
            return Instance.InternalCreateInstance(type, args);
        }
	}
}
