using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CustomTransformTool : EditorWindow
{
    private Vector3 position;
    private Vector3 rotation;
    private Vector3 scale = Vector3.one;

    private enum SnapMode { None, Grid, Vertex, Edge }
    private SnapMode snapMode = SnapMode.Grid;

    private bool snapToGrid = false;
    private bool snapX = true;
    private bool snapY = true;
    private bool snapZ = true;
    private float gridSize = 1f;

    private List<TransformPreset> presets = new List<TransformPreset>();
    private List<TransformState> transformHistory = new List<TransformState>();

    private GameObject newParent;
    private AlignMode alignMode = AlignMode.Center;

    private Color gizmoColor = Color.green;
    private float gizmoSize = 0.5f;

    [MenuItem("Tools/Custom Transform Tool")]
    public static void ShowWindow()
    {
        GetWindow<CustomTransformTool>("Custom Transform Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Transform Settings", EditorStyles.boldLabel);

        position = EditorGUILayout.Vector3Field(new GUIContent("Position", "Set the position of the selected object(s)"), position);
        rotation = EditorGUILayout.Vector3Field(new GUIContent("Rotation", "Set the rotation of the selected object(s)"), rotation);
        scale = EditorGUILayout.Vector3Field(new GUIContent("Scale", "Set the scale of the selected object(s)"), scale);

        snapMode = (SnapMode)EditorGUILayout.EnumPopup(new GUIContent("Snap Mode", "Choose the snapping mode"), snapMode);

        if (snapMode == SnapMode.Grid)
        {
            snapToGrid = EditorGUILayout.Toggle(new GUIContent("Snap to Grid", "Enable snapping to grid"), snapToGrid);
            if (snapToGrid)
            {
                snapX = EditorGUILayout.Toggle("Snap X", snapX);
                snapY = EditorGUILayout.Toggle("Snap Y", snapY);
                snapZ = EditorGUILayout.Toggle("Snap Z", snapZ);
                gridSize = EditorGUILayout.FloatField(new GUIContent("Grid Size", "Set the size of the grid for snapping"), gridSize);
            }
        }

        if (GUILayout.Button("Apply Transform"))
        {
            ApplyTransform();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Alignment & Distribution", EditorStyles.boldLabel);

        alignMode = (AlignMode)EditorGUILayout.EnumPopup(new GUIContent("Align Mode", "Select how objects should be aligned"), alignMode);

        if (GUILayout.Button(new GUIContent("Align to X Axis", "Align selected objects along the X axis")))
        {
            AlignObjects(Axis.X);
        }

        if (GUILayout.Button(new GUIContent("Distribute Evenly", "Distribute selected objects evenly between the first and last selected objects")))
        {
            DistributeObjects();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Parent/Child Management", EditorStyles.boldLabel);

        newParent = (GameObject)EditorGUILayout.ObjectField(new GUIContent("New Parent", "Set the new parent for selected objects"), newParent, typeof(GameObject), true);

        if (GUILayout.Button(new GUIContent("Reparent Selected Objects", "Set the parent of the selected objects to the specified object")))
        {
            ReparentObjects();
        }

        if (GUILayout.Button(new GUIContent("Center Child to Parent", "Center the selected child objects to their respective parents")))
        {
            CenterChildToParent();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Transform Presets", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent("Save Preset", "Save the current transform settings as a preset")))
        {
            SavePreset();
        }

        if (presets.Count > 0)
        {
            GUILayout.Label("Load Preset:");
            foreach (var preset in presets)
            {
                if (GUILayout.Button(preset.name))
                {
                    LoadPreset(preset);
                }
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Transform History", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent("Save Transform State", "Save the current transform state of the selected objects")))
        {
            SaveTransformState();
        }

        if (transformHistory.Count > 0)
        {
            GUILayout.Label("Transform History:");
            foreach (var state in transformHistory)
            {
                if (GUILayout.Button(state.name))
                {
                    LoadTransformState(state);
                }
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Gizmo Customization", EditorStyles.boldLabel);

        gizmoColor = EditorGUILayout.ColorField(new GUIContent("Gizmo Color", "Set the color of the gizmo"), gizmoColor);
        gizmoSize = EditorGUILayout.FloatField(new GUIContent("Gizmo Size", "Set the size of the gizmo"), gizmoSize);
    }

    private void ApplyTransform()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Custom Transform Tool");

            if (snapMode == SnapMode.Grid && snapToGrid)
            {
                obj.transform.position = SnapToGrid(position);
            }
            else if (snapMode == SnapMode.Vertex)
            {
                obj.transform.position = SnapToNearestVertex(position);
            }
            else if (snapMode == SnapMode.Edge)
            {
                obj.transform.position = SnapToNearestEdge(position);
            }
            else
            {
                obj.transform.position = position;
            }

            obj.transform.rotation = Quaternion.Euler(rotation);
            obj.transform.localScale = scale;
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            snapX ? Mathf.Round(position.x / gridSize) * gridSize : position.x,
            snapY ? Mathf.Round(position.y / gridSize) * gridSize : position.y,
            snapZ ? Mathf.Round(position.z / gridSize) * gridSize : position.z
        );
    }

    private Vector3 SnapToNearestVertex(Vector3 position)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Mesh mesh = hit.collider.GetComponent<MeshFilter>().sharedMesh;
            Transform hitTransform = hit.collider.transform;
            Vector3 closestVertex = Vector3.zero;
            float minDistance = float.MaxValue;

            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 worldVertex = hitTransform.TransformPoint(vertex);
                float distance = Vector3.Distance(worldVertex, hit.point);
                if (distance < minDistance)
                {
                    closestVertex = worldVertex;
                    minDistance = distance;
                }
            }

            return closestVertex;
        }
        return position;
    }

    private Vector3 SnapToNearestEdge(Vector3 position)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Mesh mesh = hit.collider.GetComponent<MeshFilter>().sharedMesh;
            Transform hitTransform = hit.collider.transform;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 closestPoint = Vector3.zero;
            float minDistance = float.MaxValue;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = hitTransform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = hitTransform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = hitTransform.TransformPoint(vertices[triangles[i + 2]]);

                Vector3 edgePoint = ClosestPointOnEdge(position, v0, v1, v2);
                float distance = Vector3.Distance(edgePoint, position);
                if (distance < minDistance)
                {
                    closestPoint = edgePoint;
                    minDistance = distance;
                }
            }

            return closestPoint;
        }
        return position;
    }

    private Vector3 ClosestPointOnEdge(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 edge1 = ProjectPointOnLineSegment(v0, v1, point);
        Vector3 edge2 = ProjectPointOnLineSegment(v1, v2, point);
        Vector3 edge3 = ProjectPointOnLineSegment(v2, v0, point);

        float dist1 = Vector3.Distance(point, edge1);
        float dist2 = Vector3.Distance(point, edge2);
        float dist3 = Vector3.Distance(point, edge3);

        if (dist1 < dist2 && dist1 < dist3)
            return edge1;
        else if (dist2 < dist1 && dist2 < dist3)
            return edge2;
        else
            return edge3;
    }

    private Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        return a + Mathf.Clamp01(t) * ab;
    }

    private void AlignObjects(Axis axis)
    {
        if (Selection.gameObjects.Length < 2) return;

        float value = 0f;
        switch (alignMode)
        {
            case AlignMode.Center:
                value = Selection.gameObjects[0].transform.position[(int)axis];
                break;
            case AlignMode.Min:
                value = Mathf.Min(Selection.gameObjects.Select(obj => obj.transform.position[(int)axis]).ToArray());
                break;
            case AlignMode.Max:
                value = Mathf.Max(Selection.gameObjects.Select(obj => obj.transform.position[(int)axis]).ToArray());
                break;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Align Objects");
            Vector3 position = obj.transform.position;
            position[(int)axis] = value;
            obj.transform.position = position;
        }
    }

    private void DistributeObjects()
    {
        if (Selection.gameObjects.Length < 2) return;

        Vector3 firstPosition = Selection.gameObjects[0].transform.position;
        Vector3 lastPosition = Selection.gameObjects[Selection.gameObjects.Length - 1].transform.position;

        for (int i = 1; i < Selection.gameObjects.Length - 1; i++)
        {
            Undo.RecordObject(Selection.gameObjects[i].transform, "Distribute Objects");
            Selection.gameObjects[i].transform.position = Vector3.Lerp(firstPosition, lastPosition, i / (float)(Selection.gameObjects.Length - 1));
        }
    }

    private void ReparentObjects()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.SetTransformParent(obj.transform, newParent.transform, "Reparent Objects");
        }
    }

    private void CenterChildToParent()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.transform.parent != null)
            {
                Undo.RecordObject(obj.transform, "Center Child to Parent");
                obj.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void SavePreset()
    {
        TransformPreset newPreset = new TransformPreset
        {
            name = "Preset " + (presets.Count + 1),
            position = position,
            rotation = rotation,
            scale = scale
        };
        presets.Add(newPreset);
    }

    private void LoadPreset(TransformPreset preset)
    {
        position = preset.position;
        rotation = preset.rotation;
        scale = preset.scale;
    }

    private void SaveTransformState()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            TransformState state = new TransformState
            {
                name = obj.name + " - " + System.DateTime.Now.ToString("HH:mm:ss"),
                position = obj.transform.position,
                rotation = obj.transform.rotation.eulerAngles,
                scale = obj.transform.localScale
            };
            transformHistory.Add(state);
        }
    }

    private void LoadTransformState(TransformState state)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Load Transform State");
            obj.transform.position = state.position;
            obj.transform.rotation = Quaternion.Euler(state.rotation);
            obj.transform.localScale = state.scale;
        }
    }

    private void OnDrawGizmos()
    {
        if (Selection.activeGameObject != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(Selection.activeGameObject.transform.position, Vector3.one * gizmoSize);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Selection.activeTransform != null)
        {
            Transform activeTransform = Selection.activeTransform;

            if (snapMode == SnapMode.Vertex || snapMode == SnapMode.Edge)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                Handles.color = Color.green;
                Vector3 snappedPosition = activeTransform.position;

                if (snapMode == SnapMode.Vertex)
                {
                    snappedPosition = SnapToNearestVertex(activeTransform.position);
                }
                else if (snapMode == SnapMode.Edge)
                {
                    snappedPosition = SnapToNearestEdge(activeTransform.position);
                }

                activeTransform.position = snappedPosition;
                Handles.DrawWireCube(snappedPosition, Vector3.one * 0.1f);
                SceneView.RepaintAll();
            }
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    [System.Serializable]
    private class TransformPreset
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    private class TransformState
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    private enum Axis { X, Y, Z }
    private enum AlignMode { Center, Min, Max }
}
