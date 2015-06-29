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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using FluorineFx.AMF3;
using FluorineFx.Configuration;
using FluorineFx.Exceptions;
using FluorineFx.IO.Readers;
using FluorineFx.Util;
#if !(NET_1_1)
using System.Collections.Generic;
#endif
#if SILVERLIGHT
using System.Xml.Linq;
#else
using log4net;
#endif

namespace FluorineFx.IO
{
	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
	/// </summary>
	public class AMFReader : BinaryReader
	{
#if !SILVERLIGHT
        private static readonly ILog log = LogManager.GetLogger(typeof(AMFReader));
#endif

		bool _useLegacyCollection = true;
        bool _faultTolerancy;
        Exception _lastError;

#if !(NET_1_1)
        List<Object> _amf0ObjectReferences;
        List<Object> _objectReferences;
        List<Object> _stringReferences;
        List<ClassDefinition> _classDefinitions;
#else
        ArrayList _amf0ObjectReferences;
        ArrayList _objectReferences;
		ArrayList _stringReferences;
		ArrayList _classDefinitions;
#endif

        private static IAMFReader[][] AmfTypeTable;

		static AMFReader()
		{
			var amf0Readers = new[]
			{
				new AMF0NumberReader(), /*0*/
				new AMF0BooleanReader(), /*1*/
				new AMF0StringReader(), /*2*/
				new AMF0ASObjectReader(), /*3*/
				new MovieclipMarker(), /*4*/
				new AMF0NullReader(), /*5*/
				new AMF0NullReader(), /*6*/
				new AMF0ReferenceReader(), /*7*/
				new AMF0AssociativeArrayReader(), /*8*/
				new AMFUnknownTagReader(), 
				new AMF0ArrayReader(), /*10*/
				new AMF0DateTimeReader(), /*11*/
				new AMF0LongStringReader(), /*12*/
				new UnsupportedMarker(),
				new AMFUnknownTagReader(),
				new AMF0XmlReader(), /*15*/
#if !FXCLIENT
				(FluorineConfiguration.Instance.OptimizerSettings != null && FluorineConfiguration.Instance.FullTrust) ? new  AMF0OptimizedObjectReader() : (IAMFReader)(new AMF0ObjectReader()), /*16*/
#else
                new AMF0ObjectReader(), /*16*/
#endif
				new AMF0AMF3TagReader() /*17*/
			};

			var amf3Readers = new[]
			{
				new AMF3NullReader(), /*0*/
				new AMF3NullReader(), /*1*/
				new AMF3BooleanFalseReader(), /*2*/
				new AMF3BooleanTrueReader(), /*3*/
				new AMF3IntegerReader(), /*4*/
				new AMF3NumberReader(), /*5*/
				new AMF3StringReader(), /*6*/
				new AMF3XmlReader(), /*7*/
				new AMF3DateTimeReader(), /*8*/
				new AMF3ArrayReader(),  /*9*/
#if !FXCLIENT
				(FluorineConfiguration.Instance.OptimizerSettings != null && FluorineConfiguration.Instance.FullTrust) ? new AMF3OptimizedObjectReader() : (IAMFReader)(new AMF3ObjectReader()), /*10*/
#else
                new AMF3ObjectReader(), /*10*/
#endif
				new AMF3XmlReader(), /*11*/
				new AMF3ByteArrayReader(), /*12*/
				new AMF3IntVectorReader(), /*13*/
				new AMF3UIntVectorReader(), /*14*/
				new AMF3DoubleVectorReader(), /*15*/
				new AMF3ObjectVectorReader(), /*16*/
				new AMFUnknownTagReader()
			};

            AmfTypeTable = new IAMFReader[4][] { amf0Readers, null, null, amf3Readers };
		}

