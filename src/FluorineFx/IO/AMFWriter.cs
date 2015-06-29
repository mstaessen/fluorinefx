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
using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;
using FluorineFx.AMF3;
using FluorineFx.Configuration;
using FluorineFx.Exceptions;
using FluorineFx.IO.Writers;
using FluorineFx.Util;
using Debug = System.Diagnostics.Debug;
#if !(NET_1_1)
using System.Collections.Generic;
using FluorineFx.Collections.Generic;
#endif
#if !(NET_1_1) && !(NET_2_0)
using System.Xml.Linq;
#endif
#if SILVERLIGHT
//using System.Xml.Linq;
#else
using System.Data;
using log4net;
#endif

namespace FluorineFx.IO
{
	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
	/// </summary>
	public class AMFWriter : BinaryWriter
	{
#if !SILVERLIGHT
		private static readonly ILog log = LogManager.GetLogger(typeof(AMFWriter));
#endif

		bool _useLegacyCollection = true;
        bool _useLegacyThrowable = true;
#if !(NET_1_1)
        static CopyOnWriteDictionary<string, ClassDefinition> classDefinitions;

        Dictionary<Object, int> _amf0ObjectReferences;
        Dictionary<Object, int> _objectReferences;
        Dictionary<Object, int> _stringReferences;
        Dictionary<ClassDefinition, int> _classDefinitionReferences;
        static Dictionary<Type, IAMFWriter>[] AmfWriterTable;
#else
        static CopyOnWriteDictionary classDefinitions;

        Hashtable	_amf0ObjectReferences;
		Hashtable	_objectReferences;
		Hashtable	_stringReferences;
		Hashtable	_classDefinitionReferences;
        static Hashtable[] AmfWriterTable;
#endif

        static AMFWriter()
		{
#if !(NET_1_1)
            var amf0Writers = new Dictionary<Type, IAMFWriter>();
#else
			Hashtable amf0Writers = new Hashtable();
#endif
			var amf0NumberWriter = new AMF0NumberWriter();
			amf0Writers.Add(typeof(SByte), amf0NumberWriter);
			amf0Writers.Add(typeof(Byte), amf0NumberWriter);
			amf0Writers.Add(typeof(Int16), amf0NumberWriter);
			amf0Writers.Add(typeof(UInt16), amf0NumberWriter);
			amf0Writers.Add(typeof(Int32), amf0NumberWriter);
			amf0Writers.Add(typeof(UInt32), amf0NumberWriter);
			amf0Writers.Add(typeof(Int64), amf0NumberWriter);
			amf0Writers.Add(typeof(UInt64), amf0NumberWriter);
			amf0Writers.Add(typeof(Single), amf0NumberWriter);
			amf0Writers.Add(typeof(Double), amf0NumberWriter);
			amf0Writers.Add(typeof(Decimal), amf0NumberWriter);
			amf0Writers.Add(typeof(DBNull), new AMF0NullWriter());
#if !SILVERLIGHT
			var amf0SqlTypesWriter = new AMF0SqlTypesWriter();
			amf0Writers.Add(typeof(INullable), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlByte), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlInt16), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlInt32), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlInt64), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlSingle), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlDouble), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlDecimal), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlMoney), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlDateTime), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlString), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlGuid), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlBinary), amf0SqlTypesWriter);
			amf0Writers.Add(typeof(SqlBoolean), amf0SqlTypesWriter);

            amf0Writers.Add(typeof(CacheableObject), new AMF0CacheableObjectWriter());
			amf0Writers.Add(typeof(XmlDocument), new AMF0XmlDocumentWriter());
			amf0Writers.Add(typeof(DataTable), new AMF0DataTableWriter());
			amf0Writers.Add(typeof(DataSet), new AMF0DataSetWriter());
            amf0Writers.Add(typeof(RawBinary), new RawBinaryWriter());
            amf0Writers.Add(typeof(NameObjectCollectionBase), new AMF0NameObjectCollectionWriter());
#endif
#if !(NET_1_1) && !(NET_2_0)
            amf0Writers.Add(typeof(XDocument), new AMF0XDocumentWriter());
            amf0Writers.Add(typeof(XElement), new AMF0XElementWriter());
#endif
            amf0Writers.Add(typeof(Guid), new AMF0GuidWriter());
			amf0Writers.Add(typeof(string), new AMF0StringWriter());
			amf0Writers.Add(typeof(bool), new AMF0BooleanWriter());
			amf0Writers.Add(typeof(Enum), new AMF0EnumWriter());
			amf0Writers.Add(typeof(Char), new AMF0CharWriter());
            amf0Writers.Add(typeof(DateTime), new AMF0DateTimeWriter());
			amf0Writers.Add(typeof(Array), new AMF0ArrayWriter());
			amf0Writers.Add(typeof(ASObject), new AMF0ASObjectWriter());

#if !(NET_1_1)
            var amf3Writers = new Dictionary<Type, IAMFWriter>();
#else
			Hashtable amf3Writers = new Hashtable();
