// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServiceCommandAttribute.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServiceCommandAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Com.Cybersoft4u.ServiceProvider.Utils
{
    #region DEPENDENCY



    #endregion

    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceCommandAttribute : Attribute
    {
        public ServiceCommandAttribute(string identifier, string name, string description = "", bool enable = true)
        {
            this.Identifier = identifier;
            this.Name = name ?? this.GetType().Name;
            this.Description = description;
            this.Enable = enable;
        }

        public string Identifier { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public bool Enable { get; private set; }
    }
}
