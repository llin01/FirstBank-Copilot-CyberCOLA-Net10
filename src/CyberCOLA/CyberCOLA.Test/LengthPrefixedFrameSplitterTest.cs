using Com.Cybersoft4u.CommonLib.Stress;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    [TestFixture]
    public class LengthPrefixedFrameSplitterTest
    {
        private static byte[] Frame(params byte[] body) =>
            new byte[] { (byte)(body.Length >> 8), (byte)body.Length }.Concat(body).ToArray();

        [Test]
        public void Split_SingleCompleteFrame_ReturnsFrameWithoutRemainder()
        {
            var frame = Frame(1, 2, 3);

            var frames = LengthPrefixedFrameSplitter.Split(frame, out var remainder);

            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(frames[0], Is.EqualTo(frame));
            Assert.That(remainder, Is.Null);
        }

        [Test]
        public void Split_SeveralFramesInOneRead_ReturnsEachFrame()
        {
            var first = Frame(1, 2);
            var second = Frame(3, 4, 5);

            var frames = LengthPrefixedFrameSplitter.Split(first.Concat(second).ToArray(), out var remainder);

            Assert.That(frames, Has.Count.EqualTo(2));
            Assert.That(frames[0], Is.EqualTo(first));
            Assert.That(frames[1], Is.EqualTo(second));
            Assert.That(remainder, Is.Null);
        }

        [Test]
        public void Split_FrameCutInBody_ReturnsCompleteFramesAndKeepsTail()
        {
            var first = Frame(1, 2);
            var second = Frame(3, 4, 5, 6);
            var read = first.Concat(second.Take(4)).ToArray();

            var frames = LengthPrefixedFrameSplitter.Split(read, out var remainder);

            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(frames[0], Is.EqualTo(first));
            Assert.That(remainder, Is.EqualTo(second.Take(4).ToArray()));
        }

        [Test]
        public void Split_LengthHeaderCutAfterFirstByte_KeepsThatByte()
        {
            var second = Frame(7, 8);
            var read = Frame(1).Concat(second.Take(1)).ToArray();

            var frames = LengthPrefixedFrameSplitter.Split(read, out var remainder);

            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(remainder, Is.EqualTo(second.Take(1).ToArray()));
        }

        [Test]
        public void Split_RemainderPrependedToNextRead_RebuildsFrame()
        {
            // Same order as WebStressSimulator: the kept tail is put in front of the next read.
            var frame = Frame(Enumerable.Range(0, 300).Select(i => (byte)i).ToArray());

            LengthPrefixedFrameSplitter.Split(frame[..100], out var remainder);
            var frames = LengthPrefixedFrameSplitter.Split(remainder!.Concat(frame[100..]).ToArray(), out var rest);

            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(frames[0], Is.EqualTo(frame));
            Assert.That(rest, Is.Null);
        }

        [Test]
        public void Split_ZeroLengthFrame_ReturnsHeaderOnlyFrame()
        {
            var frames = LengthPrefixedFrameSplitter.Split(Frame(), out var remainder);

            Assert.That(frames, Has.Count.EqualTo(1));
            Assert.That(frames[0], Has.Length.EqualTo(2));
            Assert.That(remainder, Is.Null);
        }
    }
}
