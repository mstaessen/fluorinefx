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
using FluorineFx.AMF3;
using FluorineFx.Configuration;
using FluorineFx.IO.Bytecode;
using FluorineFx.IO.Bytecode.CodeDom;
using log4net;

namespace FluorineFx.IO.Readers
{
	/// <summary>
	/// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
	/// </summary>
	class AMF0OptimizedObjectReader : IAMFReader
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(AMF0OptimizedObjectReader));

		Hashtable _optimizedReaders;

		public AMF0OptimizedObjectReader()
		{
			_optimizedReaders = new Hashtable();
		}

	    public object ReadData(AMFReader reader)
		{
			object instance = null;
			var typeIdentifier = reader.ReadString();
			if(log.IsDebugEnabled )
			{
				var msg = string.Format("Attempt to read custom object {0}", typeIdentifier);
				log.Debug(msg);
			}
            var reflectionOptimizer = _optimizedReaders[typeIdentifier] as IReflectionOptimizer;
			if( reflectionOptimizer == null )
			{
				lock(_optimizedReaders)
				{
                    if (!_optimizedReaders.Contains(typeIdentifier))
                    {
						if(log.IsDebugEnabled )
						{
							var msg = string.Format("Generating optimizer for type {0}", typeIdentifier);
							log.Debug(msg);
						}

                        //Temporary reader
                        _optimizedReaders[typeIdentifier] = new AMF0TempObjectReader();
                        var type = ObjectFactory.Locate(typeIdentifier);
						if( type != null )
						{
							instance = ObjectFactory.CreateInstance(type);
							reader.AddReference(instance);
							if (type != null)
							{
								IBytecodeProvider bytecodeProvider = null;
#if NET_1_1
								//codedom only
								if( FluorineConfiguration.Instance.OptimizerSettings != null )
									bytecodeProvider = new FluorineFx.IO.Bytecode.CodeDom.BytecodeProvider();
#else
								if (FluorineConfiguration.Instance.OptimizerSettings.Provider == "codedom")
									bytecodeProvider = new BytecodeProvider();
								if (FluorineConfiguration.Instance.OptimizerSettings.Provider == "il")
									bytecodeProvider = new Bytecode.Lightweight.BytecodeProvider();
#endif

								reflectionOptimizer = bytecodeProvider.GetReflectionOptimizer(type, null, reader, instance);
								//Fixup
								if (reflectionOptimizer != null)
									_optimizedReaders[typeIdentifier] = reflectionOptimizer;
								else
                                    _optimizedReaders[typeIdentifier] = new AMF0TempObjectReader();
							}
						}
						else
						{
							if( log.IsWarnEnabled )
								log.Warn("Custom object " + typeIdentifier + " could not be loaded. An ActionScript typed object (ASObject) will be created");

                            reflectionOptimizer = new AMF0TypedASObjectReader(typeIdentifier);
                            _optimizedReaders[typeIdentifier] = reflectionOptimizer;
                            instance = reflectionOptimizer.ReadData(reader, null);
						}
                    }
                    else
                    {
                        reflectionOptimizer = _optimizedReaders[typeIdentifier] as IReflectionOptimizer;
                        instance = reflectionOptimizer.ReadData(reader, null);
                    }
				}
			}
			else
			{
				instance = reflectionOptimizer.ReadData(reader, null);
			}
			return instance;
		}
	}

    class AMF0TempObjectReader : IReflectionOptimizer
    {
        public object CreateInstance()
        {
            throw new NotImplementedException();
        }

        public object ReadData(AMFReader reader, ClassDefinition classDefinition)
        {
            var amfObject = reader.ReadObject();
            return amfObject;
        }
    }
}