#endif
			var amf3IntWriter = new AMF3IntWriter();
			var amf3DoubleWriter = new AMF3DoubleWriter();
			amf3Writers.Add(typeof(SByte), amf3IntWriter);
			amf3Writers.Add(typeof(Byte), amf3IntWriter);
			amf3Writers.Add(typeof(Int16), amf3IntWriter);
			amf3Writers.Add(typeof(UInt16), amf3IntWriter);
			amf3Writers.Add(typeof(Int32), amf3IntWriter);
			amf3Writers.Add(typeof(UInt32), amf3IntWriter);
			amf3Writers.Add(typeof(Int64), amf3DoubleWriter);
			amf3Writers.Add(typeof(UInt64), amf3DoubleWriter);
			amf3Writers.Add(typeof(Single), amf3DoubleWriter);
			amf3Writers.Add(typeof(Double), amf3DoubleWriter);
			amf3Writers.Add(typeof(Decimal), amf3DoubleWriter);
			amf3Writers.Add(typeof(DBNull), new AMF3DBNullWriter());
#if !SILVERLIGHT
            var amf3SqlTypesWriter = new AMF3SqlTypesWriter();
			amf3Writers.Add(typeof(INullable), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlByte), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlInt16), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlInt32), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlInt64), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlSingle), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlDouble), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlDecimal), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlMoney), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlDateTime), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlString), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlGuid), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlBinary), amf3SqlTypesWriter);
			amf3Writers.Add(typeof(SqlBoolean), amf3SqlTypesWriter);

            amf3Writers.Add(typeof(CacheableObject), new AMF3CacheableObjectWriter());
			amf3Writers.Add(typeof(XmlDocument), new AMF3XmlDocumentWriter());
			amf3Writers.Add(typeof(DataTable), new AMF3DataTableWriter());
			amf3Writers.Add(typeof(DataSet), new AMF3DataSetWriter());
            amf3Writers.Add(typeof(RawBinary), new RawBinaryWriter());            
            amf3Writers.Add(typeof(NameObjectCollectionBase), new AMF3NameObjectCollectionWriter());
#endif
#if !(NET_1_1) && !(NET_2_0)
            amf3Writers.Add(typeof(XDocument), new AMF3XDocumentWriter());
            amf3Writers.Add(typeof(XElement), new AMF3XElementWriter());
#endif
            amf3Writers.Add(typeof(Guid), new AMF3GuidWriter());
			amf3Writers.Add(typeof(string), new AMF3StringWriter());
			amf3Writers.Add(typeof(bool), new AMF3BooleanWriter());
			amf3Writers.Add(typeof(Enum), new AMF3EnumWriter());
			amf3Writers.Add(typeof(Char), new AMF3CharWriter());
            amf3Writers.Add(typeof(DateTime), new AMF3DateTimeWriter());
			amf3Writers.Add(typeof(Array), new AMF3ArrayWriter());
			amf3Writers.Add(typeof(ASObject), new AMF3ASObjectWriter());
			amf3Writers.Add(typeof(ByteArray), new AMF3ByteArrayWriter());
			amf3Writers.Add(typeof(byte[]), new AMF3ByteArrayWriter());


            //amf3Writers.Add(typeof(List<int>), new AMF3IntVectorWriter());
            //amf3Writers.Add(typeof(IList<int>), new AMF3IntVectorWriter());

#if !(NET_1_1)
            AmfWriterTable = new Dictionary<Type, IAMFWriter>[4] { amf0Writers, null, null, amf3Writers };
            classDefinitions = new CopyOnWriteDictionary<string,ClassDefinition>();
#else
			AmfWriterTable = new Hashtable[4]{amf0Writers, null, null, amf3Writers};
            classDefinitions = new CopyOnWriteDictionary();
