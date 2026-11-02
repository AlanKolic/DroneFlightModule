using System.Linq;
using DroneFlightModule.Composition;
using DroneFlightModule.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneFlightModule.Editor
{
    /// <summary>
    /// Builds the single-drone prototype scene from primitives, so the scene can be recreated
    /// from code instead of being edited by hand.
    /// </summary>
    public sealed class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DronePrototype.unity";

        // Unity menu items must be static, so this only hands over to an instance.
        [MenuItem("DroneFlightModule/Create Prototype Scene")]
        private static void CreateFromMenu()
        {
            new PrototypeSceneBuilder().Create();
        }

        public void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateLight();
            var ground = CreateGround();
            var drone = CreateDrone();
            CreateScope(drone, ground.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
        }

        private void CreateCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, -12f), Quaternion.Euler(20f, 0f, 0f));
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

        private void CreateScope(DroneView drone, Transform ground)
        {
            var scopeObject = new GameObject("Drone Scope");
            var scope = scopeObject.AddComponent<DroneLifetimeScope>();

            var serialized = new SerializedObject(scope);
            serialized.FindProperty("droneView").objectReferenceValue = drone;
            serialized.FindProperty("groundPlane").objectReferenceValue = ground;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The simulation owns all physics, so primitives keep only their mesh and renderer.
        private GameObject CreatePrimitive(PrimitiveType type, string name)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            return primitive;
        }

        private void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