		/// <summary>
		/// Initializes a new instance of the AMFReader class based on the supplied stream and using UTF8Encoding.
		/// </summary>
		/// <param name="stream"></param>
		public AMFReader(Stream stream) : base(stream)
		{
			Reset();
		}
        /// <summary>
        /// Resets object references.
        /// </summary>
		public void Reset()
		{
#if !(NET_1_1)
            _amf0ObjectReferences = new List<Object>(5);
            _objectReferences = new List<Object>(15);
            _stringReferences = new List<Object>(15);
            _classDefinitions = new List<ClassDefinition>(2);
#else
			_amf0ObjectReferences = new ArrayList(5);
            _objectReferences = new ArrayList(15);
			_stringReferences = new ArrayList(15);
			_classDefinitions = new ArrayList(2);
#endif
            _lastError = null;
		}
        /// <summary>
        /// Gets or sets whether legacy collection serialization is used for AMF3.
        /// </summary>
        public bool UseLegacyCollection
		{
			get{ return _useLegacyCollection; }
			set{ _useLegacyCollection = value; }
		}
        /// <summary>
        /// Indicates whether reflection errors should raise an exception or set the LastError property.
        /// </summary>
        public bool FaultTolerancy
        {
            get { return _faultTolerancy; }
            set { _faultTolerancy = value; }
        }
        /// <summary>
        /// Returns the last exception that ocurred while deserializing an object.
        /// </summary>
        /// <returns></returns>
        public Exception LastError
        {
            get { return _lastError; }
        }
        /// <summary>
        /// Deserializes object graphs from Action Message Format (AMF).
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
		public object ReadData()
		{
			var typeCode = ReadByte();
			return ReadData(typeCode);
		}
		/// <summary>
        /// Deserializes an object using the specified type marker.
		/// </summary>
        /// <param name="typeMarker">Type marker.</param>
        /// <returns>The Object deserialized from the AMF stream.</returns>
		public object ReadData(byte typeMarker)
		{
            return AmfTypeTable[0][typeMarker].ReadData(this);
		}
        /// <summary>
        /// Reads a reference type.
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
		public object ReadReference()
		{
			int reference = ReadUInt16();
			//return _amf0ObjectReferences[reference-1];
            return _amf0ObjectReferences[reference];
		}
		/// <summary>
        /// Reads a 2-byte unsigned integer from the current AMF stream using network byte order encoding and advances the position of the stream by two bytes.
		/// </summary>
        /// <returns>The 2-byte unsigned integer.</returns>
        
        public override ushort ReadUInt16()
		{
			//Read the next 2 bytes, shift and add.
			var bytes = ReadBytes(2);
			return (ushort)(((bytes[0] & 0xff) << 8) | (bytes[1] & 0xff));
		}
        /// <summary>
        /// Reads a 2-byte signed integer from the current AMF stream using network byte order encoding and advances the position of the stream by two bytes.
        /// </summary>
        /// <returns>The 2-byte signed integer.</returns>
        public override short ReadInt16()
		{
			//Read the next 2 bytes, shift and add.
			var bytes = ReadBytes(2);
			return (short)((bytes[0] << 8) | bytes[1]);
		}
        /// <summary>
        /// Reads an UTF-8 encoded String from the current AMF stream.
        /// </summary>
        /// <returns>The String value.</returns>
		public override string ReadString()
		{
			//Get the length of the string (first 2 bytes).
			int length = ReadUInt16();
			return ReadUTF(length);
		}
        /// <summary>
        /// Reads a Boolean value from the current AMF stream using network byte order encoding and advances the position of the stream by one byte.
        /// </summary>
        /// <returns>The Boolean value.</returns>
        public override bool ReadBoolean()
		{
			return base.ReadBoolean();
		}
        /// <summary>
        /// Reads a 4-byte signed integer from the current AMF stream using network byte order encoding and advances the position of the stream by four bytes.
        /// </summary>
        /// <returns>The 4-byte signed integer.</returns>
		public override int ReadInt32()
		{
			// Read the next 4 bytes, shift and add
			var bytes = ReadBytes(4);
			var value = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            return value;
		}
        /// <summary>
        /// Reads a 4-byte signed integer from the current AMF stream.
        /// </summary>
        /// <returns>The 4-byte signed integer.</returns>
        public int ReadReverseInt()
        {
            var bytes = ReadBytes(4);
            var val = 0;
            val += bytes[3] << 24;
            val += bytes[2] << 16;
            val += bytes[1] << 8;
            val += bytes[0];
            return val;
        }
        /// <summary>
        /// Reads a 3-byte signed integer from the current AMF stream using network byte order encoding and advances the position of the stream by three bytes.
        /// </summary>
        /// <returns>The 3-byte signed integer.</returns>
        public int ReadUInt24()
        {
            var bytes = ReadBytes(3);
            var value = bytes[0] << 16 | bytes[1] << 8 | bytes[2];
            return value;
        }
        /// <summary>
        /// Reads an 8-byte IEEE-754 double precision floating point number from the current AMF stream using network byte order encoding and advances the position of the stream by eight bytes.
        /// </summary>
        /// <returns>The 8-byte double precision floating point number.</returns>
		public override double ReadDouble()
		{			
			var bytes = ReadBytes(8);
			var reverse = new byte[8];
			//Grab the bytes in reverse order 
			for(int i = 7, j = 0 ; i >= 0 ; i--, j++)
			{
				reverse[j] = bytes[i];
			}
			var value = BitConverter.ToDouble(reverse, 0);
			return value;
		}
        /// <summary>
        /// Reads a single-precision floating point number from the current AMF stream using network byte order encoding and advances the position of the stream by eight bytes.
        /// </summary>
        /// <returns>The single-precision floating point number.</returns>
		public float ReadFloat()
		{			
			var bytes = ReadBytes(4);
			var invertedBytes = new byte[4];
			//Grab the bytes in reverse order from the backwards index
			for(int i = 3, j = 0 ; i >= 0 ; i--, j++)
			{
				invertedBytes[j] = bytes[i];
			}
			var value = BitConverter.ToSingle(invertedBytes, 0);
			return value;
		}
        /// <summary>
        /// Add object reference.
        /// </summary>
        /// <param name="instance">The object instance.</param>
		public void AddReference(object instance)
		{
			_amf0ObjectReferences.Add(instance);
		}
        /// <summary>
        /// Reads an AMF0 object.
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
		public object ReadObject()
		{
			var typeIdentifier = ReadString();

#if !SILVERLIGHT
            if(log.IsDebugEnabled )
				log.Debug(String.Format(Resources.TypeIdentifier_Loaded, typeIdentifier));
#endif

