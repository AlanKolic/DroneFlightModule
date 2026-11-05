using System;
using System.Collections.Generic;
using System.Linq;
using DroneFlightModule.Core;
using DroneFlightModule.Pilots;
using DroneFlightModule.Telemetry;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DroneFlightModule.Composition.Tests
{
    public class ConfigAssetTests
    {
        private const string SettingsPath = "Assets/DroneFlightModule/Config/FleetSettings.asset";

        private readonly List<ScriptableObject> created = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in created)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
            created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = typeof(T).Name;
            created.Add(asset);
            return asset;
        }

        private void Set(ScriptableObject asset, string field, float value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetReference(ScriptableObject asset, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Every public property, so a value added to a Core config can't be left out of its asset.
        private void AssertSameValues(object expected, object actual)
        {
            foreach (var property in expected.GetType().GetProperties())
            {
                Assert.That(property.GetValue(actual), Is.EqualTo(property.GetValue(expected)), property.Name);
            }
        }

        private FleetSettingsAsset CompleteSettings()
        {
            var settings = Create<FleetSettingsAsset>();
            SetReference(settings, "frame", Create<DroneFrameAsset>());
            SetReference(settings, "velocityController", Create<VelocityControllerAsset>());
            SetReference(settings, "rangefinder", Create<RangefinderAsset>());
            SetReference(settings, "failsafe", Create<FailsafeAsset>());
            SetReference(settings, "telemetry", Create<TelemetryAsset>());
            SetReference(settings, "pilots", Create<PilotRosterAsset>());
            SetReference(settings, "server", Create<SimulatedServerAsset>());
            SetReference(settings, "layout", Create<FleetLayoutAsset>());
            SetReference(settings, "devices", Create<InputDevicesAsset>());
            return settings;
        }

        [Test]
        public void NewAssets_MapToTheCoreDefaults()
        {
            AssertSameValues(DroneConfig.Default, Create<DroneFrameAsset>().ToConfig());
            AssertSameValues(VelocityControllerConfig.Default, Create<VelocityControllerAsset>().ToConfig());
            AssertSameValues(RangefinderConfig.Default, Create<RangefinderAsset>().ToConfig());
            AssertSameValues(FailsafeConfig.Default, Create<FailsafeAsset>().ToConfig());
            AssertSameValues(TelemetryConfig.Default, Create<TelemetryAsset>().ToConfig());
        }

        [Test]
        public void EditedValues_ReachTheCoreConfig()
        {
            var frame = Create<DroneFrameAsset>();
            Set(frame, "mass", 2.5f);
            var failsafe = Create<FailsafeAsset>();
            Set(failsafe, "holdDuration", 4f);

            Assert.That(frame.ToConfig().Mass, Is.EqualTo(2.5f));
            Assert.That(failsafe.ToConfig().HoldDuration, Is.EqualTo(4f));
        }

        [Test]
        public void InvalidValues_AreRejectedByTheCoreConfig()
        {
            var frame = Create<DroneFrameAsset>();
            Set(frame, "mass", 0f);
            var failsafe = Create<FailsafeAsset>();
            Set(failsafe, "touchdownSinkRate", 5f);
            var server = Create<SimulatedServerAsset>();
            Set(server, "latencySeconds", float.NaN);

            Assert.Throws<ArgumentOutOfRangeException>(() => frame.ToConfig());
            Assert.Throws<ArgumentOutOfRangeException>(() => failsafe.ToConfig());
            Assert.Throws<ArgumentException>(() => server.ToLatency());
        }

        [Test]
        public void ANewRoster_HasOneRegisteredPilotAboveTheGuest()
        {
            var profiles = Create<PilotRosterAsset>().ToProfiles();

            Assert.That(profiles.Select(profile => profile.Id), Is.EqualTo(new[] { new PilotId("ace") }));
            Assert.That(profiles[0].Priority, Is.GreaterThan(PilotProfile.GuestPriority));
        }

        [Test]
        public void ANewLayout_HasThreeDronesInARow()
        {
            var layout = Create<FleetLayoutAsset>();

            Assert.That(layout.Drones.Select(drone => drone.Id), Is.EqualTo(new[] { "drone-1", "drone-2", "drone-3" }));
            Assert.That(layout.Drones.Select(drone => drone.SpawnPosition.x), Is.EqualTo(new[] { -4f, 0f, 4f }));
            Assert.That(layout.SpawnStep, Is.EqualTo(new Vector3(4f, 0f, 0f)));
        }

        [Test]
        public void NewInputDevices_AreTheKeyboardThenGamepads()
        {
            var groups = Create<InputDevicesAsset>().Groups;

            Assert.That(groups.Select(group => group.Layout), Is.EqualTo(new[] { "Keyboard", "Gamepad" }));
            Assert.That(groups.Select(group => group.BindingGroup), Is.EqualTo(new[] { "Keyboard", "Gamepad" }));
        }

        [Test]
        public void TheSettingsAsset_MapsEveryPart()
        {
            var settings = CompleteSettings().ToSettings();

            AssertSameValues(DroneConfig.Default, settings.Frame);
            AssertSameValues(FailsafeConfig.Default, settings.Failsafe);
            Assert.That(settings.ServerLatency, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
            Assert.That(settings.Pilots.Count, Is.EqualTo(1));
            Assert.That(settings.Drones.Count, Is.EqualTo(3));
            Assert.That(settings.DeviceGroups.Count, Is.EqualTo(2));
        }

        [TestCase("frame")]
        [TestCase("velocityController")]
        [TestCase("rangefinder")]
        [TestCase("failsafe")]
        [TestCase("telemetry")]
        [TestCase("pilots")]
        [TestCase("server")]
        [TestCase("layout")]
        [TestCase("devices")]
        public void AMissingPart_FailsWithItsName(string field)
        {
            var settings = CompleteSettings();
            SetReference(settings, field, null);

            var failure = Assert.Throws<InvalidOperationException>(() => settings.ToSettings());
            Assert.That(failure.Message, Does.Contain(field));
        }

        [Test]
        public void FleetSettings_RejectsMissingOrInvalidValues()
        {
            var pilots = Array.Empty<PilotProfile>();
            var drones = Array.Empty<FleetDroneEntry>();
            var groups = Array.Empty<InputDeviceGroup>();

            Assert.Throws<ArgumentNullException>(() => new FleetSettings(
                null, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                pilots, TimeSpan.Zero, 0f, Vector3.right, drones, groups));
            Assert.Throws<ArgumentNullException>(() => new FleetSettings(
                DroneConfig.Default, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                null, TimeSpan.Zero, 0f, Vector3.right, drones, groups));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FleetSettings(
                DroneConfig.Default, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                pilots, TimeSpan.Zero, 0f, new Vector3(float.NaN, 0f, 0f), drones, groups));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FleetSettings(
                DroneConfig.Default, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                pilots, TimeSpan.Zero, 0f, Vector3.right, new[] { new FleetDroneEntry("drone-1", new Vector3(0f, float.PositiveInfinity, 0f)) }, groups));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FleetSettings(
                DroneConfig.Default, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                pilots, TimeSpan.Zero, float.NaN, Vector3.right, drones, groups));
            Assert.Throws<ArgumentNullException>(() => new FleetSettings(
                DroneConfig.Default, VelocityControllerConfig.Default, RangefinderConfig.Default, FailsafeConfig.Default, TelemetryConfig.Default,
                pilots, TimeSpan.Zero, 0f, Vector3.right, drones, null));
        }

        [Test]
        public void TheShippedSettings_MapToAValidFleet()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FleetSettingsAsset>(SettingsPath);
            Assert.That(asset, Is.Not.Null, $"No fleet settings asset at {SettingsPath}.");

            var settings = asset.ToSettings();

            Assert.DoesNotThrow(() => new InMemoryPilotRegistry(settings.Pilots), "The roster must be a valid registry.");
            var ids = settings.Drones.Select(drone => new DroneId(drone.Id)).ToList();
            Assert.That(ids, Is.Not.Empty);
            Assert.That(ids, Is.Unique);
            Assert.That(settings.ServerLatency, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            Assert.That(settings.DeviceGroups, Is.Not.Empty, "Some device must get a pilot station.");
            Assert.That(settings.Drones.All(drone => drone.SpawnPosition.y >= settings.GroundHeight), Is.True, "Drones must start on or above the ground.");
        }
    }
}
