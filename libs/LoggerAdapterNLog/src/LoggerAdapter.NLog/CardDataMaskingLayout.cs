// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CardDataMaskingLayout.cs">
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.Utils.NLog
{
    #region DEPENDENCY

    using global::NLog;
    using global::NLog.Config;
    using global::NLog.Layouts;
    using global::NLog.Targets;
    using global::NLog.Targets.Wrappers;

    #endregion

    /// <summary>
    /// Wraps a target's layout so the fully rendered line - message, exception text and anything else in the
    /// layout - is passed through <see cref="CardDataMasker"/> before it is written. Applying it to every target
    /// covers code that logs through NLog directly, including third-party DLLs (e.g. the HSM client), which the
    /// LoggerAdapter cannot intercept.
    /// </summary>
    public sealed class CardDataMaskingLayout : Layout
    {
        public CardDataMaskingLayout(Layout inner)
        {
            this.Inner = inner;
        }

        public Layout Inner { get; }

        /// <summary>Wraps the layout of every target (including targets inside wrappers) in the configuration.</summary>
        public static void Apply(LoggingConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            foreach (var target in configuration.AllTargets)
            {
                Wrap(target);
            }
        }

        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            return CardDataMasker.Mask(this.Inner.Render(logEvent));
        }

        private static void Wrap(Target target)
        {
            if (target is WrapperTargetBase wrapper && wrapper.WrappedTarget != null)
            {
                Wrap(wrapper.WrappedTarget);
            }

            if (target is CompoundTargetBase compound)
            {
                foreach (var inner in compound.Targets)
                {
                    Wrap(inner);
                }
            }

            if (target is TargetWithLayout withLayout && withLayout.Layout != null && !(withLayout.Layout is CardDataMaskingLayout))
            {
                withLayout.Layout = new CardDataMaskingLayout(withLayout.Layout);
            }
        }
    }
}
