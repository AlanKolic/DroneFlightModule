using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class DisconnectableInputProviderTests
    {
        private static readonly ControlInput Climb = new ControlInput(Vec3.Up, 0f);

        private ScriptedInputProvider source;
        private DisconnectableInputProvider provider;

        [SetUp]
        public void SetUp()
        {
            source = new ScriptedInputProvider { Current = Climb };
            provider = new DisconnectableInputProvider(source);
        }

        [Test]
        public void Constructor_RequiresSource()
        {
            Assert.Throws<ArgumentNullException>(() => new DisconnectableInputProvider(null));
        }

        [Test]
        public void LinkUp_PassesSourceThrough()
        {
            Assert.That(provider.IsConnected, Is.True);
            Assert.That(provider.Read(), Is.EqualTo(Climb));
        }

        [Test]
        public void LinkCut_ReportsDisconnectedAndReadsZero()
        {
            provider.IsLinkCut = true;

            Assert.That(provider.IsConnected, Is.False);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }

        [Test]
        public void SourceDisconnected_ReportsDisconnectedEvenWithLinkUp()
        {
            source.IsConnected = false;

            Assert.That(provider.IsConnected, Is.False);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }

        [Test]
        public void LinkRestored_PassesSourceThroughAgain()
        {
            provider.IsLinkCut = true;
            provider.IsLinkCut = false;

            Assert.That(provider.Read(), Is.EqualTo(Climb));
        }
    }
}
