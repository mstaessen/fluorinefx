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

using System.IO;

namespace FluorineFx.IO
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class AMFDeserializer : AMFReader
    {
        /// <summary>
        /// Initializes a new instance of the AMFDeserializer class.
        /// </summary>
        /// <param name="stream"></param>
        public AMFDeserializer(Stream stream) : base(stream)
        {
            FaultTolerancy = true;
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns></returns>
        
        public AMFMessage ReadAMFMessage()
        {
            // Version stored in the first two bytes.
            var version = ReadUInt16();
            var message = new AMFMessage(version);
            // Read header count.
            int headerCount = ReadUInt16();
            for (var i = 0; i < headerCount; i++) {
                message.AddHeader(ReadHeader());
            }
            // Read header count.
            int bodyCount = ReadUInt16();
            for (var i = 0; i < bodyCount; i++) {
                var amfBody = ReadBody();
                if (amfBody != null) //not failed
                    message.AddBody(amfBody);
            }
            return message;
        }

        private AMFHeader ReadHeader()
        {
            Reset();
            // Read name.
            var name = ReadString();
            // Read must understand flag.
            var mustUnderstand = ReadBoolean();
            // Read the length of the header.
            var length = ReadInt32();
            // Read content.
            var content = ReadData();
            return new AMFHeader(name, mustUnderstand, content);
        }

        private AMFBody ReadBody()
        {
            Reset();
            var target = ReadString();
            var response = ReadString();
            var length = ReadInt32();
            var content = ReadData();
            return new AMFBody(target, response, content);        
        }
    }
}