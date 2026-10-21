using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class CannonSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/CannonRange.unity";
    [MenuItem("Cannon/Build demonstration scene")]
    public static void Build()
    {
        foreach (string folder in new[] { "Materials", "Prefabs", "Scenes" }) Directory.CreateDirectory("Assets/" + folder);
        var oldScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (oldScene.isDirty && string.IsNullOrEmpty(oldScene.path)) EditorSceneManager.SaveScene(oldScene, "Assets/Scenes/OriginalUntitled.unity");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Time.fixedDeltaTime = 0.02f;
        Physics.gravity = new Vector3(0, -9.81f, 0);
        PlayerSettings.runInBackground = true;
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Cannon";
        tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue = "Projectile";
        tags.ApplyModifiedPropertiesWithoutUndo();
        Physics.IgnoreLayerCollision(8, 9, true);
        var ground = Material("Ground", new Color(0.12f, 0.22f, 0.25f));
        var dark = Material("Gunmetal", new Color(0.12f, 0.16f, 0.2f));
        var gold = Material("Brass", new Color(0.95f, 0.57f, 0.13f));
        var orange = Material("Target", new Color(1, 0.25f, 0.1f));
        var cyan = Material("Trajectory", new Color(0.2f, 1, 0.9f), true);
        var white = Material("Markings", new Color(0.45f, 0.68f, 0.7f), true);
        Primitive("Ground", PrimitiveType.Cube, null, new Vector3(0,-0.5f,35), new Vector3(100,1,130), ground);
        for (int z = 0; z <= 90; z += 10)
            Primitive("Range stripe " + z, PrimitiveType.Cube, null, new Vector3(0,0.015f,z), new Vector3(46,0.02f,0.08f), white, false);
        for (int x = -20; x <= 20; x += 10)
            Primitive("Lane", PrimitiveType.Cube, null, new Vector3(x,0.015f,40), new Vector3(0.06f,0.02f,100), white, false);
        var cannon = new GameObject("Cannon").AddComponent<CannonController>();
        cannon.gameObject.layer = 8;
        Primitive("Carriage", PrimitiveType.Cube, cannon.transform, new Vector3(0,0.7f,0), new Vector3(1.8f,0.8f,2.4f), dark);
        foreach (float x in new[] {-1.05f,1.05f})
        {
            var wheel = Primitive("Wheel", PrimitiveType.Cylinder, cannon.transform, new Vector3(x,0.65f,0), new Vector3(1.3f,0.18f,1.3f), gold);
            wheel.transform.localRotation = Quaternion.Euler(0,0,90);
        }
        var pivot = new GameObject("BarrelPivot").transform; pivot.SetParent(cannon.transform, false);
        pivot.localPosition = new Vector3(0,1.45f,0); pivot.localRotation = Quaternion.Euler(-20,0,0);
        var barrel = Primitive("Barrel", PrimitiveType.Cylinder, pivot, new Vector3(0,0,1.35f), new Vector3(0.65f,1.5f,0.65f), dark);
        barrel.transform.localRotation = Quaternion.Euler(90,0,0);
        var rim = Primitive("Muzzle rim", PrimitiveType.Cylinder, pivot, new Vector3(0,0,2.8f), new Vector3(0.8f,0.1f,0.8f), gold);
        rim.transform.localRotation = Quaternion.Euler(90,0,0);
        var muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot,false); muzzle.localPosition = new Vector3(0,0,3.15f);
        foreach (Transform t in cannon.GetComponentsInChildren<Transform>()) t.gameObject.layer = 8;
        cannon.muzzle = muzzle; cannon.barrelPivot = pivot;
        var line = new GameObject("Trajectory").AddComponent<LineRenderer>();
        line.useWorldSpace = true; line.widthMultiplier = 0.09f; line.sharedMaterial = cyan; line.numCapVertices = 4;
        cannon.trajectory = line.gameObject.AddComponent<TrajectoryRenderer>(); cannon.trajectory.muzzle = muzzle;
        var ball = Primitive("Projectile", PrimitiveType.Sphere,null,Vector3.zero,Vector3.one,gold);
        ball.layer = 9; ball.AddComponent<Rigidbody>(); ball.AddComponent<QuadraticDrag>();
        cannon.projectilePrefab = PrefabUtility.SaveAsPrefabAsset(ball,"Assets/Prefabs/Projectile.prefab").GetComponent<QuadraticDrag>(); Object.DestroyImmediate(ball);
        var target = Primitive("AirTarget",PrimitiveType.Sphere,null,Vector3.zero,Vector3.one,orange);
        target.AddComponent<Rigidbody>().useGravity = false; target.AddComponent<Target>();
        var targetPrefab = PrefabUtility.SaveAsPrefabAsset(target,"Assets/Prefabs/AirTarget.prefab").GetComponent<Target>(); Object.DestroyImmediate(target);
        var spawner = new GameObject("TargetSpawner").AddComponent<TargetSpawner>(); spawner.targetPrefab = targetPrefab;
        foreach (float x in new[] {-15f,15f})
        {
            Primitive("Target zone post",PrimitiveType.Cube,null,new Vector3(x,4,40),new Vector3(0.15f,8,0.15f),gold,false);
        }
        var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.transform.position = new Vector3(45,32,-32); camera.transform.LookAt(new Vector3(0,3,30));
        camera.fieldOfView = 53; camera.farClipPlane = 250; camera.backgroundColor = new Color(0.06f,0.1f,0.17f); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
        light.transform.rotation = Quaternion.Euler(45,-35,0); light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f,0.65f,0.75f);
        CreateHud(cannon);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Cannon scene saved: " + ScenePath);
    }
    static Material Material(string name, Color color, bool unlit = false)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetColor("_BaseColor",color); EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 s, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false); go.transform.localPosition = p; go.transform.localScale = s;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    static void CreateHud(CannonController cannon)
    {
        if (!Resources.Load<TMP_Settings>("TMP Settings")) TMP_PackageResourceImporter.ImportResources(true, false, false);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Materials/HudFont.asset");
        if (!font)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSans-Regular.ttf");
            font = TMP_FontAsset.CreateFontAsset(source);
            AssetDatabase.CreateAsset(font,"Assets/Materials/HudFont.asset");
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach (var tex in font.atlasTextures) if (tex) AssetDatabase.AddObjectToAsset(tex,font);
        }
        var canvas = new GameObject("HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = 0.5f;
        var title = Text("Title",canvas.transform,font,new Vector2(24,-22),new Vector2(1000,80),32);
        title.text = "ПУШКА И МИШЕНИ  <color=#62E8DB>/  BALLISTICS LAB</color>";
        var readout = Text("Readout",canvas.transform,font,new Vector2(24,-110),new Vector2(630,160),23);
        var hud = canvas.AddComponent<CannonHud>(); hud.cannon = cannon; hud.readout = readout;
        var controls = Text("Controls",canvas.transform,font,new Vector2(24,24),new Vector2(1500,60),23);
        controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = controls.rectTransform.pivot = new Vector2(0,0);
        controls.text = "WASD  движение    Q / E  поворот    SPACE  выстрел    ↑ / ↓  угол ствола\n35 m/s  •  воздушные мишени: 8 m / 40 m";
    }
    static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, Vector2 pos, Vector2 size, int fontSize)
    {
        var text = new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); text.transform.SetParent(parent,false);
        var r = text.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1); r.anchoredPosition = pos; r.sizeDelta = size;
        text.font = font; text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false; return text;
    }
}
