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

using System.Collections.Specialized;
using System.Reflection;

namespace FluorineFx.IO.Writers
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    class AMF0NameObjectCollectionWriter : IAMFWriter
    {
        public bool IsPrimitive{ get{return false;} }

        public void WriteData(AMFWriter writer, object data)
        {
            var collection = data as NameObjectCollectionBase;
            var attributes = collection.GetType().GetCustomAttributes(typeof(DefaultMemberAttribute), false);
            if (attributes != null  && attributes.Length > 0)
            {
                var defaultMemberAttribute = attributes[0] as DefaultMemberAttribute;
                var pi = collection.GetType().GetProperty(defaultMemberAttribute.MemberName, new[] { typeof(string) });
                if (pi != null)
                {
                    var aso = new ASObject();
                    for (var i = 0; i < collection.Keys.Count; i++)
                    {
                        var key = collection.Keys[i];
                        var value = pi.GetValue(collection, new object[]{ key });
                        aso.Add(key, value);
                    }
                    writer.WriteASO(ObjectEncoding.AMF0, aso);
                    return;
                }
            }

            //We could not access an indexer so write out as it is.
            writer.WriteObject(ObjectEncoding.AMF0, data);
        }
    }
}
