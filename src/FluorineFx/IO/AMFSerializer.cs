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
using System.IO;
using FluorineFx.Util;
using log4net;

namespace FluorineFx.IO
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class AMFSerializer : AMFWriter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (AMFSerializer));

        /// <summary>
        /// Initializes a new instance of the AMFSerializer class.
        /// </summary>
        /// <param name="stream"></param>
        public AMFSerializer(Stream stream) : base(stream) {}

        /// <summary>
        /// Initializes a new instance of the AMFSerializer class.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="stream"></param>
        internal AMFSerializer(AMFWriter writer, Stream stream)
            : base(writer, stream) {}

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="amfMessage"></param>
        public void WriteMessage(AMFMessage amfMessage)
        {
            try {
                WriteShort(amfMessage.Version);
                var headerCount = amfMessage.HeaderCount;
                WriteShort(headerCount);
                for (var i = 0; i < headerCount; i++) {
                    WriteHeader(amfMessage.GetHeaderAt(i), ObjectEncoding.AMF0);
                }
                var bodyCount = amfMessage.BodyCount;
                WriteShort(bodyCount);
                for (var i = 0; i < bodyCount; i++) {
                    var responseBody = amfMessage.GetBodyAt(i) as ResponseBody;
                    if (responseBody != null && !responseBody.IgnoreResults) {
                        responseBody.WriteBody(amfMessage.ObjectEncoding, this);
                    } else {
                        var amfBody = amfMessage.GetBodyAt(i);
                        ValidationUtils.ObjectNotNull(amfBody, "amfBody");
                        amfBody.WriteBody(amfMessage.ObjectEncoding, this);
                    }
                }
            } catch (Exception exception) {
                if (Log.IsFatalEnabled)
                    Log.Fatal(String.Format(Resources.Amf_SerializationFail), exception);
                throw;
            }
        }

        private void WriteHeader(AMFHeader header, ObjectEncoding objectEncoding)
        {
            Reset();
            WriteUTF(header.Name);
            WriteBoolean(header.MustUnderstand);
            WriteInt32(-1);
            WriteData(objectEncoding, header.Content);
        }
    }
}
