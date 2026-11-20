// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServiceCommandOperationAttribute.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServiceCommandOperationAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Com.Cybersoft4u.CommonLib.Network.Utilis
{
    #region DEPENDENCY



    #endregion

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceCommandOperationAttribute : Attribute
    {
        public ServiceCommandOperationAttribute(string identifier, string name, string description = "")
        {
            this.Identifier = identifier;
            this.Name = name ?? this.GetType().Name;
            this.Description = description;
        }

        public string Identifier { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }
    }
}
