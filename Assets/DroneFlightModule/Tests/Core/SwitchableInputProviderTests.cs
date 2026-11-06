using System;
using NUnit.Framework;

namespace DroneFlightModule.Core.Tests
{
    public class SwitchableInputProviderTests
    {
        private sealed class FixedInput : IInputProvider
        {
            private readonly ControlInput input;

            public FixedInput(ControlInput input, bool isConnected)
            {
                this.input = input;
                IsConnected = isConnected;
            }

            public bool IsConnected { get; }
            public ControlInput Read() => input;
        }

        private readonly ControlInput climb = new ControlInput(Vec3.Up, 0f);
        private readonly ControlInput yaw = new ControlInput(Vec3.Zero, 1f);

        [Test]
        public void ReadsAndReportsItsCurrentSource()
        {
            var switchable = new SwitchableInputProvider(new FixedInput(climb, isConnected: false));

            Assert.That(switchable.IsConnected, Is.False);
            Assert.That(switchable.Read().Move, Is.EqualTo(Vec3.Up));
        }

        [Test]
        public void Use_TakesEffectOnTheNextRead()
        {
            var switchable = new SwitchableInputProvider(new FixedInput(climb, isConnected: false));

            switchable.Use(new FixedInput(yaw, isConnected: true));

            Assert.That(switchable.IsConnected, Is.True);
            Assert.That(switchable.Read().Yaw, Is.EqualTo(1f));
        }

        [Test]
        public void RequiresASource()
        {
            Assert.Throws<ArgumentNullException>(() => new SwitchableInputProvider(null));
            var switchable = new SwitchableInputProvider(new FixedInput(climb, isConnected: true));
            Assert.Throws<ArgumentNullException>(() => switchable.Use(null));
        }
    }
}
