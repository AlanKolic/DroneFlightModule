using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity.Tests
{
    public class KeyboardInputProviderTests : InputTestFixture
    {
        private Keyboard keyboard;
        private KeyboardInputProvider provider;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            provider = new KeyboardInputProvider();
        }

        [Test]
        public void NoKeysPressed_ReadsZero()
        {
            Assert.That(provider.IsConnected, Is.True);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }

        [Test]
        public void W_MovesForward()
        {
            Press(keyboard.wKey);

            Assert.That(provider.Read().Move, Is.EqualTo(Vec3.Forward));
        }

        [Test]
        public void S_MovesBack()
        {
            Press(keyboard.sKey);

            Assert.That(provider.Read().Move, Is.EqualTo(-Vec3.Forward));
        }

        [Test]
        public void D_StrafesRight()
        {
            Press(keyboard.dKey);

            Assert.That(provider.Read().Move, Is.EqualTo(Vec3.Right));
        }

        [Test]
        public void A_StrafesLeft()
        {
            Press(keyboard.aKey);

            Assert.That(provider.Read().Move, Is.EqualTo(-Vec3.Right));
        }

        [Test]
        public void Space_Climbs()
        {
            Press(keyboard.spaceKey);

            Assert.That(provider.Read().Move, Is.EqualTo(Vec3.Up));
        }

        [Test]
        public void E_YawsRight_Q_YawsLeft()
        {
            Press(keyboard.eKey);
            Assert.That(provider.Read().Yaw, Is.EqualTo(1f));

            Release(keyboard.eKey);
            Press(keyboard.qKey);
            Assert.That(provider.Read().Yaw, Is.EqualTo(-1f));
        }

        [Test]
        public void OpposingKeys_CancelOut()
        {
            Press(keyboard.wKey);
            Press(keyboard.sKey);

            Assert.That(provider.Read().Move, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void NoKeyboard_ReportsDisconnectedAndReadsZero()
        {
            InputSystem.RemoveDevice(keyboard);

            Assert.That(provider.IsConnected, Is.False);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }
    }
}
