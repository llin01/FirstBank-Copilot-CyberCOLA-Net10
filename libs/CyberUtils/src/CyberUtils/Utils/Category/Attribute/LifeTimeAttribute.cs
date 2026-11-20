namespace Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute
{
    #region DEPENDENCY

    using System;

    #endregion

    [AttributeUsage(AttributeTargets.All)]
    public class LifeTimeAttribute : CategoryAttribute
    {
        public LifeTimeAttribute(AgeEnum age = AgeEnum.Undefined)
        {
            this.Age = age;
        }

        public enum AgeEnum
        {
            Undefined,

            Long,

            Normal,

            Short
        }

        public AgeEnum Age { get; private set; }
    }
}
