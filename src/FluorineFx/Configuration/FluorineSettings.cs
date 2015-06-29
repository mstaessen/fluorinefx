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
using System.Xml.Serialization;
using FluorineFx.AMF3;
using System.Collections.Generic;
using FluorineFx.Collections.Generic;

namespace FluorineFx.Configuration
{
	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
	[XmlType(TypeName="settings")]
    public sealed class FluorineSettings
	{
		private ClassMappingCollection classMappings;
	    private CustomErrors customErrors;

	    /// <summary>
        /// Initializes a new instance of the FluorineSettings class.
        /// </summary>
		public FluorineSettings()
		{
			TimezoneCompensation = TimezoneCompensation.None;
			RemotingServiceAttribute = RemotingServiceAttributeConstraint.Access;
			AcceptNullValueTypes = false;
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlArray("classMappings")]
		[XmlArrayItem("classMapping",typeof(ClassMapping))]
        public ClassMappingCollection ClassMappings
		{
			get { return classMappings ?? (classMappings = new ClassMappingCollection()); }
		}

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(ElementName = "timezoneCompensation")]
#endif
        public TimezoneCompensation TimezoneCompensation { get; set; }

	    /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(ElementName = "acceptNullValueTypes")]
#endif
        public bool AcceptNullValueTypes { get; set; }

	    /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(ElementName = "remotingServiceAttribute")]
#endif
        public RemotingServiceAttributeConstraint RemotingServiceAttribute { get; set; }

	    /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(ElementName = "optimizer")]
#endif
        public OptimizerSettings Optimizer { get; set; }

	    /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(ElementName = "customErrors")]
