using System;
using DroneFlightModule.Core;
using NUnit.Framework;

namespace DroneFlightModule.Composition.Tests
{
    public class LocalPilotInputTests
    {
        private sealed class FullThrottle : IInputProvider
        {
            public bool IsConnected { get; set; } = true;
            public ControlInput Read() => new ControlInput(new Vec3(0f, 1f, 0f), 0f);
        }

        private FullThrottle source;
        private LocalPilotInput input;

        [SetUp]
        public void SetUp()
        {
            source = new FullThrottle();
            input = new LocalPilotInput(source);
        }

        [Test]
        public void Constructor_RequiresASource()
        {
            Assert.Throws<ArgumentNullException>(() => new LocalPilotInput(null));
        }

        [Test]
        public void TheSelectedPilot_ReadsTheSource()
        {
            input.IsSelected = true;

            Assert.That(input.Read().Move.Y, Is.EqualTo(1f));
        }

        [Test]
        public void AParkedPilot_StaysConnectedWithTheirHandsOff()
        {
            Assert.That(input.IsConnected, Is.True);
            Assert.That(input.Read().Move.Y, Is.EqualTo(0f));
        }

        [Test]
        public void ACutLink_DisconnectsEvenTheSelectedPilot()
        {
            input.IsSelected = true;
            source.IsConnected = false;

            Assert.That(input.IsConnected, Is.False);
            Assert.That(input.Read().Move.Y, Is.EqualTo(0f));
        }
    }
}