#endif

        }

		/// <summary>
		/// Initializes a new instance of the AMFReader class based on the supplied stream and using UTF8Encoding.
		/// </summary>
		/// <param name="stream"></param>
		public AMFWriter(Stream stream) : base(stream)
		{
			Reset();
		}

        internal AMFWriter(AMFWriter writer, Stream stream)
            : base(stream)
        {
            _amf0ObjectReferences = writer._amf0ObjectReferences;
            _objectReferences = writer._objectReferences;
            _stringReferences = writer._stringReferences;
            _classDefinitionReferences = writer._classDefinitionReferences;
            _useLegacyCollection = writer._useLegacyCollection;
        }
        /// <summary>
        /// Resets object references.
        /// </summary>
		public void Reset()
		{
#if !(NET_1_1)
            _amf0ObjectReferences = new Dictionary<Object, int>(5);
            _objectReferences = new Dictionary<Object, int>(5);
            _stringReferences = new Dictionary<Object, int>(5);
            _classDefinitionReferences = new Dictionary<ClassDefinition, int>();
#else
			_amf0ObjectReferences = new Hashtable(5);
			_objectReferences = new Hashtable(5);
			_stringReferences = new Hashtable(5);
			_classDefinitionReferences = new Hashtable();
#endif
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
        /// Gets or sets whether legacy exception serialization is used for AMF3.
        /// </summary>
        public bool UseLegacyThrowable
        {
            get { return _useLegacyThrowable; }
            set { _useLegacyThrowable = value; }
        }
        /// <summary>
        /// Writes a byte to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A byte to write to the stream.</param>
		public void WriteByte(byte value)
		{
			BaseStream.WriteByte(value);
		}
        /// <summary>
        /// Writes a stream of bytes to the current position in the AMF stream.
        /// </summary>
        /// <param name="buffer">The memory buffer containing the bytes to write to the AMF stream</param>
		public void WriteBytes(byte[] buffer)
		{
			for(var i = 0; buffer != null && i < buffer.Length; i++)
				BaseStream.WriteByte(buffer[i]);
		}
        /// <summary>
        /// Writes a 16-bit unsigned integer to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A 16-bit unsigned integer.</param>
        public void WriteShort(int value)
		{
            var bytes = BitConverter.GetBytes((ushort)value);
			WriteBigEndian(bytes);
		}
        /// <summary>
        /// Writes an UTF-8 string to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">The UTF-8 string.</param>
        /// <remarks>Standard or long string header is written depending on the string length.</remarks>
		public void WriteString(string value)
		{
			var utf8Encoding = new UTF8Encoding(true, true);
            var byteCount = utf8Encoding.GetByteCount(value);
			if( byteCount < 65536 )
			{
				WriteByte(AMF0TypeCode.String);
                WriteUTF(value);
			}
			else
			{
				WriteByte(AMF0TypeCode.LongString);
                WriteLongUTF(value);
			}
		}
        /// <summary>
        /// Writes a UTF-8 string to the current position in the AMF stream.
        /// The length of the UTF-8 string in bytes is written first, as a 16-bit integer, followed by the bytes representing the characters of the string.
        /// </summary>
        /// <param name="value">The UTF-8 string.</param>
        /// <remarks>Standard or long string header is not written.</remarks>
        public void WriteUTF(string value)
		{
			//null string is not accepted
			//in case of custom serialization leads to TypeError: Error #2007: Parameter value must be non-null.  at flash.utils::ObjectOutput/writeUTF()

			//Length - max 65536.
			var utf8Encoding = new UTF8Encoding();
            var byteCount = utf8Encoding.GetByteCount(value);
            var buffer = utf8Encoding.GetBytes(value);
			WriteShort(byteCount);
			if (buffer.Length > 0)
				Write(buffer);
		}
        /// <summary>
        /// Writes a UTF-8 string to the current position in the AMF stream.
        /// Similar to WriteUTF, but does not prefix the string with a 16-bit length word.
        /// </summary>
        /// <param name="value">The UTF-8 string.</param>
        /// <remarks>Standard or long string header is not written.</remarks>
        public void WriteUTFBytes(string value)
		{
			//Length - max 65536.
			var utf8Encoding = new UTF8Encoding();
			var buffer = utf8Encoding.GetBytes(value);
			if (buffer.Length > 0)
				Write(buffer);
		}

        private void WriteLongUTF(string value)
		{
			var utf8Encoding = new UTF8Encoding(true, true);
            var byteCount = (uint)utf8Encoding.GetByteCount(value);
			var buffer = new Byte[byteCount+4];
			//unsigned long (always 32 bit, big endian byte order)
			buffer[0] = (byte)((byteCount >> 0x18) & 0xff);
			buffer[1] = (byte)((byteCount >> 0x10) & 0xff);
			buffer[2] = (byte)((byteCount >> 8) & 0xff);
			buffer[3] = (byte)((byteCount & 0xff));
            var bytesEncodedCount = utf8Encoding.GetBytes(value, 0, value.Length, buffer, 4);
            if (buffer.Length > 0)
                BaseStream.Write(buffer, 0, buffer.Length);
		}

		/// <summary>
        /// Serializes object graphs in Action Message Format (AMF).
		/// </summary>
        /// <param name="objectEncoding">AMF version to use.</param>
        /// <param name="data">The Object to serialize in the AMF stream.</param>
		public void WriteData(ObjectEncoding objectEncoding, object data)
		{
			//If we have ObjectEncoding.AMF3 anything that serializes to String, Number, Boolean, Date will use AMF0 encoding
			//For other types we have to switch the encoding to AMF3
			if( data == null )
			{
				WriteNull();
				return;
			}
			var type = data.GetType();
			if( _amf0ObjectReferences.ContainsKey( data ) )
			{
				WriteReference( data );
				return;
			}

            IAMFWriter amfWriter = null;
            if( AmfWriterTable[0].ContainsKey(type) )
                amfWriter = AmfWriterTable[0][type];
			//Second try with basetype (enums and arrays for example)
            if (amfWriter == null && type.BaseType != null && AmfWriterTable[0].ContainsKey(type.BaseType))
                amfWriter = AmfWriterTable[0][type.BaseType];

			if( amfWriter == null )
			{
				lock(AmfWriterTable)
				{
                    if (!AmfWriterTable[0].ContainsKey(type))
					{
						amfWriter = new AMF0ObjectWriter();
						AmfWriterTable[0].Add(type, amfWriter);
					}
					else
						amfWriter = AmfWriterTable[0][type];
				}
			}

			if( amfWriter != null )
			{
				if( objectEncoding == ObjectEncoding.AMF0 )
					amfWriter.WriteData(this, data);
				else
				{
					if( amfWriter.IsPrimitive )
						amfWriter.WriteData(this, data);
					else
					{
						WriteByte(AMF0TypeCode.AMF3Tag);
						WriteAMF3Data(data);
					}
				}
			}
			else
			{
                var msg = String.Format(Resources.TypeSerializer_NotFound, type.FullName);
				throw new FluorineException(msg);
			}
		}

		internal void AddReference(object value)
		{
			_amf0ObjectReferences.Add( value, _amf0ObjectReferences.Count);
		}

		internal void WriteReference(object value)
		{
			//Circular references
			WriteByte(AMF0TypeCode.Reference);
			WriteShort(_amf0ObjectReferences[value]);
		}
        /// <summary>
        /// Writes a null type marker to the current position in the AMF stream.
        /// </summary>
		public void WriteNull()
		{
			WriteByte(AMF0TypeCode.Null);
		}
        /// <summary>
        /// Writes a double-precision floating point number to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A double-precision floating point number.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
		public void WriteDouble(double value)
		{
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
		}
        /// <summary>
        /// Writes a single-precision floating point number to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A double-precision floating point number.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteFloat(float value)
		{
			var bytes = BitConverter.GetBytes(value);			
			WriteBigEndian(bytes);
		}
        /// <summary>
        /// Writes a 32-bit signed integer to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A 32-bit signed integer.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteInt32(int value)
		{
			var bytes = BitConverter.GetBytes(value);
			WriteBigEndian(bytes);
		}
        /// <summary>
        /// Writes a 32-bit signed integer to the current position in the AMF stream using variable length unsigned 29-bit integer encoding.
        /// </summary>
        /// <param name="value">A 32-bit signed integer.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteUInt24(int value)
        {
            var bytes = new byte[3];
            bytes[0] = (byte)(0xFF & (value >> 16));
            bytes[1] = (byte)(0xFF & (value >> 8));
            bytes[2] = (byte)(0xFF & (value >> 0));
            BaseStream.Write(bytes, 0, bytes.Length);
        }
        /// <summary>
        /// Writes a Boolean value to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteBoolean(bool value)
		{
            BaseStream.WriteByte(value ? ((byte)1) : ((byte)0));
		}
        /// <summary>
        /// Writes a 64-bit signed integer to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A 64-bit signed integer.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteLong(long value)
		{
            var bytes = BitConverter.GetBytes(value);
			WriteBigEndian(bytes);
		}

		private void WriteBigEndian(byte[] bytes)
		{
			if( bytes == null )
				return;
			for(var i = bytes.Length-1; i >= 0; i--)
			{
				BaseStream.WriteByte( bytes[i] );
			}
		}
        /// <summary>
        /// Writes a DateTime value to the current position in the AMF stream.
        /// An ActionScript Date is serialized as the number of milliseconds elapsed since the epoch of midnight on 1st Jan 1970 in the UTC time zone.
        /// </summary>
        /// <param name="value">A DateTime value.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteDateTime(DateTime value)
		{
/*
#if !SILVERLIGHT
			if( FluorineConfiguration.Instance.TimezoneCompensation == TimezoneCompensation.Auto )
				date = date.Subtract( DateWrapper.ClientTimeZone );
#endif
*/
#if !(NET_1_1)
            switch (FluorineConfiguration.Instance.TimezoneCompensation)
            {
                case TimezoneCompensation.IgnoreUTCKind:
                    //Do not convert to UTC, consider we have it in universal time
                    break;
                default:
#if !(NET_1_1)
                    value = value.ToUniversalTime();
#endif
                    break;
            }
#else
			if( FluorineConfiguration.Instance.TimezoneCompensation == TimezoneCompensation.Auto )
				value = value.Subtract( DateWrapper.ClientTimeZone );
#endif
            // Write date (milliseconds from 1970).
			var timeStart = new DateTime(1970, 1, 1);
            var span = value.Subtract(timeStart);
			var milliSeconds = (long)span.TotalMilliseconds;
            WriteDouble(milliSeconds);

#if !SILVERLIGHT
            span = TimeZone.CurrentTimeZone.GetUtcOffset(value);
			//whatever we write back, it is ignored
			//this.WriteLong(span.TotalMinutes);
			//this.WriteShort((int)span.TotalHours);
			//this.WriteShort(65236);
			if( FluorineConfiguration.Instance.TimezoneCompensation == TimezoneCompensation.None )
				WriteShort(0);
			else
				WriteShort((int)(span.TotalMilliseconds/60000));
#else
            this.WriteShort(0);
#endif
        }

#if !SILVERLIGHT
        /// <summary>
        /// Writes an XmlDocument object to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">An XmlDocument object.</param>
        /// <remarks>Xml type marker is written in the AMF stream.</remarks>
        public void WriteXmlDocument(XmlDocument value)
		{
            if (value != null)
			{
                AddReference(value);
				BaseStream.WriteByte(AMF0TypeCode.Xml);
                var xml = value.DocumentElement.OuterXml;
				WriteLongUTF(xml);
			}
			else
				WriteNull();
		}
#endif
#if !(NET_1_1) && !(NET_2_0)
        public void WriteXDocument(XDocument xDocument)
        {
            if (xDocument != null)
            {
                AddReference(xDocument);
                BaseStream.WriteByte(15);//xml code (0x0F)
                var xml = xDocument.ToString();
                WriteLongUTF(xml);
            }
            else
                WriteNull();
        }

        public void WriteXElement(XElement xElement)
        {
            if (xElement != null)
            {
                AddReference(xElement);
                BaseStream.WriteByte(15);//xml code (0x0F)
                var xml = xElement.ToString();
                WriteLongUTF(xml);
            }
            else
                WriteNull();
        }

#endif

        /// <summary>
        /// Writes an Array value to the current position in the AMF stream.
        /// </summary>
        /// <param name="objectEcoding">Object encoding used.</param>
        /// <param name="value">An Array object.</param>
        public void WriteArray(ObjectEncoding objectEcoding, Array value)
		{
            if (value == null)
				WriteNull();
			else
			{
                AddReference(value);
                WriteByte(AMF0TypeCode.Array);
                WriteInt32(value.Length);
                for (var i = 0; i < value.Length; i++)
				{
                    WriteData(objectEcoding, value.GetValue(i));
				}
			}
		}
        /// <summary>
        /// Writes an associative array to the current position in the AMF stream.
        /// </summary>
        /// <param name="objectEncoding">Object encoding used.</param>
        /// <param name="value">An Dictionary object.</param>
        public void WriteAssociativeArray(ObjectEncoding objectEncoding, IDictionary value)
		{
            if (value == null)
				WriteNull();
			else
			{
                AddReference(value);
				WriteByte(AMF0TypeCode.AssociativeArray);
                WriteInt32(value.Count);
                foreach (DictionaryEntry entry in value)
				{
					WriteUTF(entry.Key.ToString());
					WriteData(objectEncoding, entry.Value);
				}
				WriteEndMarkup();
			}
		}

        /// <summary>
        /// Writes an object to the current position in the AMF stream.
        /// </summary>
        /// <param name="objectEncoding">Object encoding used.</param>
        /// <param name="obj">The object to serialize.</param>
        public void WriteObject(ObjectEncoding objectEncoding, object obj)
		{
			if( obj == null )
			{
				WriteNull();
				return;
			}
			AddReference(obj);

			var type = obj.GetType();

			WriteByte(16);
			var customClass = type.FullName;
			customClass = FluorineConfiguration.Instance.GetCustomClass(customClass);

            if( log.IsDebugEnabled )
				log.Debug(String.Format(Resources.TypeMapping_Write, type.FullName, customClass));
			WriteUTF( customClass );

            var classDefinition = GetClassDefinition(obj);
            if (classDefinition == null)
            {
                //Something went wrong in our reflection?
                var msg = String.Format(Resources.Fluorine_Fatal, "serializing " + obj.GetType().FullName);
                if (log.IsFatalEnabled)
                    log.Fatal(msg);
                Debug.Assert(false, msg);
                return;
            }
            var proxy = ObjectProxyRegistry.Instance.GetObjectProxy(type);
            for (var i = 0; i < classDefinition.MemberCount; i++)
            {
                var cm = classDefinition.Members[i];
                WriteUTF(cm.Name);
                var memberValue = proxy.GetValue(obj, cm);
                WriteData(objectEncoding, memberValue);
            }
			WriteEndMarkup();
		}

		internal void WriteEndMarkup()
		{
			//Write the end object flag 0x00, 0x00, 0x09
			BaseStream.WriteByte(0);
			BaseStream.WriteByte(0);
			BaseStream.WriteByte(AMF0TypeCode.EndOfObject);
		}
        /// <summary>
        /// Writes an anonymous ActionScript object to the current position in the AMF stream.
        /// </summary>
        /// <param name="objectEncoding">Object encoding to use.</param>
        /// <param name="asObject">The ActionScript object.</param>
		public void WriteASO(ObjectEncoding objectEncoding, ASObject asObject)
		{
			if( asObject != null )
			{
				AddReference(asObject);
				if(asObject.Alias == null)
				{
					// Object "Object"
					BaseStream.WriteByte(3);
				}
				else
				{
					BaseStream.WriteByte(16);
					WriteUTF(asObject.Alias);
				}
#if !(NET_1_1)
                foreach (var entry in asObject)
#else
				foreach(DictionaryEntry entry in asObject)
#endif
                {
					WriteUTF(entry.Key);
					WriteData(objectEncoding, entry.Value);
				}
				WriteEndMarkup();
			}
			else
				WriteNull();
		}

	    /// <summary>
        /// Serializes object graphs in Action Message Format (AMF).
        /// </summary>
        /// <param name="data">The Object to serialize in the AMF stream.</param>
        public void WriteAMF3Data(object data)
		{
			if( data == null )
			{
				WriteAMF3Null();
				return;
			}
			if(data is DBNull )
			{
				WriteAMF3Null();
				return;
			}
            var type = data.GetType();
            IAMFWriter amfWriter = null;
            if (AmfWriterTable[3].ContainsKey(type))
                amfWriter = AmfWriterTable[3][type];
			//Second try with basetype (Enums for example)
            if (amfWriter == null && type.BaseType != null && AmfWriterTable[3].ContainsKey(type.BaseType))
                amfWriter = AmfWriterTable[3][type.BaseType];

			if( amfWriter == null )
			{
				lock(AmfWriterTable)
				{
                    if (!AmfWriterTable[3].ContainsKey(type))
                    {
                        amfWriter = new AMF3ObjectWriter();
                        AmfWriterTable[3].Add(type, amfWriter);
                    }
                    else
                        amfWriter = AmfWriterTable[3][type];
				}
			}

			if( amfWriter != null )
			{
				amfWriter.WriteData(this, data);
			}
			else
			{
                var msg = string.Format("Could not find serializer for type {0}", type.FullName);
				throw new FluorineException(msg);
			}
			//WriteByte(AMF3TypeCode.Object);
			//WriteAMF3Object(data);
		}
        /// <summary>
        /// Writes a null type marker to the current position in the AMF stream.
        /// </summary>
		public void WriteAMF3Null()
		{
			//Write the null code (0x1) to the output stream.
			WriteByte(AMF3TypeCode.Null);
		}
        /// <summary>
        /// Writes a Boolean value to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
		public void WriteAMF3Bool(bool value)
		{
			WriteByte( value ? AMF3TypeCode.BooleanTrue : AMF3TypeCode.BooleanFalse);
		}
        /// <summary>
        /// Writes an Array value to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">An Array object.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteAMF3Array(Array value)
		{
            if (_amf0ObjectReferences.ContainsKey(value))
			{
                WriteReference(value);
				return;
			}

            if (!_objectReferences.ContainsKey(value))
			{
                _objectReferences.Add(value, _objectReferences.Count);
                var handle = value.Length;
				handle = handle << 1;
				handle = handle | 1;
				WriteAMF3IntegerData(handle);
				WriteAMF3UTF(string.Empty);//hash name
                for (var i = 0; i < value.Length; i++)
				{
                    WriteAMF3Data(value.GetValue(i));
				}
			}
			else
			{
                var handle = _objectReferences[value];
				handle = handle << 1;
				WriteAMF3IntegerData(handle);
			}
		}
        /// <summary>
        /// Writes an Array value to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">An Array object.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
		public void WriteAMF3Array(IList value)
		{
            if (!_objectReferences.ContainsKey(value))
			{
				_objectReferences.Add(value, _objectReferences.Count);
				var handle = value.Count;
				handle = handle << 1;
				handle = handle | 1;
				WriteAMF3IntegerData(handle);
				WriteAMF3UTF(string.Empty);//hash name
				for(var i = 0; i < value.Count; i++)
				{
					WriteAMF3Data(value[i]);
				}
			}
			else
			{
				var handle = _objectReferences[value];
				handle = handle << 1;
				WriteAMF3IntegerData(handle);
			}
		}
        /// <summary>
        /// Writes an associative array to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">An Dictionary object.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
		public void WriteAMF3AssociativeArray(IDictionary value)
		{
            if (!_objectReferences.ContainsKey(value))
			{
				_objectReferences.Add(value, _objectReferences.Count);
				WriteAMF3IntegerData(1);
				foreach(DictionaryEntry entry in value)
				{
					WriteAMF3UTF(entry.Key.ToString());
					WriteAMF3Data(entry.Value);
				}
				WriteAMF3UTF(string.Empty);
			}
			else
			{
				var handle = _objectReferences[value];
				handle = handle << 1;
				WriteAMF3IntegerData(handle);
			}
		}

		internal void WriteByteArray(ByteArray byteArray)
		{
			_objectReferences.Add(byteArray, _objectReferences.Count);
			WriteByte(AMF3TypeCode.ByteArray);
			var handle = (int)byteArray.Length;
			handle = handle << 1;
			handle = handle | 1;
			WriteAMF3IntegerData(handle);
			WriteBytes( byteArray.MemoryStream.ToArray() );
		}

        
        public void WriteAMF3IntVector(IList<int> value)
        {
            if (!_objectReferences.ContainsKey(value))
            {
                _objectReferences.Add(value, _objectReferences.Count);
                var handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (var i = 0; i < value.Count; i++)
                {
                    WriteInt32(value[i]);
                }
            }
            else
            {
                var handle = _objectReferences[value];
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        
        public void WriteAMF3UIntVector(IList<uint> value)
        {
            if (!_objectReferences.ContainsKey(value))
            {
                _objectReferences.Add(value, _objectReferences.Count);
                var handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (var i = 0; i < value.Count; i++)
                {
                    WriteInt32((int)value[i]);
                }
            }
            else
            {
                var handle = _objectReferences[value];
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        
        public void WriteAMF3DoubleVector(IList<double> value)
        {
            if (!_objectReferences.ContainsKey(value))
            {
                _objectReferences.Add(value, _objectReferences.Count);
                var handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (var i = 0; i < value.Count; i++)
                {
                    WriteDouble(value[i]);
                }
            }
            else
            {
                var handle = _objectReferences[value];
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        
        public void WriteAMF3ObjectVector(IList<string> value)
        {
            WriteAMF3ObjectVector(string.Empty, value as IList);
        }

        
        public void WriteAMF3ObjectVector(IList<Boolean> value)
        {
            WriteAMF3ObjectVector(string.Empty, value as IList);
        }

        
        public void WriteAMF3ObjectVector(IList value)
        {
            var listItemType = ReflectionUtils.GetListItemType(value.GetType());
            WriteAMF3ObjectVector(listItemType.FullName, value);
        }

        private void WriteAMF3ObjectVector(string typeIdentifier, IList value)
        {
            if (!_objectReferences.ContainsKey(value))
            {
                _objectReferences.Add(value, _objectReferences.Count);
                var handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                WriteAMF3String(typeIdentifier);
                for (var i = 0; i < value.Count; i++)
                {
                    WriteAMF3Data(value[i]);
                }
            }
            else
            {
                var handle = _objectReferences[value];
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        /// <summary>
        /// Writes a UTF-8 string to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">The UTF-8 string.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteAMF3UTF(string value)
		{
			if( value == string.Empty )
			{
				WriteAMF3IntegerData(1);
			}
			else
			{
                if (!_stringReferences.ContainsKey(value))
				{
					_stringReferences.Add(value, _stringReferences.Count);
					var utf8Encoding = new UTF8Encoding();
					var byteCount = utf8Encoding.GetByteCount(value);
					var handle = byteCount;
					handle = handle << 1;
					handle = handle | 1;
					WriteAMF3IntegerData(handle);
					var buffer = utf8Encoding.GetBytes(value);
					if (buffer.Length > 0)
						Write(buffer);
				}
				else
				{
					var handle = _stringReferences[value];
					handle = handle << 1;
					WriteAMF3IntegerData(handle);
				}
			}
		}
        /// <summary>
        /// Writes an UTF-8 string to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">The UTF-8 string.</param>
        public void WriteAMF3String(string value)
		{
			WriteByte(AMF3TypeCode.String);
			WriteAMF3UTF(value);
		}
        /// <summary>
        /// Writes a DateTime value to the current position in the AMF stream.
        /// An ActionScript Date is serialized as the number of milliseconds elapsed since the epoch of midnight on 1st Jan 1970 in the UTC time zone.
        /// Local time zone information is not sent.
        /// </summary>
        /// <param name="value">A DateTime value.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteAMF3DateTime(DateTime value)
		{
            if (!_objectReferences.ContainsKey(value))
			{
				_objectReferences.Add(value, _objectReferences.Count);
				var handle = 1;
				WriteAMF3IntegerData(handle);

				// Write date (milliseconds from 1970).
				var timeStart = new DateTime(1970, 1, 1, 0, 0, 0);
                switch (FluorineConfiguration.Instance.TimezoneCompensation)
                {
                    case TimezoneCompensation.IgnoreUTCKind:
                        //Do not convert to UTC, consider we have it in universal time
                        break;
                    default:
#if !(NET_1_1)
                        value = value.ToUniversalTime();
#endif
                        break;
                }

                var span = value.Subtract(timeStart);
				var milliSeconds = (long)span.TotalMilliseconds;
				//long date = BitConverter.DoubleToInt64Bits((double)milliSeconds);
				//this.WriteLong(date);
                WriteDouble(milliSeconds);
			}
			else
			{
				var handle = _objectReferences[value];
				handle = handle << 1;
				WriteAMF3IntegerData(handle);
			}
		}

		private void WriteAMF3IntegerData(int value)
		{
			//Sign contraction - the high order bit of the resulting value must match every bit removed from the number
			//Clear 3 bits 
			value &= 0x1fffffff;
			if(value < 0x80)
				WriteByte((byte)value);
			else
				if(value < 0x4000)
			{
					WriteByte((byte)(value >> 7 & 0x7f | 0x80));
					WriteByte((byte)(value & 0x7f));
			}
			else
				if(value < 0x200000)
			{
				WriteByte((byte)(value >> 14 & 0x7f | 0x80));
				WriteByte((byte)(value >> 7 & 0x7f | 0x80));
				WriteByte((byte)(value & 0x7f));
			} 
			else
			{
				WriteByte((byte)(value >> 22 & 0x7f | 0x80));
				WriteByte((byte)(value >> 15 & 0x7f | 0x80));
				WriteByte((byte)(value >> 8 & 0x7f | 0x80));
				WriteByte((byte)(value & 0xff));
			}
		}
        /// <summary>
        /// Writes a 32-bit signed integer to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A 32-bit signed integer.</param>
        /// <remarks>Type marker is written in the AMF stream.</remarks>
		public void WriteAMF3Int(int value)
		{
			if(value >= -268435456 && value <= 268435455)//check valid range for 29bits
			{
				WriteByte(AMF3TypeCode.Integer);
				WriteAMF3IntegerData(value);
			}
			else
			{
				//overflow condition would occur upon int conversion
				WriteAMF3Double(value);
			}
		}
        /// <summary>
        /// Writes a double-precision floating point number to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">A double-precision floating point number.</param>
        /// <remarks>Type marker is written in the AMF stream.</remarks>
		public void WriteAMF3Double(double value)
		{
			WriteByte(AMF3TypeCode.Number);
			//long tmp = BitConverter.DoubleToInt64Bits( double.Parse(value.ToString()) );
            WriteDouble(value);
		}

#if !SILVERLIGHT
        /// <summary>
        /// Writes an XmlDocument object to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">An XmlDocument object.</param>
        /// <remarks>Xml type marker is written in the AMF stream.</remarks>
        public void WriteAMF3XmlDocument(XmlDocument value)
		{
			WriteByte(AMF3TypeCode.Xml);
            var xml = string.Empty;
            if (value.DocumentElement != null && value.DocumentElement.OuterXml != null)
                xml = value.DocumentElement.OuterXml;
            if (xml == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                if (!_objectReferences.ContainsKey(value))
                {
                    _objectReferences.Add(value, _objectReferences.Count);
                    var utf8Encoding = new UTF8Encoding();
                    var byteCount = utf8Encoding.GetByteCount(xml);
                    var handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    var buffer = utf8Encoding.GetBytes(xml);
                    if (buffer.Length > 0)
                        Write(buffer);
                }
                else
                {
                    var handle = _objectReferences[value];
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
		}
#endif
#if !(NET_1_1) && !(NET_2_0)
        public void WriteAMF3XDocument(XDocument xDocument)
        {
            WriteByte(AMF3TypeCode.Xml);
            var value = string.Empty;
            if (xDocument != null)
                value = xDocument.ToString();
            if (value == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                if (!_objectReferences.ContainsKey(value))
                {
                    _objectReferences.Add(value, _objectReferences.Count);
                    var utf8Encoding = new UTF8Encoding();
                    var byteCount = utf8Encoding.GetByteCount(value);
                    var handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    var buffer = utf8Encoding.GetBytes(value);
                    if (buffer.Length > 0)
                        Write(buffer);
                }
                else
                {
                    var handle = _objectReferences[value];
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
        }

        public void WriteAMF3XElement(XElement xElement)
        {
            WriteByte(AMF3TypeCode.Xml);
            var value = string.Empty;
            if (xElement != null)
                value = xElement.ToString();
            if (value == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                if (!_objectReferences.ContainsKey(value))
                {
                    _objectReferences.Add(value, _objectReferences.Count);
                    var utf8Encoding = new UTF8Encoding();
                    var byteCount = utf8Encoding.GetByteCount(value);
                    var handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    var buffer = utf8Encoding.GetBytes(value);
                    if (buffer.Length > 0)
                        Write(buffer);
                }
                else
                {
                    var handle = _objectReferences[value];
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
        }
#endif

        /// <summary>
        /// Writes an object to the current position in the AMF stream.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <remarks>No type marker is written in the AMF stream.</remarks>
        public void WriteAMF3Object(object value)
		{
            if (!_objectReferences.ContainsKey(value))
			{
				_objectReferences.Add(value, _objectReferences.Count);

				var classDefinition = GetClassDefinition(value);
                if (classDefinition == null)
                {
                    //Something went wrong in our reflection?
                    var msg = String.Format(Resources.Fluorine_Fatal, "serializing " + value.GetType().FullName);
#if !SILVERLIGHT
                    if (log.IsFatalEnabled)
                        log.Fatal(msg);
#endif
                    Debug.Assert(false, msg);
                    return;
                }
                if (_classDefinitionReferences.ContainsKey(classDefinition))
                {
                    //Existing class-def
                    var handle = _classDefinitionReferences[classDefinition];//handle = classRef 0 1
                    handle = handle << 2;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                }
                else
				{//inline class-def
					
					//classDefinition = CreateClassDefinition(value);
                    _classDefinitionReferences.Add(classDefinition, _classDefinitionReferences.Count);
					//handle = memberCount dynamic externalizable 1 1
					var handle = classDefinition.MemberCount;
					handle = handle << 1;
					handle = handle | (classDefinition.IsDynamic ? 1 : 0);
					handle = handle << 1;
					handle = handle | (classDefinition.IsExternalizable ? 1 : 0);
					handle = handle << 2;
					handle = handle | 3;
					WriteAMF3IntegerData(handle);
					WriteAMF3UTF(classDefinition.ClassName);
					for(var i = 0; i < classDefinition.MemberCount; i++)
					{
						var key = classDefinition.Members[i].Name;
						WriteAMF3UTF(key);
					}
				}
				//write inline object
				if( classDefinition.IsExternalizable )
				{
					if( value is IExternalizable )
					{
						var externalizable = value as IExternalizable;
						var dataOutput = new DataOutput(this);
						externalizable.WriteExternal(dataOutput);
					}
					else
						throw new FluorineException(String.Format(Resources.Externalizable_CastFail,classDefinition.ClassName));
				}
				else
				{
                    var type = value.GetType();
                    var proxy = ObjectProxyRegistry.Instance.GetObjectProxy(type);

					for(var i = 0; i < classDefinition.MemberCount; i++)
					{
                        //object memberValue = GetMember(value, classDefinition.Members[i]);
                        var memberValue = proxy.GetValue(value, classDefinition.Members[i]);
                        WriteAMF3Data(memberValue);
					}

					if(classDefinition.IsDynamic)
					{
						var dictionary = value as IDictionary;
						foreach(DictionaryEntry entry in dictionary)
						{
							WriteAMF3UTF(entry.Key.ToString());
							WriteAMF3Data(entry.Value);
						}
						WriteAMF3UTF(string.Empty);
					}
				}
			}
			else
			{
				//handle = objectRef 0
				var handle = _objectReferences[value];
				handle = handle << 1;
				WriteAMF3IntegerData(handle);
			}
		}

		private ClassDefinition GetClassDefinition(object obj)
		{
            ClassDefinition classDefinition = null;
            if (obj is ASObject)
            {
                var asObject = obj as ASObject;
                if (asObject.IsTypedObject)
                    classDefinitions.TryGetValue(asObject.Alias, out classDefinition);
                if (classDefinition != null)
                    return classDefinition;

                var proxy = ObjectProxyRegistry.Instance.GetObjectProxy(typeof(ASObject));
                classDefinition = proxy.GetClassDefinition(obj);
                if (asObject.IsTypedObject)
                {
                    //Only typed ASObject class definitions are cached.
                    classDefinitions[asObject.Alias] = classDefinition;
                }
                return classDefinition;
            }
		    var typeName = obj.GetType().FullName;
		    if( !classDefinitions.TryGetValue(typeName, out classDefinition))
		    {
		        var proxy = ObjectProxyRegistry.Instance.GetObjectProxy(obj.GetType());
		        classDefinition = proxy.GetClassDefinition(obj);
		        classDefinitions[typeName] = classDefinition;
		    }
		    return classDefinition;
		}
	}
}
