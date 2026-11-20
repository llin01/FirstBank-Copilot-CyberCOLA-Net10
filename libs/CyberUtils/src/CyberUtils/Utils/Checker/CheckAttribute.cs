namespace Com.Cybersoft4u.CyberUtils.Utils.Checker
{
    #region DEPENDENCY

    using System;
    using System.Linq;
    using System.Reflection;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute;

    #endregion

    [AttributeUsage(AttributeTargets.All)]
    public class CheckAttribute : CategoryAttribute
    {
        public Type Type;

        static CheckAttribute()
        {
            CheckClass();
        }

        public CheckAttribute(Type type)
        {
            this.Type = type;
        }

        private static void CheckClass()
        {
            var assemblies = AssemblyUtils.GetAllAssemblies();
            assemblies.ToList()
                .ForEach(
                    assembly =>
                    {
                        assembly.GetTypes()
                            .Where(type => !ObjectUtils.IsNull(type.GetCustomAttribute(typeof(CheckAttribute))))
                            .ToList()
                            .ForEach(
                                needCheckType =>
                                assemblies.ToList()
                                    .ForEach(
                                        checkAssembly =>
                                        checkAssembly.GetTypes()
                                            .Where(
                                                type =>
                                                type.IsClass && !type.IsAbstract && type.IsSubclassOf(needCheckType))
                                            .ToList()
                                            .Where(
                                                type =>
                                                !type.GetCustomAttributes(
                                                    ((CheckAttribute)
                                                     needCheckType.GetCustomAttribute(typeof(CheckAttribute))).Type)
                                                     .Any())
                                            .ToList()
                                            .ForEach(
                                                type =>
                                                {
                                                    throw new ConstraintNotEnoughException(
                                                        string.Format(
                                                            "{0} should have attribute {1}",
                                                            type.FullName,
                                                            needCheckType.GetCustomAttribute(
                                                            typeof(CheckAttribute)).GetType().FullName));
                                                })));
                    });
        }
    }
}
