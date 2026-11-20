namespace Com.Cybersoft4u.TM.ISO8583.Category.Attributes
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute;

    #endregion

    [AttributeUsage(AttributeTargets.Class)]
    public class FieldTypeAttribute : CategoryAttribute
    {
        public FieldTypeAttribute(FIELD field)
        {
            this.Field = field;
        }

        public FIELD Field { get; private set; }
    }
}
