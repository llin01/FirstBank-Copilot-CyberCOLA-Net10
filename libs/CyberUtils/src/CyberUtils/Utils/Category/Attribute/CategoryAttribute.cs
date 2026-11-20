namespace Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute
{
    #region DEPENDENCY

    using System;

    #endregion

    [AttributeUsage(AttributeTargets.All)]
    public class CategoryAttribute : System.Attribute
    {
        public CategoryAttribute(string name = null)
        {
            this.Name = name ?? this.GetType().Name;
        }

        public string Name { get; private set; }
    }
}
