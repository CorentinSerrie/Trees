using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TreesWindow : EditorWindow
{
    private static readonly MethodInfo intersectRayMeshMethod = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    private static GameObject lastGameObjectUnderCursor;
    private static Mesh branchMesh;
    private static TreeArchetype archetype;


    private bool creatingTree = false;

    [MenuItem("Window/Trees")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TreesWindow));
    }
    void OnEnable()
    {
        if (!branchMesh)
        {
            branchMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Meshes/branch.fbx", typeof(Mesh));
        }
        SceneView.duringSceneGui += SceneGUI;
    }

    void SceneGUI(SceneView sceneView)
    {
        if (!creatingTree)
        {
            return;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GameObject tree = CreateTree();

            if (RaycastAgainstScene(out RaycastHit hit))
            {
                tree.transform.position = hit.point;
            }
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                float denominator = Vector3.Dot(ray.direction, Vector3.up);
                if (denominator > 0.00001f || denominator < -0.00001f)
                {
                    tree.transform.position = ray.origin + ray.direction * Vector3.Dot(Vector3.zero - ray.origin, Vector3.up) / denominator;
                }
                else
                {
                    Debug.Log("No intersection, tree instantiated at origin");
                }
            }
        }

        if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
        {
            creatingTree = false;
            ShowWindow();
        }
    }

    void OnGUI()
    {
        branchMesh = (Mesh)EditorGUILayout.ObjectField("Branch Mesh", branchMesh, typeof(Mesh), false);
        archetype = (TreeArchetype)EditorGUILayout.ObjectField("Tree Archetype", archetype, typeof(TreeArchetype), false);
        if (GUILayout.Button(creatingTree ? "No Tree" : "Tree"))
        {
            creatingTree = !creatingTree;
        }

        if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
        {
            creatingTree = false;
        }
    }

    private GameObject CreateTree()
    {
        GameObject tree = new GameObject("tree");
        tree.AddComponent<MeshFilter>().mesh = GenerateMesh(archetype.LoopCount, archetype.sides, Vector3.zero, Vector3.up, Vector3.forward, archetype.truncHeight, archetype.truncWidth, archetype.MainBranchHeightScaleInterval, archetype.MainBranchWidthScaleInterval, archetype.MainBranchBendingInterval, archetype.MainBranchRotationInterval, archetype.SecondaryBranchHeightScaleInterval, archetype.SecondaryBranchWidthScaleInterval, archetype.SecondaryBranchBendingInterval, archetype.SecondaryBranchRotationInterval);
        tree.AddComponent<MeshRenderer>().material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        //Branch(tree, 0);

        return tree;
    }

    private void Branch(GameObject trunc, int loop)
    {
        GameObject mainBranch = new GameObject("main_branch_" + loop);
        mainBranch.AddComponent<MeshFilter>().mesh = branchMesh;
        mainBranch.AddComponent<MeshRenderer>().material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        mainBranch.transform.parent = trunc.transform;
        mainBranch.transform.localPosition = new Vector3(0, 3.5f, 0);
        mainBranch.transform.localScale = Vector3.up * Random.Range(archetype.MainBranchHeightScaleInterval.x, archetype.MainBranchHeightScaleInterval.y) + (Vector3.forward + Vector3.right) * Random.Range(archetype.MainBranchWidthScaleInterval.x, archetype.MainBranchWidthScaleInterval.y);
        mainBranch.transform.localEulerAngles = new Vector3(0, Random.Range(archetype.MainBranchRotationInterval.x, archetype.MainBranchRotationInterval.y), Random.Range(archetype.MainBranchBendingInterval.x, archetype.MainBranchBendingInterval.y));

        if (loop < archetype.LoopCount)
        {
            Branch(mainBranch, loop + 1);
        }

        GameObject secondaryBranch = new GameObject("secondary_branch_" + loop);
        secondaryBranch.AddComponent<MeshFilter>().mesh = branchMesh;
        secondaryBranch.AddComponent<MeshRenderer>().material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        secondaryBranch.transform.parent = trunc.transform;
        secondaryBranch.transform.localPosition = new Vector3(0, 3.5f, 0);
        secondaryBranch.transform.localScale = Vector3.up * Random.Range(archetype.SecondaryBranchHeightScaleInterval.x, archetype.SecondaryBranchHeightScaleInterval.y) + (Vector3.forward + Vector3.right) * Random.Range(archetype.SecondaryBranchWidthScaleInterval.x, archetype.SecondaryBranchWidthScaleInterval.y);
        secondaryBranch.transform.localEulerAngles = new Vector3(0, Random.Range(archetype.SecondaryBranchRotationInterval.x, archetype.SecondaryBranchRotationInterval.y), Random.Range(archetype.SecondaryBranchBendingInterval.x, archetype.SecondaryBranchBendingInterval.y));

        if (loop < archetype.LoopCount)
        {
            Branch(secondaryBranch, loop + 1);
        }
    }

    private Mesh GenerateMesh(int loops, int sides, Vector3 origin, Vector3 up, Vector3 forward, float height, float width, Vector2 heightScaleInterval, Vector2 widthScaleInterval, Vector2 bendingAngleInterval, Vector2 rotationAngleInterval, Vector2 branchHeightScaleInterval, Vector2 branchWidthScaleInterval, Vector2 branchBendingAngleInterval, Vector2 branchRotationAngleInterval)
    {
        Mesh result = new Mesh();
        if (loops <= 0)
        {
            return result;
        }

        //randomize original rotation
        forward = Quaternion.AngleAxis(Random.Range(0, 360), up) * forward;

        Vector3[] vertices = new Vector3[3 * sides * (int)System.Math.Pow(2, loops - 1) - sides];
        int[] triangles = new int[3 * 2 * sides * ((int)System.Math.Pow(2, loops) - 1)];

        int verticesIndex = 0;
        int trianglesIndex = 0;
        FillArrays(loops, origin, up, forward, height, width, ref verticesIndex, ref trianglesIndex, ref vertices, ref triangles);

        result.vertices = vertices;
        result.triangles = triangles;
        result.RecalculateNormals();

        return result;


        void FillArrays(int loops, Vector3 origin, Vector3 up, Vector3 forward, float height, float width, ref int currentVerticesIndex, ref int currentTrianglesIndex, ref Vector3[] vertices, ref int[] triangles)
        {
            if (loops <= 0)
            {
                return;
            }

            int nextVerticesIndex = currentVerticesIndex + sides * (loops + 1);
            int nextTriangleIndex = currentTrianglesIndex + 3 * 2 * sides * loops;

            for (int j = 0; j < sides; j++)
            {
                vertices[currentVerticesIndex + j] = origin + Quaternion.AngleAxis(j * 360f / sides, up) * forward * width;
            }

            for (int i = 0; i < loops; i++)
            {
                origin = origin + height * up;
                width *= Random.Range(widthScaleInterval[0], widthScaleInterval[1]);

                Quaternion branchRotationAroundUp = Quaternion.AngleAxis(Random.Range(branchRotationAngleInterval[0], branchRotationAngleInterval[1]), up);
                Vector3 branchUp = branchRotationAroundUp * Quaternion.AngleAxis(Random.Range(branchBendingAngleInterval[0], branchBendingAngleInterval[1]), forward) * up;
                Vector3 branchForward = branchRotationAroundUp * forward;

                Vector3 intermediateUp = up;
                Vector3 intermediateForward = forward;

                Quaternion rotationAroundUp = Quaternion.AngleAxis(Random.Range(rotationAngleInterval[0], rotationAngleInterval[1]), up);
                up = rotationAroundUp * Quaternion.AngleAxis(Random.Range(bendingAngleInterval[0], bendingAngleInterval[1]), forward) * up;
                forward = rotationAroundUp * forward;

                intermediateUp = (intermediateUp + up).normalized;
                intermediateForward = (intermediateForward + forward).normalized;


                for (int j = 0; j < sides; j++)
                {
                    vertices[currentVerticesIndex + (i + 1) * sides + j] = origin + Quaternion.AngleAxis(j * 360f / sides, intermediateUp) * intermediateForward * width;

                    triangles[currentTrianglesIndex + 6 * (i * sides + j)] = currentVerticesIndex + i * sides + j;
                    triangles[currentTrianglesIndex + 6 * (i * sides + j) + 1] = currentVerticesIndex + i * sides + ((j + 1) % sides);
                    triangles[currentTrianglesIndex + 6 * (i * sides + j) + 2] = currentVerticesIndex + (i + 1) * sides + j;

                    triangles[currentTrianglesIndex + 6 * (i * sides + j) + 3] = currentVerticesIndex + (i + 1) * sides + j;
                    triangles[currentTrianglesIndex + 6 * (i * sides + j) + 4] = currentVerticesIndex + i * sides + ((j + 1) % sides);
                    triangles[currentTrianglesIndex + 6 * (i * sides + j) + 5] = currentVerticesIndex + (i + 1) * sides + ((j + 1) % sides);
                }

                FillArrays(loops - i - 1, origin, branchUp, branchForward,
                    height * Random.Range(branchHeightScaleInterval[0], branchHeightScaleInterval[1]),
                    width * Random.Range(branchWidthScaleInterval[0], branchWidthScaleInterval[1]),
                    ref nextVerticesIndex,
                    ref nextTriangleIndex,
                    ref vertices, ref triangles);

                height *= Random.Range(heightScaleInterval[0], heightScaleInterval[1]);
            }

            currentVerticesIndex = nextVerticesIndex;
            currentTrianglesIndex = nextTriangleIndex;
        }
    }

    public static bool RaycastAgainstScene(out RaycastHit hit)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        // First, try raycasting against scene geometry with or without colliders (it doesn't matter)
        // Credit: https://forum.unity.com/threads/editor-raycast-against-scene-meshes-without-collider-editor-select-object-using-gui-coordinate.485502
        GameObject gameObjectUnderCursor;
        switch (Event.current.type)
        {
            // HandleUtility.PickGameObject doesn't work with some EventTypes in OnSceneGUI
            case EventType.Layout:
            case EventType.Repaint:
            case EventType.ExecuteCommand: gameObjectUnderCursor = lastGameObjectUnderCursor; break;
            default: gameObjectUnderCursor = HandleUtility.PickGameObject(Event.current.mousePosition, false); break;
        }

        if (gameObjectUnderCursor)
        {
            Mesh meshUnderCursor = null;
            if (gameObjectUnderCursor.TryGetComponent(out MeshFilter meshFilter))
                meshUnderCursor = meshFilter.sharedMesh;
            if (!meshUnderCursor && gameObjectUnderCursor.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                meshUnderCursor = skinnedMeshRenderer.sharedMesh;

            if (meshUnderCursor)
            {
                // Remember this GameObject so that it can be used inside problematic EventTypes, as well
                lastGameObjectUnderCursor = gameObjectUnderCursor;

                object[] rayMeshParameters = new object[] { ray, meshUnderCursor, gameObjectUnderCursor.transform.localToWorldMatrix, null };
                if ((bool)intersectRayMeshMethod.Invoke(null, rayMeshParameters))
                {
                    hit = (RaycastHit)rayMeshParameters[3];
                    return true;
                }
            }
            else
                lastGameObjectUnderCursor = null;
        }

        // Raycast against scene geometry with colliders
        object raycastResult = HandleUtility.RaySnap(ray);
        if (raycastResult != null && raycastResult is RaycastHit)
        {
            hit = (RaycastHit)raycastResult;
            return true;
        }

        hit = new RaycastHit();
        return false;
    }
}


