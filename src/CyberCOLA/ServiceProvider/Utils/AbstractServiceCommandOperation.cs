// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="AbstractServiceCommandOperation.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the AbstractServiceCommandOperation type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Com.Cybersoft4u.CyberUtils.Exception;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.ServiceProvider.Utils
{
    public abstract class AbstractServiceCommandOperation
    {
        protected readonly Dictionary<string, MethodInfo> MethodMaps = new Dictionary<string, MethodInfo>();

        private Logger logger;

        protected AbstractServiceCommandOperation()
        {
            this.GetType().GetRuntimeMethods().ToList().ForEach(
                method =>
                    {
                        var attribute = method.GetCustomAttribute(typeof(ServiceCommandAttribute)) as ServiceCommandAttribute;
                        if (!ObjectUtils.IsNull(attribute))
                        {
                            if (this.MethodMaps.ContainsKey(attribute.Identifier))
                            {
                                throw new ValueAlreadySetException();
                            }

                            this.MethodMaps[attribute.Identifier] = method;
                        }
                    });
        }

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }

            set
            {
                this.logger = value;
            }
        }

        public abstract void ServiceTrap();

        public abstract void PrintServiceHelper();

        public abstract void MessagePrinter(string msg);

        protected string ReadConsoleInput()
        {
            var readline = Console.ReadLine();
            if (readline == null)
            {
                return null;
            }

            this.MessagePrinter(string.Format("Console Input: {0}", readline));
            return readline;
        }
    }
}