			var type = ObjectFactory.Locate(typeIdentifier);
			if( type != null )
			{
				var instance = ObjectFactory.CreateInstance(type);
                AddReference(instance);

				var key = ReadString();
                for (var typeCode = ReadByte(); typeCode != AMF0TypeCode.EndOfObject; typeCode = ReadByte())
				{
					var value = ReadData(typeCode);
                    SetMember(instance, key, value);
					key = ReadString();
				}
                return instance;
			}
#if !SILVERLIGHT
            if( log.IsWarnEnabled )
                log.Warn(String.Format(Resources.TypeLoad_ASO, typeIdentifier));
#endif

            ASObject asObject;
            //Reference added in ReadASObject
            asObject = ReadASObject();
            asObject.Alias = typeIdentifier;
            return asObject;
		}
        /// <summary>
        /// Reads an anonymous ActionScript object.
        /// </summary>
        /// <returns>The anonymous ActionScript object deserialized from the AMF stream.</returns>
		public ASObject ReadASObject()
		{
			var asObject = new ASObject();
			AddReference(asObject);
			var key = ReadString();
			for(var typeCode = ReadByte(); typeCode != AMF0TypeCode.EndOfObject; typeCode = ReadByte())
			{
				//asObject.Add(key, ReadData(typeCode));
                asObject[key] = ReadData(typeCode);
				key = ReadString();
			}
			return asObject;
		}
        /// <summary>
        /// Reads an UTF-8 encoded String.
        /// </summary>
        /// <param name="length">Byte-length header.</param>
        /// <returns>The String value.</returns>
		public string ReadUTF(int length)
		{
			if( length == 0 )
                return string.Empty;
			var utf8 = new UTF8Encoding(false, true);
			var encodedBytes = ReadBytes(length);
#if !(NET_1_1)
            var decodedString = utf8.GetString(encodedBytes, 0, encodedBytes.Length);
#else
            string decodedString = utf8.GetString(encodedBytes);
#endif
            return decodedString;
		}
		/// <summary>
        /// Reads an UTF-8 encoded AMF0 Long String type.
		/// </summary>
        /// <returns>The String value.</returns>
		public string ReadLongString()
		{
			var length = ReadInt32();
			return ReadUTF(length);
		}

#if !(NET_1_1)
        /// <summary>
        /// Reads an ECMA or associative Array.
        /// </summary>
        /// <returns>The associative Array.</returns>
        internal Dictionary<string, Object> ReadAssociativeArray()
        {
            // Get the length property set by flash.
            var length = ReadInt32();
            var result = new Dictionary<string, Object>(length);
            AddReference(result);
            var key = ReadString();
            for (var typeCode = ReadByte(); typeCode != AMF0TypeCode.EndOfObject; typeCode = ReadByte())
            {
                var value = ReadData(typeCode);
                result.Add(key, value);
                key = ReadString();
            }
            return result;
        }
#else
        internal Hashtable ReadAssociativeArray()
		{
			// Get the length property set by flash.
			int length = this.ReadInt32();
			Hashtable result = new Hashtable(length);
			AddReference(result);
			string key = ReadString();
			for(byte typeCode = ReadByte(); typeCode != AMF0TypeCode.EndOfObject; typeCode = ReadByte())
			{
				object value = ReadData(typeCode);
				result.Add(key, value);
				key = ReadString();
			}
			return result;
		}
#endif
        /// <summary>
        /// Reads an AMF0 strict Array.
        /// </summary>
        /// <returns>The Array.</returns>
		internal IList ReadArray()
		{
			//Get the length of the array.
			var length = ReadInt32();
			var array = new object[length];
			//ArrayList array = new ArrayList(length);
			AddReference(array);
			for(var i = 0; i < length; i++)
			{
				array[i] = ReadData();
				//array.Add( ReadData() );
			}
			return array;
		} 
        /// <summary>
        /// Reads an ActionScript Date.
        /// </summary>
        /// <returns>The DateTime.</returns>
		public DateTime ReadDateTime()
		{
			var milliseconds = ReadDouble();
			var start = new DateTime(1970, 1, 1);

			var date = start.AddMilliseconds(milliseconds);
#if !(NET_1_1)
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
#endif
            int tmp = ReadUInt16();
			//Note for the latter than values greater than 720 (12 hours) are 
			//represented as 2^16 - the value.
			//Thus GMT+1 is 60 while GMT-5 is 65236
			if(tmp > 720)
				tmp = (65536 - tmp);
			var tz = tmp / 60;
			switch(FluorineConfiguration.Instance.TimezoneCompensation)
			{
				case TimezoneCompensation.None:
					break;
				case TimezoneCompensation.Auto:
					date = date.AddHours(tz);
#if !(NET_1_1)
                    date = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
#endif								
					//if(TimeZone.CurrentTimeZone.IsDaylightSavingTime(date))
					//	date = date.AddMilliseconds(-3600000);
                    break;
                case TimezoneCompensation.Server:
                    //Convert to local time
                    date = date.ToLocalTime();
                    break;
			}

			return date;
		}
 
#if !SILVERLIGHT
        /// <summary>
        /// Reads an XML Document Type.
        /// The XML document type is always encoded as a long UTF-8 string.
        /// </summary>
        /// <returns>The XmlDocument.</returns>
		public XmlDocument ReadXmlDocument()
		{
			var text = ReadLongString();
			var document = new XmlDocument();
            if( text != null && text != string.Empty)
			    document.LoadXml(text);
			return document;
		}
#else
        public XDocument ReadXmlDocument()
        {
            string text = this.ReadLongString();
            XDocument document;
            if (text != null && text != string.Empty)
                document = XDocument.Parse(text);
            else
                document = new XDocument();
            return document;
        }
#endif

