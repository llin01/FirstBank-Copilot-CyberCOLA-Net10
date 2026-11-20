namespace Com.Cybersoft4u.TM.ISO8583.ActionCode
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public abstract class ActionCode
    {
        public static readonly ActionCode Default = new DefaultActionCode();

        private static readonly Dictionary<string, ActionCode> V1987Map = new Dictionary<string, ActionCode>();

        private static readonly Dictionary<string, ActionCode> V2003Map = new Dictionary<string, ActionCode>();

        private static readonly Dictionary<Type, ActionCode> TypeMap = new Dictionary<Type, ActionCode>();

        static ActionCode()
        {
            var selfType = typeof(ActionCode);
            AssemblyUtils.GetAllAssemblies().ToList().ForEach(
                assembly =>
                    {
                        assembly.GetTypes()
                            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(selfType))
                            .ToList()
                            .ForEach(
                                type =>
                                    {
                                        var code = (ActionCode)Activator.CreateInstance(type);
                                        V1987Map[code.V1987String()] = code;
                                        V2003Map[code.V2003String()] = code;
                                        TypeMap[type] = code;
                                    });
                    });
            DuplicateHandler();
        }

        public static ActionCode GetActionCodeOfV1987(string value)
        {
            ActionCode code;
            try
            {
                if (!V1987Map.TryGetValue(value, out code))
                {
                    code = Default;
                }
            }
            catch (Exception)
            {
                code = Default;
            }

            return code;
        }

        public static ActionCode GetActionCodeOfV2003(string value)
        {
            ActionCode code;
            try
            {
                if (!V2003Map.TryGetValue(value, out code))
                {
                    code = Default;
                }
            }
            catch (Exception)
            {
                code = Default;
            }

            return code;
        }

        public static TActionCode GetActionCode<TActionCode>() where TActionCode : ActionCode
        {
            ActionCode code;
            try
            {
                if (!TypeMap.TryGetValue(typeof(TActionCode), out code))
                {
                    code = Default;
                }
            }
            catch (Exception)
            {
                code = Default;
            }

            return (TActionCode)code;
        }

        public abstract string V1987String();

        public abstract string V2003String();

        private static void DuplicateHandler()
        {
            V1987Map[string.Empty] = Default;
            V1987Map["00"] = TypeMap[typeof(Approved)];
        }

        public class DefaultActionCode : ActionCode
        {
            public override string V1987String()
            {
                return string.Empty;
            }

            public override string V2003String()
            {
                return string.Empty;
            }
        }
    }
}
