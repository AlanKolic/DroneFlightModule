using System.IO;
using DroneFlightModule.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneFlightModule.Unity.Tests
{
    public class InputActionInputProviderTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";
        private const float Tolerance = 1e-4f;

        private InputActionAsset actions;
        private Keyboard keyboard;
        private InputActionInputProvider provider;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            // A copy of the shipped asset, so the tests check the real bindings without enabling the asset itself.
            actions = InputActionAsset.FromJson(File.ReadAllText(ControlsPath));
            provider = new InputActionInputProvider(actions);
        }

        public override void TearDown()
        {
            provider.Dispose();
            Object.DestroyImmediate(actions);
            base.TearDown();
        }

        private void AssertMove(Vec3 expected)
        {
            var move = provider.Read().Move;
            Assert.That(move.X, Is.EqualTo(expected.X).Within(Tolerance));
            Assert.That(move.Y, Is.EqualTo(expected.Y).Within(Tolerance));
            Assert.That(move.Z, Is.EqualTo(expected.Z).Within(Tolerance));
        }

        [Test]
        public void NoKeysPressed_ReadsZero()
        {
            Assert.That(provider.IsConnected, Is.True);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }

        [Test]
        public void W_MovesForward_S_MovesBack()
        {
            Press(keyboard.wKey);
            AssertMove(Vec3.Forward);

            Release(keyboard.wKey);
            Press(keyboard.sKey);
            AssertMove(-Vec3.Forward);
        }

        [Test]
        public void D_StrafesRight_A_StrafesLeft()
        {
            Press(keyboard.dKey);
            AssertMove(Vec3.Right);

            Release(keyboard.dKey);
            Press(keyboard.aKey);
            AssertMove(-Vec3.Right);
        }

        [Test]
        public void Diagonal_IsNormalized()
        {
            Press(keyboard.wKey);
            Press(keyboard.dKey);

            float component = Mathf.Sqrt(0.5f);
            AssertMove(new Vec3(component, 0f, component));
        }

        [Test]
        public void Space_Climbs_LeftCtrl_Descends()
        {
            Press(keyboard.spaceKey);
            AssertMove(Vec3.Up);

            Release(keyboard.spaceKey);
            Press(keyboard.leftCtrlKey);
            AssertMove(-Vec3.Up);
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
        public void Gamepad_RightStickMoves_LeftStickClimbsAndYaws()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.rightStick, new Vector2(0f, 1f));
            Set(gamepad.leftStick, new Vector2(1f, 0f));
            Assert.That(provider.Read().Move.Z, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(provider.Read().Yaw, Is.EqualTo(1f).Within(Tolerance));

            Set(gamepad.leftStick, new Vector2(0f, -1f));
            Assert.That(provider.Read().Move.Y, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void NoDevices_ReportsDisconnected()
        {
            InputSystem.RemoveDevice(keyboard);

            Assert.That(provider.IsConnected, Is.False);
            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
        }

        [Test]
        public void GamepadOnly_ReportsConnected()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.AddDevice<Gamepad>();

            Assert.That(provider.IsConnected, Is.True);
        }

        [Test]
        public void AfterDispose_ReadsZeroAndReportsDisconnected()
        {
            Press(keyboard.wKey);

            provider.Dispose();

            Assert.That(provider.Read(), Is.EqualTo(ControlInput.Zero));
            Assert.That(provider.IsConnected, Is.False);
        }
    }
}