	    /// <summary>
        /// Deserializes object graphs from Action Message Format (AMF3).
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
        public object ReadAMF3Data()
		{
			var typeCode = ReadByte();
			return ReadAMF3Data(typeCode);
		}
        /// <summary>
        /// Deserializes an object using the specified type marker.
        /// </summary>
        /// <param name="typeMarker">Type marker.</param>
        /// <returns>The Object deserialized from the AMF stream.</returns>
        public object ReadAMF3Data(byte typeMarker)
		{
            return AmfTypeTable[3][typeMarker].ReadData(this);
		}
        /// <summary>
        /// Add object reference.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        public void AddAMF3ObjectReference(object instance)
        {
            _objectReferences.Add(instance);
        }
        /// <summary>
        /// Reads a reference type.
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
        public object ReadAMF3ObjectReference(int index)
        {
            return _objectReferences[index];
        }
		/// <summary>
		/// Handle decoding of the variable-length representation which gives seven bits of value per serialized byte by using the high-order bit 
		/// of each byte as a continuation flag.
		/// </summary>
		/// <returns></returns>
		public int ReadAMF3IntegerData()
		{
			int acc = ReadByte();
			int tmp;
			if(acc < 128)
				return acc;
		    acc = (acc & 0x7f) << 7;
		    tmp = ReadByte();
		    if(tmp < 128)
		        acc = acc | tmp;
		    else
		    {
		        acc = (acc | tmp & 0x7f) << 7;
		        tmp = ReadByte();
		        if(tmp < 128)
		            acc = acc | tmp;
		        else
		        {
		            acc = (acc | tmp & 0x7f) << 8;
		            tmp = ReadByte();
		            acc = acc | tmp;
		        }
		    }
		    //To sign extend a value from some number of bits to a greater number of bits just copy the sign bit into all the additional bits in the new format.
			//convert/sign extend the 29bit two's complement number to 32 bit
			var mask = 1 << 28; // mask
			var r = -(acc & mask) | acc;
			return r;

			//The following variation is not portable, but on architectures that employ an 
			//arithmetic right-shift, maintaining the sign, it should be fast. 
			//s = 32 - 29;
			//r = (x << s) >> s;
		}
        /// <summary>
        /// Reads a 4-byte signed integer from the current AMF stream.
        /// </summary>
        /// <returns>The 4-byte signed integer.</returns>
		public int ReadAMF3Int()
		{
			var intData = ReadAMF3IntegerData();
			return intData;
		}
        /// <summary>
        /// Reads an ActionScript Date.
        /// </summary>
        /// <returns>The DateTime.</returns>
        public DateTime ReadAMF3Date()
		{
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1)  != 0 );
			handle = handle >> 1;
			if( inline )
			{
				var milliseconds = ReadDouble();
				var start = new DateTime(1970, 1, 1, 0, 0, 0);

				var date = start.AddMilliseconds(milliseconds);
#if !(NET_1_1)
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
#endif
				switch(FluorineConfiguration.Instance.TimezoneCompensation)
				{
					case TimezoneCompensation.None:
                        //No conversion by default
						break;
					case TimezoneCompensation.Auto:
						//Not applicable for AMF3
						break;
                    case TimezoneCompensation.Server:
                        //Convert to local time
                        date = date.ToLocalTime();
                        break;
                }
                AddAMF3ObjectReference(date);
				return date;
			}
            return (DateTime)ReadAMF3ObjectReference(handle);
		}

        internal void AddStringReference(string str)
        {
            _stringReferences.Add(str);
        }

        internal string ReadStringReference(int index)
        {
            return _stringReferences[index] as string;
        }
        /// <summary>
        /// Reads an UTF-8 encoded String from the current AMF stream.
        /// </summary>
        /// <returns>The String value.</returns>
        public string ReadAMF3String()
		{
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1) != 0 );
			handle = handle >> 1;
			if( inline )
			{
				var length = handle;
                if (length == 0)
                    return string.Empty;
				var str = ReadUTF(length);
                AddStringReference(str);
				return str;
			}
            return ReadStringReference(handle);
		}

