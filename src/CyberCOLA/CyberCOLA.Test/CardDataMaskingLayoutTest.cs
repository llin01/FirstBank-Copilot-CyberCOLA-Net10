using Com.Cybersoft4u.Utils.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Synthetic test card numbers only - never real card data.
    [TestFixture]
    public class CardDataMaskingLayoutTest
    {
        private const string Pan = "4000000000000002";

        private static (LogFactory Factory, MemoryTarget Target) CreateFactory(string layout)
        {
            var target = new MemoryTarget("memory") { Layout = layout };
            var config = new LoggingConfiguration();
            config.AddRuleForAllLevels(target);
            CardDataMaskingLayout.Apply(config);
            var factory = new LogFactory { Configuration = config };
            return (factory, target);
        }

        [Test]
        public void Apply_MessageWithPan_WritesMaskedLine()
        {
            var (factory, target) = CreateFactory("${message}");

            factory.GetLogger("test").Info($"CardNbr is :[{Pan}]");

            Assert.That(target.Logs, Is.EqualTo(new[] { "CardNbr is :[************0002]" }));
        }

        [Test]
        public void Apply_ExceptionWithPan_MasksExceptionText()
        {
            // Exception text is rendered by the layout, not the message, so the logger adapter alone missed it.
            var (factory, target) = CreateFactory("${message}|${exception:format=message}");

            factory.GetLogger("test").Error(new InvalidOperationException($"bad track {Pan}=2901201112"), "failed");

            Assert.That(target.Logs, Is.EqualTo(new[] { "failed|bad track ************0002=**********" }));
        }

        [Test]
        public void Apply_TargetInsideWrapper_WrapsInnerLayout()
        {
            var inner = new MemoryTarget("inner") { Layout = "${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(new AsyncTargetWrapper("async", inner));

            CardDataMaskingLayout.Apply(config);

            Assert.That(inner.Layout, Is.TypeOf<CardDataMaskingLayout>());
        }

        [Test]
        public void Apply_CalledTwice_DoesNotWrapTwice()
        {
            var target = new MemoryTarget("memory") { Layout = "${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);

            CardDataMaskingLayout.Apply(config);
            var first = target.Layout;
            CardDataMaskingLayout.Apply(config);

            Assert.That(target.Layout, Is.SameAs(first));
        }
    }
}
