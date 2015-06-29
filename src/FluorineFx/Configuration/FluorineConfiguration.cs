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

using System.Configuration;
using System.Threading;
using System.Xml.Serialization;

namespace FluorineFx.Configuration
{
    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public enum RemotingServiceAttributeConstraint
    {
        /// <summary>
        /// All public .NET classes are accessible to clients (can act as a remoting service).
        /// </summary>
        [XmlEnum(Name = "browse")]
        Browse = 1,

        /// <summary>
        /// Only classes marked [FluorineFx.RemotingServiceAttribute] are accessible to clients.
        /// </summary>
        [XmlEnum(Name = "access")]
        Access = 2
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public enum TimezoneCompensation
    {
        /// <summary>
        /// No timezone compensation.
        /// </summary>
        [XmlEnum(Name = "none")]
        None = 0,

        /// <summary>
        /// Auto timezone compensation.
        /// </summary>
        [XmlEnum(Name = "auto")]
        Auto = 1,

        /// <summary>
        /// Convert to the server timezone.
        /// </summary>
        [XmlEnum(Name = "server")]
        Server = 2,

        /// <summary>
        /// Ignore UTCKind for DateTimes received from the client code.
        /// </summary>
        [XmlEnum(Name = "ignoreUTCKind")]
        IgnoreUTCKind = 3
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class FluorineConfiguration
    {
        private static readonly object ObjLock = new object();
        private static volatile FluorineConfiguration instance;

        private static FluorineSettings fluorineSettings;
        private static bool fullTrust;


        private FluorineConfiguration() {}

        /// <summary>
        /// Gets the current Fluorine configuration object.
        /// </summary>
        public static FluorineConfiguration Instance
        {
            get
            {
                if (instance == null) {
                    lock (ObjLock) {
                        if (instance == null) {
                            instance = new FluorineConfiguration();
                            fluorineSettings = (FluorineSettings) ConfigurationManager.GetSection("fluorinefx/settings") ?? new FluorineSettings();
                            fullTrust = CheckApplicationPermissions();
                            Thread.MemoryBarrier();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Gets the Fluorine settings object.
        /// </summary>
        public FluorineSettings FluorineSettings
        {
            get { return fluorineSettings; }
        }

        internal ClassMappingCollection ClassMappings
        {
            get { return fluorineSettings.ClassMappings; }
        }

        internal string GetMappedTypeName(string customClass)
        {
            return ClassMappings != null ? ClassMappings.GetType(customClass) : customClass;
        }

        internal string GetCustomClass(string type)
        {
            return ClassMappings != null ? ClassMappings.GetCustomClass(type) : type;
        }

        /// <summary>
        /// Gets a value indicating whether to accept null value types.
        /// </summary>
        public bool AcceptNullValueTypes
        {
            get
            {
                return fluorineSettings != null && fluorineSettings.AcceptNullValueTypes;
            }
        }

        /// <summary>
        /// Gets the timezone compensation setting.
        /// </summary>
        public TimezoneCompensation TimezoneCompensation
        {
            get
            {
                return fluorineSettings != null ? fluorineSettings.TimezoneCompensation : TimezoneCompensation.None;
            }
        }

        /// <summary>
        /// Gets the optimizer settings.
        /// </summary>
        public OptimizerSettings OptimizerSettings
        {
            get
            {
                return fluorineSettings != null ? fluorineSettings.Optimizer : null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the gateway is running under full trust.
        /// </summary>
        public bool FullTrust
        {
            get { return fullTrust; }
        }

        private static bool CheckApplicationPermissions()
        {
            return false;
        }
    }
}