#if !SILVERLIGHT
        /// <summary>
        /// Reads an XML Document Type.
        /// The XML document type is always encoded as a long UTF-8 string.
        /// </summary>
        /// <returns>The XmlDocument.</returns>
        public XmlDocument ReadAMF3XmlDocument()
		{
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1) != 0 );
			handle = handle >> 1;
			var xml = string.Empty;
			if( inline )
			{
                if (handle > 0)//length
				    xml = ReadUTF(handle);
                AddAMF3ObjectReference(xml);
			}
			else
			{
				xml = ReadAMF3ObjectReference(handle) as string;
			}
			var xmlDocument = new XmlDocument();
            if (xml != null && xml != string.Empty)
                xmlDocument.LoadXml(xml);
			return xmlDocument;
		}
#else
        public XDocument ReadAMF3XmlDocument()
        {
            int handle = ReadAMF3IntegerData();
            bool inline = ((handle & 1) != 0);
            handle = handle >> 1;
            string xml = string.Empty;
            if (inline)
            {
                if (handle > 0)//length
                    xml = this.ReadUTF(handle);
                AddAMF3ObjectReference(xml);
            }
            else
            {
                xml = ReadAMF3ObjectReference(handle) as string;
            }
            XDocument document;
            if (xml != null && xml != string.Empty)
                document = XDocument.Parse(xml);
            else
                document = new XDocument();
            return document;
        }
