namespace Com.Cybersoft4u.TM.ISO8583.Category.Attributes
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute;

    #endregion

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ActionAttribute : CategoryAttribute
    {
        public static readonly UndefinedActionAttribute Undefined = new UndefinedActionAttribute();

        public static readonly Type UndefinedType = Undefined.GetType();

        public class UndefinedActionAttribute : ActionAttribute
        {
        }
    }
}
