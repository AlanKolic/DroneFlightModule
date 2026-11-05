using System;
using System.Linq;
using DroneFlightModule.Composition;
using DroneFlightModule.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace DroneFlightModule.Editor
{
    /// <summary>
    /// Builds the fleet prototype scene from primitives, so the scene can be recreated from code
    /// instead of being edited by hand: a drone prefab, the ground, the fleet scope that builds the
    /// drones of its settings' layout from the prefab and flies them, a follow camera, and the
    /// fleet dashboard when its UI assembly is there. The scene holds no drones of its own.
    /// </summary>
    public sealed class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DronePrototype.unity";
        private const string ControlsPath = "Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions";
        private const string DronePrefabPath = "Assets/DroneFlightModule/Unity/View/Drone.prefab";
        private const string SettingsPath = "Assets/DroneFlightModule/Config/FleetSettings.asset";
        private const string DashboardScopeType = "DroneFlightModule.UI.DashboardLifetimeScope";

        // Unity menu items must be static, so this only hands over to an instance.
        [MenuItem("DroneFlightModule/Create Prototype Scene")]
        private static void CreateFromMenu()
        {
            new PrototypeSceneBuilder().Create();
        }

        public void Create()
        {
            // Checked before the open scene is replaced, so a missing asset leaves the user's scene alone.
            LoadControls();
            LoadSettings();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Loaded again: replacing the scene unloads assets nothing references, which would leave a dead reference.
            var controls = LoadControls();
            var settings = LoadSettings();

            var prefab = CreateDronePrefab();
            var camera = CreateCamera();
            CreateLight();
            var ground = CreateGround();
            CreateScope(settings, prefab, camera, ground.transform, controls);
            CreateDashboard();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"The prototype scene could not be saved to {ScenePath}.");
            }
            AddToBuildSettings();
        }

        private InputActionAsset LoadControls()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                throw new InvalidOperationException($"Input actions asset not found at {ControlsPath}.");
            }
            return controls;
        }

        private FleetSettingsAsset LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FleetSettingsAsset>(SettingsPath);
            if (settings == null)
            {
                throw new InvalidOperationException($"Fleet settings asset not found at {SettingsPath}.");
            }
            return settings;
        }

        // The fleet scope points it at the first drone once the drones exist.
        private FollowCamera CreateCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 2.5f, -6f), Quaternion.Euler(15f, 0f, 0f));
            return camera.GetComponent<FollowCamera>();
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

        // Saved after the new scene exists, which would otherwise unload it as unused.
        private DroneView CreateDronePrefab()
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

            var prefab = PrefabUtility.SaveAsPrefabAsset(drone, DronePrefabPath, out bool isSaved);
            Object.DestroyImmediate(drone);
            if (!isSaved)
            {
                throw new InvalidOperationException($"The drone prefab could not be saved to {DronePrefabPath}.");
            }
            return prefab.GetComponent<DroneView>();
        }

        private void CreateScope(FleetSettingsAsset settings, DroneView prefab, FollowCamera camera, Transform ground, InputActionAsset controls)
        {
            var scopeObject = new GameObject("Fleet Scope");
            var hud = scopeObject.AddComponent<FlightDebugHud>();
            var scope = scopeObject.AddComponent<FleetLifetimeScope>();

            var serialized = new SerializedObject(scope);
            SetReference(serialized, "settings", settings);
            SetReference(serialized, "controls", controls);
            SetReference(serialized, "debugHud", hud);
            SetReference(serialized, "followCamera", camera);
            SetReference(serialized, "groundPlane", ground);
            SetReference(serialized, "dronePrefab", prefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Found by name, so the editor never depends on the deletable UI assembly; without it the scene has no dashboard.
        private void CreateDashboard()
        {
            var scopeType = TypeCache.GetTypesDerivedFrom<LifetimeScope>().FirstOrDefault(type => type.FullName == DashboardScopeType);
            if (scopeType == null)
            {
                Debug.Log($"No {DashboardScopeType} found, so the scene has no dashboard.");
                return;
            }
            var scope = new GameObject("Dashboard").AddComponent(scopeType);
            var serialized = new SerializedObject(scope);
            FindRelative(FindProperty(serialized, "parentReference"), "TypeName").stringValue = typeof(FleetLifetimeScope).FullName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetReference(SerializedObject serialized, string field, Object value)
        {
            FindProperty(serialized, field).objectReferenceValue = value;
        }

        // Fails with the field name instead of a null reference when a serialized field is renamed.
        private SerializedProperty FindProperty(SerializedObject serialized, string field)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{serialized.targetObject.GetType().Name} has no serialized field '{field}'.");
            }
            return property;
        }

        private SerializedProperty FindRelative(SerializedProperty parent, string field)
        {
            var property = parent.FindPropertyRelative(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{parent.type} has no serialized field '{field}'.");
            }
            return property;
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