#endif

        /// <summary>
        /// Reads a ByteArray.
        /// </summary>
        /// <returns>The ByteArray instance.</returns>
        /// <remarks>
        /// 	<para>ActionScript 3.0 introduces a new type to hold an Array of bytes, namely
        ///     ByteArray. AMF 3 serializes this type using a variable length encoding 29-bit
        ///     integer for the byte-length prefix followed by the raw bytes of the ByteArray.
        ///     ByteArray instances can be sent as a reference to a previously occurring ByteArray
        ///     instance by using an index to the implicit object reference table.</para>
        /// 	<para>Note that this encoding imposes some theoretical limits on the use of
        ///     ByteArray. The maximum byte-length of each ByteArray instance is limited to 2^28 -
        ///     1 bytes (approx 256 MB).</para>
        /// </remarks>
        
		public ByteArray ReadAMF3ByteArray()
		{
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1) != 0 );
			handle = handle >> 1;
			if( inline )
			{
				var length = handle;
				var buffer = ReadBytes(length);
				var ba = new ByteArray(buffer);
				AddAMF3ObjectReference(ba);
				return ba;
			}
            return ReadAMF3ObjectReference(handle) as ByteArray;
		}
        /// <summary>
        /// Reads an AMF3 Array (string or associative).
        /// </summary>
        /// <returns>The Array instance.</returns>
		public object ReadAMF3Array()
		{
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1)  != 0 ); handle = handle >> 1;
			if( inline )
			{
#if !(NET_1_1)
                Dictionary<string, object> hashtable = null;
#else
                Hashtable hashtable = null;
#endif
				var key = ReadAMF3String();
                while (key != null && key != string.Empty)
				{
					if( hashtable == null )
					{
#if !(NET_1_1)
                        hashtable = new Dictionary<string, object>();
#else
                        hashtable = new Hashtable();
#endif
						AddAMF3ObjectReference(hashtable);
					}
					var value = ReadAMF3Data();
					hashtable.Add(key, value);
					key = ReadAMF3String();
				}
				//Not an associative array
				if( hashtable == null )
				{
                    var array = new object[handle];
                    AddAMF3ObjectReference(array);
					for(var i = 0; i < handle; i++)
					{
						//Grab the type for each element.
						var typeCode = ReadByte();
						var value = ReadAMF3Data(typeCode);
						array[i] = value;
					}
					return array;
				}
			    for(var i = 0; i < handle; i++)
			    {
			        var value = ReadAMF3Data();
			        hashtable.Add( i.ToString(), value);
			    }
			    return hashtable;
			}
            return ReadAMF3ObjectReference(handle);
		}

        
        public IList<int> ReadAMF3IntVector()
        {
			var handle = ReadAMF3IntegerData();
			var inline = ((handle & 1) != 0 ); handle = handle >> 1;
            if (inline)
            {
                var list = new List<int>(handle);
                AddAMF3ObjectReference(list);
                var @fixed = ReadAMF3IntegerData();
                for (var i = 0; i < handle; i++)
                {
                    list.Add(ReadInt32());
                }
                return @fixed == 1 ? list.AsReadOnly() as IList<int> : list;
            }
            return ReadAMF3ObjectReference(handle) as List<int>;
        }

        
        public IList<uint> ReadAMF3UIntVector()
        {
            var handle = ReadAMF3IntegerData();
            var inline = ((handle & 1) != 0); handle = handle >> 1;
            if (inline)
            {
                var list = new List<uint>(handle);
                AddAMF3ObjectReference(list);
                var @fixed = ReadAMF3IntegerData();
                for (var i = 0; i < handle; i++)
                {
                    list.Add((uint)ReadInt32());
                }
                return @fixed == 1 ? list.AsReadOnly() as IList<uint> : list;
            }
            return ReadAMF3ObjectReference(handle) as List<uint>;
        }

        
        public IList<double> ReadAMF3DoubleVector()
        {
            var handle = ReadAMF3IntegerData();
            var inline = ((handle & 1) != 0); handle = handle >> 1;
            if (inline)
            {
                var list = new List<double>(handle);
                AddAMF3ObjectReference(list);
                var @fixed = ReadAMF3IntegerData();
                for (var i = 0; i < handle; i++)
                {
                    list.Add(ReadDouble());
                }
                return @fixed == 1 ? list.AsReadOnly() as IList<double> : list;
            }
            return ReadAMF3ObjectReference(handle) as List<double>;
        }

        
        public IList ReadAMF3ObjectVector()
        {
            var handle = ReadAMF3IntegerData();
            var inline = ((handle & 1) != 0); handle = handle >> 1;
            if (inline)
            {
                //List<object> list = new List<object>(handle);
                var @fixed = ReadAMF3IntegerData();
                var typeIdentifier = ReadAMF3String();
                IList list;
                if (!string.Empty.Equals(typeIdentifier))
                    list = ReflectionUtils.CreateGeneric(typeof(List<>), ObjectFactory.Locate(typeIdentifier)) as IList;
                else
                    list = new List<object>();
                AddAMF3ObjectReference(list);
                for (var i = 0; i < handle; i++)
                {
                    var typeCode = ReadByte();
                    var obj = ReadAMF3Data(typeCode);
                    list.Add(obj);
                }
                if (@fixed == 1)
                    return list.GetType().GetMethod("AsReadOnly").Invoke(list, null) as IList;
                
                return list;
            }
            return ReadAMF3ObjectReference(handle) as IList;
        }

        internal void AddClassReference(ClassDefinition classDefinition)
        {
            _classDefinitions.Add(classDefinition);
        }

        internal ClassDefinition ReadClassReference(int index)
        {
            return _classDefinitions[index];
        }

		internal ClassDefinition ReadClassDefinition(int handle)
		{
			ClassDefinition classDefinition = null;
			//an inline object
			var inlineClassDef = ((handle & 1) != 0 );handle = handle >> 1;
			if( inlineClassDef )
			{
				//inline class-def
				var typeIdentifier = ReadAMF3String();
				//flags that identify the way the object is serialized/deserialized
				var externalizable = ((handle & 1) != 0 );handle = handle >> 1;
				var dynamic = ((handle & 1) != 0 );handle = handle >> 1;

                var members = new ClassMember[handle];
				for (var i = 0; i < handle; i++)
				{
                    var name = ReadAMF3String();
                    var classMember = new ClassMember(name, BindingFlags.Default, MemberTypes.Custom, null);
                    members[i] = classMember;
				}
				classDefinition = new ClassDefinition(typeIdentifier, members, externalizable, dynamic);
				AddClassReference(classDefinition);
			}
			else
			{
				//A reference to a previously passed class-def
				classDefinition = ReadClassReference(handle);
			}
#if !SILVERLIGHT
            if (log.IsDebugEnabled)
			{
				if (classDefinition.IsTypedObject)
					log.Debug(String.Format(Resources.ClassDefinition_Loaded, classDefinition.ClassName));
				else
					log.Debug(String.Format(Resources.ClassDefinition_LoadedUntyped));
			}
#endif
			return classDefinition;
		}

        internal object ReadAMF3Object(ClassDefinition classDefinition)
        {
            object instance = null;
            if (!string.IsNullOrEmpty(classDefinition.ClassName))
                instance = ObjectFactory.CreateInstance(classDefinition.ClassName);
            else
                instance = new ASObject();
            if (instance == null)
            {
#if !SILVERLIGHT
                if (log.IsWarnEnabled)
                    log.Warn(String.Format(Resources.TypeLoad_ASO, classDefinition.ClassName));
#endif
                instance = new ASObject(classDefinition.ClassName);
            }
            AddAMF3ObjectReference(instance);
            if (classDefinition.IsExternalizable)
            {
                if (instance is IExternalizable)
                {
                    var externalizable = instance as IExternalizable;
                    var dataInput = new DataInput(this);
                    externalizable.ReadExternal(dataInput);
                }
                else
                {
                    var msg = String.Format(Resources.Externalizable_CastFail, instance.GetType().FullName);
                    throw new FluorineException(msg);
                }
            }
            else
            {
                for (var i = 0; i < classDefinition.MemberCount; i++)
                {
                    var key = classDefinition.Members[i].Name;
                    var value = ReadAMF3Data();
                    SetMember(instance, key, value);
                }
                if (classDefinition.IsDynamic)
                {
                    var key = ReadAMF3String();
                    while (key != null && key != string.Empty)
                    {
                        var value = ReadAMF3Data();
                        SetMember(instance, key, value);
                        key = ReadAMF3String();
                    }
                }
            }
            return instance;
        }
        /// <summary>
        /// Reads an AMF3 object.
        /// </summary>
        /// <returns>The Object deserialized from the AMF stream.</returns>
        public object ReadAMF3Object()
        {
            var handle = ReadAMF3IntegerData();
            var inline = ((handle & 1) != 0); handle = handle >> 1;
            if (!inline)
            {
                //An object reference
                return ReadAMF3ObjectReference(handle);
            }
            var classDefinition = ReadClassDefinition(handle);
            var obj = ReadAMF3Object(classDefinition);
            return obj;
        }

	    internal void SetMember(object instance, string memberName, object value)
        {
            if (instance is ASObject)
            {
                ((ASObject)instance)[memberName] = value;
                return;
            }
            var type = instance.GetType();
            //PropertyInfo propertyInfo = type.GetProperty(memberName);
            PropertyInfo propertyInfo = null;
            try
            {
                propertyInfo = type.GetProperty(memberName);
            }
            catch (AmbiguousMatchException)
            {
                //To resolve the ambiguity, include BindingFlags.DeclaredOnly to restrict the search to members that are not inherited.
                propertyInfo = type.GetProperty(memberName, BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            }
            if (propertyInfo != null)
            {
                try
                {
                    value = TypeHelper.ChangeType(value, propertyInfo.PropertyType);
                    if (propertyInfo.CanWrite && propertyInfo.GetSetMethod() != null)
                    {
                        if (propertyInfo.GetIndexParameters() == null || propertyInfo.GetIndexParameters().Length == 0)
                            propertyInfo.SetValue(instance, value, null);
                        else
                        {
                            var msg = String.Format(Resources.Reflection_PropertyIndexFail, string.Format("{0}.{1}", type.FullName, memberName));
#if !SILVERLIGHT
                            if (log.IsErrorEnabled)
                                log.Error(msg);
#endif
                            if( !_faultTolerancy )
                                throw new FluorineException(msg);
                            _lastError = new FluorineException(msg);
                        }
                    }
                    else
                    {
                        var msg = String.Format(Resources.Reflection_PropertyReadOnly, string.Format("{0}.{1}", type.FullName, memberName));
#if !SILVERLIGHT
                        if (log.IsWarnEnabled)
                            log.Warn(msg);
#endif
                    }
                }
                catch (Exception ex)
                {
                    var msg = String.Format(Resources.Reflection_PropertySetFail, string.Format("{0}.{1}", type.FullName, memberName), ex.Message);
#if !SILVERLIGHT
                    if (log.IsErrorEnabled)
                        log.Error(msg, ex);
#endif
                    if (!_faultTolerancy)
                        throw new FluorineException(msg);
                    _lastError = new FluorineException(msg);
                }
            }
            else
            {
                var fi = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
                try
                {
                    if (fi != null)
                    {
                        value = TypeHelper.ChangeType(value, fi.FieldType);
                        fi.SetValue(instance, value);
                    }
                    else
                    {
                        var msg = String.Format(Resources.Reflection_MemberNotFound, string.Format("{0}.{1}", type.FullName, memberName));
#if !SILVERLIGHT
                        if (log.IsWarnEnabled)
                            log.Warn(msg);
#endif
                    }
                }
                catch (Exception ex)
                {
                    var msg = String.Format(Resources.Reflection_FieldSetFail, string.Format("{0}.{1}", type.FullName, memberName), ex.Message);
#if !SILVERLIGHT
                    if (log.IsErrorEnabled)
                        log.Error(msg, ex);
#endif
                    if (!_faultTolerancy)
                        throw new FluorineException(msg);
                    _lastError = new FluorineException(msg);
                }
            }
        }
 	}
}
