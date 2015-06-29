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
using System.Collections.Generic;
using System.Text;

namespace FluorineFx.IO
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class AMFBody
    {
        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public const string Recordset = "rs://";

        /// <summary>
        /// Suffix to denote a success.
        /// </summary>
        public const string OnResult = "/onResult";

        /// <summary>
        /// Suffix to denote a failure.
        /// </summary>
        public const string OnStatus = "/onStatus";

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public const string OnDebugEvents = "/onDebugEvents";

        /// <summary>
        /// Initializes a new instance of the AMFBody class.
        /// </summary>
        public AMFBody() {}

        /// <summary>
        /// Initializes a new instance of the AMFBody class.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="response"></param>
        /// <param name="content"></param>
        public AMFBody(string target, string response, object content)
        {
            Target = target;
            Response = response;
            Content = content;
        }

        /// <summary>
        /// Gets or set the target URI.
        /// The target URI describes which operation, function, or method is to be remotely invoked.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Indicates an empty target.
        /// </summary>
        public bool IsEmptyTarget
        {
            get { return String.IsNullOrEmpty(Target) || Target == "null"; }
        }

        /// <summary>
        /// Gets or sets the response URI.
        /// Response URI which specifies a unique operation name that will be used to match the response to the client invocation.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Gets or sets the actual data associated with the operation.
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsAuthenticationAction { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IgnoreResults { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsDescribeService { get; set; }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsWebService
        {
            get
            {
                if (TypeName != null) {
                    if (TypeName.ToLower().EndsWith(".asmx"))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsRecordsetDelivery
        {
            get {
                return Target.StartsWith(Recordset);
            }
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public string GetRecordsetArgs()
        {
            if (Target != null) {
                if (IsRecordsetDelivery) {
                    var args = Target.Substring(Recordset.Length);
                    args = args.Substring(0, args.IndexOf("/"));
                    return args;
                }
            }
            return null;
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public string TypeName
        {
            get
            {
                if (Target != "null" && !String.IsNullOrEmpty(Target)) {
                    if (Target.LastIndexOf('.') != -1) {
                        var target = Target.Substring(0, Target.LastIndexOf('.'));
                        if (IsRecordsetDelivery) {
                            target = target.Substring(Recordset.Length);
                            target = target.Substring(target.IndexOf("/") + 1);
                            target = target.Substring(0, target.LastIndexOf('.'));
                        }
                        return target;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public string Method
        {
            get
            {
                if (Target != "null" && !String.IsNullOrEmpty(Target)) {
                    if (Target != null && Target.LastIndexOf('.') != -1) {
                        var target = Target;
                        if (IsRecordsetDelivery) {
                            target = target.Substring(Recordset.Length);
                            target = target.Substring(target.IndexOf("/") + 1);
                        }

                        if (IsRecordsetDelivery)
                            target = target.Substring(0, target.LastIndexOf('.'));
                        var method = target.Substring(target.LastIndexOf('.') + 1);

                        return method;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// This member supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public string Call
        {
            get { return TypeName + "." + Method; }
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public string GetSignature()
        {
            var sb = new StringBuilder();
            sb.Append(Target);
            var parameterList = GetParameterList();
            foreach (var parameter in parameterList) {
                sb.Append(parameter.GetType().FullName);
            }
            return sb.ToString();
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// The returned IList has property IsFixedSize set to true, no new elements can be added to it.
        /// </summary>
        public virtual IList GetParameterList()
        {
            IList list = null;
            if (!IsEmptyTarget) //Flash RPC parameters
            {
                if (!(Content is IList)) {
                    list = new List<object> {Content};
                } else
                    list = (IList) Content;
            }

            return list ?? (new List<object>());
        }

        internal void WriteBody(ObjectEncoding objectEncoding, AMFWriter writer)
        {
            writer.Reset();
            writer.WriteUTF(Target ?? "null");
            writer.WriteUTF(Response ?? "null");
            writer.WriteInt32(-1);
            WriteBodyData(objectEncoding, writer);
        }

        /// <summary>
        /// This method supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        protected virtual void WriteBodyData(ObjectEncoding objectEncoding, AMFWriter writer)
        {
            var content = Content;
            writer.WriteData(objectEncoding, content);
        }
    }
}