#endif
        public CustomErrors CustomErrors
        {
            get { return customErrors ?? (customErrors = new CustomErrors()); }
	        set { customErrors = value; }
        }

	}

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class ClassMappingCollection : CollectionBase<ClassMapping>
    {
        private Dictionary<string, string> typeToCustomClass = new Dictionary<string, string>();
        private Dictionary<string, string> customClassToType = new Dictionary<string, string>();
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ClassMappingCollection()
		{
			Add(typeof(ArrayCollection).FullName, "flex.messaging.io.ArrayCollection");
			Add(typeof(ByteArray).FullName, "flex.messaging.io.ByteArray");
			Add(typeof(ObjectProxy).FullName, "flex.messaging.io.ObjectProxy");
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="customClass"></param>
		public void Add(string type, string customClass)
		{
			var classMapping = new ClassMapping();
			classMapping.Type = type;
			classMapping.CustomClass = customClass;
			this.Add(classMapping);
		}


        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(ClassMapping value)
        {
            typeToCustomClass[value.Type] = value.CustomClass;
            customClassToType[value.CustomClass] = value.Type;
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, ClassMapping value)
        {
            typeToCustomClass[value.Type] = value.CustomClass;
            customClassToType[value.CustomClass] = value.Type;
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        public override bool Remove(ClassMapping value)
        {
            typeToCustomClass.Remove(value.Type);
            customClassToType.Remove(value.CustomClass);
            return base.Remove(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		public string GetCustomClass(string type)
		{
			if( typeToCustomClass.ContainsKey( type ) )
				return typeToCustomClass[type] as string;
			else
				return type;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="customClass"></param>
        /// <returns></returns>
		public string GetType(string customClass)
		{
			if( customClass == null )
				return null;
			if( customClassToType.ContainsKey(customClass) )
				return customClassToType[customClass] as string;
			else
				return customClass;
		}
	}

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !FXCLIENT
    [XmlType(TypeName = "classMapping")]
#endif
    public sealed class ClassMapping
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ClassMapping()
		{
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(DataType = "string", ElementName = "type")]
#endif
		public string Type { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlElement(DataType = "string", ElementName = "customClass")]
#endif
		public string CustomClass { get; set; }
	}

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class ServiceCollection : CollectionBase<ServiceConfiguration>
#else
    public sealed class ServiceCollection : CollectionBase
#endif
    {
#if !(NET_1_1)
        private Dictionary<string, ServiceConfiguration> serviceNames = new Dictionary<string, ServiceConfiguration>(5);
        private Dictionary<string, ServiceConfiguration> serviceLocations = new Dictionary<string, ServiceConfiguration>(5);
#else
        private Hashtable _serviceNames = new Hashtable(5);
        private Hashtable _serviceLocations = new Hashtable(5);
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ServiceCollection()
		{
		}
#if !(NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(ServiceConfiguration value)
        {
            serviceNames[value.Name] = value;
            serviceLocations[value.ServiceLocation] = value;
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, ServiceConfiguration value)
        {
            serviceNames[value.Name] = value;
            serviceLocations[value.ServiceLocation] = value;
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        public override bool Remove(ServiceConfiguration value)
        {
            serviceNames.Remove(value.Name);
            serviceLocations.Remove(value.ServiceLocation);
            return base.Remove(value);
        }
#else
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( ServiceConfiguration value )  
		{
			_serviceNames[value.Name] = value;
			_serviceLocations[value.ServiceLocation] = value;
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, ServiceConfiguration value )  
		{
			_serviceNames[value.Name] = value;
			_serviceLocations[value.ServiceLocation] = value;
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( ServiceConfiguration value )
		{
            _serviceNames.Remove(value.Name);
            _serviceLocations.Remove(value.ServiceLocation);
			List.Remove(value);
		}
#endif

#if (NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( ServiceConfiguration value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( ServiceConfiguration value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public ServiceConfiguration this[ int index ]  
		{
			get  
			{
				return List[index] as ServiceConfiguration;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
		public bool Contains(string serviceName)
		{
			return serviceNames.ContainsKey(serviceName);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
		public string GetServiceLocation(string serviceName)
		{
			if( serviceNames.ContainsKey(serviceName) )
				return (serviceNames[serviceName] as ServiceConfiguration).ServiceLocation;
			else
				return serviceName;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="serviceLocation"></param>
        /// <returns></returns>
		public string GetServiceName(string serviceLocation)
		{
			if( serviceLocations.ContainsKey(serviceLocation) )
				return (serviceLocations[serviceLocation] as ServiceConfiguration).Name;
			else
				return serviceLocation;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
		public string GetMethod(string serviceName, string name)
		{
            ServiceConfiguration serviceConfiguration = null;
            if( serviceNames.ContainsKey(serviceName) )
                serviceConfiguration = serviceNames[serviceName] as ServiceConfiguration;
			if( serviceConfiguration != null )
				return serviceConfiguration.Methods.GetMethod(name);
			return name;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="serviceLocation"></param>
        /// <param name="method"></param>
        /// <returns></returns>
		public string GetMethodName(string serviceLocation, string method)
		{
            ServiceConfiguration serviceConfiguration = null;
            if( serviceLocations.ContainsKey(serviceLocation) )
                serviceConfiguration = serviceLocations[serviceLocation] as ServiceConfiguration;
			if( serviceConfiguration != null )
				return serviceConfiguration.Methods.GetMethodName(method);
			return method;
		}
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [XmlType(TypeName = "service")]
    public sealed class ServiceConfiguration
	{
        private RemoteMethodCollection remoteMethodCollection;
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ServiceConfiguration()
		{
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(DataType = "string", ElementName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(DataType = "string", ElementName = "service-location")]
        public string ServiceLocation { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlArray("methods")]
		[XmlArrayItem("remote-method",typeof(RemoteMethod))]
		public RemoteMethodCollection Methods
		{
			get
			{
				if (remoteMethodCollection == null)
					remoteMethodCollection = new RemoteMethodCollection();
				return remoteMethodCollection;
			}
			//set{ _remoteMethodCollection = value; }
		}
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class RemoteMethodCollection : CollectionBase<RemoteMethod>
#else
    public sealed class RemoteMethodCollection : CollectionBase
#endif
    {
#if !(NET_1_1)
        Dictionary<string, string> methods = new Dictionary<string, string>(3);
        Dictionary<string, string> methodsNames = new Dictionary<string, string>(3);
#else
        Hashtable _methods = new Hashtable(3);
        Hashtable _methodsNames = new Hashtable(3);
#endif

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public RemoteMethodCollection()
		{
		}

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(RemoteMethod value)
        {
            methods[value.Name] = value.Method;
            methodsNames[value.Method] = value.Name;
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, RemoteMethod value)
        {
            methods[value.Name] = value.Method;
            methodsNames[value.Method] = value.Name;
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
		public string GetMethod(string name)
		{
			if( methods.ContainsKey(name) )
				return methods[name] as string;
			return name;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
		public string GetMethodName(string method)
		{
			if( methodsNames.ContainsKey(method) )
				return methodsNames[method] as string;
			return method;
		}
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class RemoteMethod
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(DataType = "string", ElementName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(DataType = "string", ElementName = "method")]
        public string Method { get; set; }
	}
#endif

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class NullableTypeCollection : CollectionBase<NullableType>
#else
    public sealed class NullableTypeCollection : CollectionBase
#endif
    {
#if !(NET_1_1)
        Dictionary<string, NullableType> nullableDictionary = new Dictionary<string, NullableType>(5);
#else
        Hashtable _nullableDictionary = new Hashtable(5);
#endif

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public NullableTypeCollection()
		{
		}
#if !(NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(NullableType value)
        {
            nullableDictionary[value.TypeName] = value;
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, NullableType value)
        {
            nullableDictionary[value.TypeName] = value;
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        public override bool Remove(NullableType value)
        {
            nullableDictionary.Remove(value.TypeName);
            return base.Remove(value);
        }
#else
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( NullableType value )  
		{
			_nullableDictionary[value.TypeName] = value;
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( NullableType value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, NullableType value )  
		{
			_nullableDictionary[value.TypeName] = value;
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( NullableType value )  
		{
			_nullableDictionary.Remove(value.TypeName);
			List.Remove(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( NullableType value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public NullableType this[ int index ]  
		{
			get  
			{
				return List[index] as NullableType;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		public bool ContainsKey( Type type )  
		{
			return nullableDictionary.ContainsKey(type.FullName);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		public object this[ Type type ]  
		{
			get  
			{
				if( nullableDictionary.ContainsKey(type.FullName))
					return (nullableDictionary[type.FullName] as NullableType).NullValue;
				return null;
			}
		}
	}

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !FXCLIENT
    [XmlType(TypeName = "type")]
#endif
    public sealed class NullableType
	{
		private string typeName;
		private string value;

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "name")]
#endif
        public string TypeName
		{
			get{return typeName;}
			set
			{
				typeName = value;
				Init();
			}
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "value")]
#endif
        public string Value
		{
			get{return value;}
			set
			{
				this.value = value;
				Init();
			}
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlIgnore]
#endif
        public object NullValue { get; private set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		private void Init()
		{
			if( typeName == null || value == null )
				return;

			var type = Type.GetType(typeName);
			// Check if this is a static field of the type
			var fi = type.GetField(value, BindingFlags.Public | BindingFlags.Static);
			if( fi != null )
				NullValue = fi.GetValue(null);
			else
#if (NET_1_1)
				_nullValue = System.Convert.ChangeType(_value, type);
#else
                NullValue = Convert.ChangeType(value, type, null);
#endif
		}
	}

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class CacheCollection : CollectionBase<CachedService>
#else
    public sealed class CacheCollection : CollectionBase
#endif
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public CacheCollection()
		{
		}
#if (NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( CachedService value )  
		{
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( CachedService value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, CachedService value )  
		{
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( CachedService value )  
		{
			List.Remove(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( CachedService value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public CachedService this[ int index ]  
		{
			get  
			{
				return List[index] as CachedService;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [XmlType(TypeName = "cachedService")]
	public sealed class CachedService
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public CachedService()
		{
			Timeout = 30;
			SlidingExpiration = false;
		}

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "timeout")]
		public int Timeout { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "boolean", AttributeName = "slidingExpiration")]
		public bool SlidingExpiration { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "string", AttributeName = "type")]
		public string Type { get; set; }
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class ImportNamespaceCollection : CollectionBase<ImportNamespace>
#else
    public sealed class ImportNamespaceCollection : CollectionBase
#endif
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ImportNamespaceCollection()
		{
		}
#if (NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( ImportNamespace value )  
		{
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( ImportNamespace value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, ImportNamespace value )  
		{
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( ImportNamespace value )  
		{
			List.Remove(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( ImportNamespace value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public ImportNamespace this[ int index ]  
		{
			get  
			{
				return List[index] as ImportNamespace;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class ImportNamespace
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public ImportNamespace()
		{
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "string", AttributeName = "namespace")]
		public string Namespace { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "string", AttributeName = "assembly")]
		public string Assembly { get; set; }
	}
#endif




#if !FXCLIENT

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class MimeTypeEntryCollection : CollectionBase<MimeTypeEntry>
#else
    public sealed class MimeTypeEntryCollection : CollectionBase
#endif
    {
#if !(NET_1_1)
        Dictionary<string, MimeTypeEntry> excludedTypes = new Dictionary<string, MimeTypeEntry>(StringComparer.OrdinalIgnoreCase);
#else
		Hashtable _excludedTypes = new Hashtable(null, CaseInsensitiveComparer.Default);
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public MimeTypeEntryCollection()
		{
		}

#if !(NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(MimeTypeEntry value)
        {
            excludedTypes.Add(value.Type, value);
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, MimeTypeEntry value)
        {
            excludedTypes.Add(value.Type, value);
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public override bool Remove( MimeTypeEntry value )  
		{
			excludedTypes.Remove(value.Type);
			return base.Remove(value);
		}
#else
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( MimeTypeEntry value )  
		{
			_excludedTypes.Add(value.Type, value);
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( MimeTypeEntry value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, MimeTypeEntry value )  
		{
			_excludedTypes.Add(value.Type, value);
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( MimeTypeEntry value )  
		{
			_excludedTypes.Remove(value.Type);
			List.Remove(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( MimeTypeEntry value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public MimeTypeEntry this[ int index ]  
		{
			get  
			{
				return List[index] as MimeTypeEntry;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		public bool Contains( string type )  
		{
			return excludedTypes.ContainsKey(type);
		}
	}
#endif

#if !FXCLIENT

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
#if !(NET_1_1)
    public sealed class PathEntryCollection : CollectionBase<PathEntry>
#else
    public sealed class PathEntryCollection : CollectionBase
#endif
    {
#if !(NET_1_1)
        Dictionary<string, PathEntry> excludedPaths = new Dictionary<string, PathEntry>(StringComparer.OrdinalIgnoreCase);
#else
        Hashtable _excludedPaths = new Hashtable(null, CaseInsensitiveComparer.Default);
#endif

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public PathEntryCollection()
		{
		}

#if !(NET_1_1)
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override void Add(PathEntry value)
        {
            excludedPaths.Add(value.Path, value);
            base.Add(value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(int index, PathEntry value)
        {
            excludedPaths.Add(value.Path, value);
            base.Insert(index, value);
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        public override bool Remove(PathEntry value)
        {
            excludedPaths.Remove(value.Path);
            return base.Remove(value);
        }
#else
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int Add( PathEntry value )  
		{
			_excludedPaths.Add(value.Path, value);
			return List.Add(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public int IndexOf( PathEntry value )  
		{
			return List.IndexOf(value) ;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
		public void Insert( int index, PathEntry value )  
		{
			_excludedPaths.Add(value.Path, value);
			List.Insert(index, value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
		public void Remove( PathEntry value )  
		{
			_excludedPaths.Remove(value.Path);
			List.Remove(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public bool Contains( PathEntry value )  
		{
			return List.Contains(value);
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
		public PathEntry this[ int index ]  
		{
			get  
			{
				return List[index] as PathEntry;
			}
			set  
			{
				List[index] = value;
			}
		}
#endif
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
		public bool Contains( string path )  
		{
			return excludedPaths.ContainsKey(path);
		}
	}
#endif

#if !FXCLIENT

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class MimeTypeEntry
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class PathEntry
	{
        /// <summary>
        /// Gets or sets the to exclude from compression.
        /// The path is a relative url.
        /// </summary>
        [XmlAttribute(AttributeName = "path")]
		public string Path { get; set; }
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class RtmpServerSettings
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public RtmpServerSettings()
		{
            RtmpConnectionSettings = new RtmpConnectionSettings();
            RtmptConnectionSettings = new RtmptConnectionSettings();
            RtmpTransportSettings = new RtmpTransportSettings();
		}

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(ElementName = "rtmpConnection")]
        public RtmpConnectionSettings RtmpConnectionSettings { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(ElementName = "rtmptConnection")]
        public RtmptConnectionSettings RtmptConnectionSettings { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlElement(ElementName = "rtmpTransport")]
        public RtmpTransportSettings RtmpTransportSettings { get; set; }
	}
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class RtmpConnectionSettings
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public RtmpConnectionSettings()
        {
            PingInterval = 5000;
            MaxInactivity = 60000;
            MaxHandshakeTimeout = 5000;
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "pingInterval")]
        public int PingInterval { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "maxInactivity")]
        public int MaxInactivity { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "maxHandshakeTimeout")]
        public int MaxHandshakeTimeout { get; set; }
    }
#endif

#if !FXCLIENT
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class RtmptConnectionSettings
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public RtmptConnectionSettings()
        {
            PingInterval = 5000;
            MaxInactivity = 60000;
            MaxHandshakeTimeout = 5000;
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "pingInterval")]
        public int PingInterval { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "maxInactivity")]
        public int MaxInactivity { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [XmlAttribute(DataType = "int", AttributeName = "maxHandshakeTimeout")]
        public int MaxHandshakeTimeout { get; set; }
    }
#endif

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class RtmpTransportSettings
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public RtmpTransportSettings()
        {
            ReceiveBufferSize = 4096;
            SendBufferSize = 4096;
            TcpNoDelay = true;
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "int", AttributeName = "receiveBufferSize")]
#endif
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "int", AttributeName = "sendBufferSize")]
#endif
        public int SendBufferSize { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "boolean", AttributeName = "tcpNoDelay")]
#endif
        public bool TcpNoDelay { get; set; }
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class OptimizerSettings
	{
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
		public OptimizerSettings()
		{
            Provider = "codedom";
			Debug = true;
            //Generate type checking by default
            TypeCheck = true;
		}
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "provider")]
#endif
        public string Provider { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "boolean", AttributeName = "debug")]
#endif
        public bool Debug { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "boolean", AttributeName = "typeCheck")]
#endif
        public bool TypeCheck { get; set; }
	}

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class CustomErrors
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public CustomErrors()
        {
            Mode = "On";
            StackTrace = false;
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "mode")]
#endif
        public string Mode { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "boolean", AttributeName = "stackTrace")]
#endif
        public bool StackTrace { get; set; }
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class Debug
    {
        /// <summary>
        /// Debug mode off.
        /// </summary>
        public const string Off = "Off";

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public Debug()
        {
            Mode = Off;
        }
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "mode")]
#endif
        public string Mode { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
#if !FXCLIENT
        [XmlAttribute(DataType = "string", AttributeName = "dumpPath")]
#endif
        public string DumpPath { get; set; }
    }
}
