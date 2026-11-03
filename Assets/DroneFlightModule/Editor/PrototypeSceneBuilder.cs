using System;
using System.Linq;
using DroneFlightModule.Composition;
using DroneFlightModule.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DroneFlightModule.Editor
{
    /// <summary>
    /// Builds the single-drone prototype scene from primitives, so the scene can be recreated
    /// from code instead of being edited by hand.
    /// </summary>
    public sealed class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DronePrototype.unity";
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";

        // Unity menu items must be static, so this only hands over to an instance.
        [MenuItem("DroneFlightModule/Create Prototype Scene")]
        private static void CreateFromMenu()
        {
            new PrototypeSceneBuilder().Create();
        }

        public void Create()
        {
            // Checked before the open scene is replaced, so a missing asset leaves the user's scene alone.
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                throw new InvalidOperationException($"Input actions asset not found at {ControlsPath}.");
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var drone = CreateDrone();
            CreateCamera(drone.transform);
            CreateLight();
            var ground = CreateGround();
            CreateScope(drone, ground.transform, controls);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
        }

        private void CreateCamera(Transform target)
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 2.5f, -6f), Quaternion.Euler(15f, 0f, 0f));

            var serialized = new SerializedObject(camera.GetComponent<FollowCamera>());
            SetReference(serialized, "target", target);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CreateLight()
        {
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private GameObject CreateGround()
        {
            var ground = CreatePrimitive(PrimitiveType.Plane, "Ground");
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            return ground;
        }

        private DroneView CreateDrone()
        {
            var drone = new GameObject("Drone", typeof(DroneView));

            var body = CreatePrimitive(PrimitiveType.Cube, "Body");
            body.transform.SetParent(drone.transform, false);
            body.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);
            body.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            // A small block on the front edge makes the heading and yaw visible.
            var nose = CreatePrimitive(PrimitiveType.Cube, "Nose");
            nose.transform.SetParent(drone.transform, false);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            nose.transform.localPosition = new Vector3(0f, 0.2f, 0.4f);

            return drone.GetComponent<DroneView>();
        }

        private void CreateScope(DroneView drone, Transform ground, InputActionAsset controls)
        {
            var scopeObject = new GameObject("Drone Scope");
            var hud = scopeObject.AddComponent<FlightDebugHud>();
            var scope = scopeObject.AddComponent<DroneLifetimeScope>();

            var serialized = new SerializedObject(scope);
            SetReference(serialized, "controls", controls);
            SetReference(serialized, "droneView", drone);
            SetReference(serialized, "debugHud", hud);
            SetReference(serialized, "groundPlane", ground);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Fails with the field name instead of a null reference when a serialized field is renamed.
        private void SetReference(SerializedObject serialized, string field, Object value)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{serialized.targetObject.GetType().Name} has no serialized field '{field}'.");
            }
            property.objectReferenceValue = value;
        }

        // The simulation owns all physics, so primitives keep only their mesh and renderer.
        private GameObject CreatePrimitive(PrimitiveType type, string name)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            return primitive;
        }

        // The prototype scene goes first, so it is the one a build starts in.
        private void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
