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

    private Vector2 scrollPosition; // Scroll position variable

    [MenuItem("Tools/Custom Transform Tool")]
    public static void ShowWindow()
    {
        GetWindow<CustomTransformTool>("Custom Transform Tool");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // Begin scroll view

        EditorGUILayout.LabelField("Transform Settings", EditorStyles.boldLabel);

        position = EditorGUILayout.Vector3Field(new GUIContent("Position", "Set the position of the selected object(s). If grid snapping is enabled, this will snap to the nearest grid point."), position);
        rotation = EditorGUILayout.Vector3Field(new GUIContent("Rotation", "Set the rotation of the selected object(s)."), rotation);
        scale = EditorGUILayout.Vector3Field(new GUIContent("Scale", "Set the scale of the selected object(s)."), scale);

        snapMode = (SnapMode)EditorGUILayout.EnumPopup(new GUIContent("Snap Mode", "Choose the snapping mode. Grid will snap to a defined grid size, while Vertex and Edge snapping allow for snapping to nearby vertices or edges."), snapMode);

        if (snapMode == SnapMode.Grid)
        {
            snapToGrid = EditorGUILayout.Toggle(new GUIContent("Enable Grid Snapping", "Enable snapping to a grid."), snapToGrid);
            if (snapToGrid)
            {
                snapX = EditorGUILayout.Toggle("Snap X", snapX);
                snapY = EditorGUILayout.Toggle("Snap Y", snapY);
                snapZ = EditorGUILayout.Toggle("Snap Z", snapZ);
                gridSize = EditorGUILayout.FloatField(new GUIContent("Grid Size", "Set the size of the grid. Objects will snap to the nearest grid lines based on this size."), gridSize);
            }
        }

        if (GUILayout.Button("Apply Transform"))
        {
            ApplyTransform();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Alignment & Distribution", EditorStyles.boldLabel);

        alignMode = (AlignMode)EditorGUILayout.EnumPopup(new GUIContent("Align Mode", "Select how objects should be aligned along a specified axis."), alignMode);

        if (GUILayout.Button(new GUIContent("Align to X Axis", "Align selected objects along the X axis based on the chosen alignment mode (Center, Min, Max).")))
        {
            AlignObjects(Axis.X);
        }

        if (GUILayout.Button(new GUIContent("Distribute Evenly", "Distribute selected objects evenly between the first and last selected objects.")))
        {
            DistributeObjects();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Parent/Child Management", EditorStyles.boldLabel);

        newParent = (GameObject)EditorGUILayout.ObjectField(new GUIContent("New Parent", "Set the new parent for selected objects."), newParent, typeof(GameObject), true);

        if (GUILayout.Button(new GUIContent("Reparent Selected Objects", "Set the parent of the selected objects to the specified object.")))
        {
            ReparentObjects();
        }

        if (GUILayout.Button(new GUIContent("Center Child to Parent", "Center the selected child objects to their respective parents.")))
        {
            CenterChildToParent();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Transform Presets", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent("Save Preset", "Save the current transform settings as a preset.")))
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

        if (GUILayout.Button(new GUIContent("Save Transform State", "Save the current transform state of the selected objects.")))
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

        gizmoColor = EditorGUILayout.ColorField(new GUIContent("Gizmo Color", "Set the color of the gizmo in the Scene view."), gizmoColor);
        gizmoSize = EditorGUILayout.FloatField(new GUIContent("Gizmo Size", "Set the size of the gizmo in the Scene view."), gizmoSize);

        EditorGUILayout.EndScrollView(); // End scroll view
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
                obj.transform.position = SnapToNearestVertex(position, obj.transform);
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

    private Vector3 SnapToNearestVertex(Vector3 position, Transform transformToIgnore)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transformToIgnore)
                return position; // Ignore snapping to self

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

        float targetValue = Selection.gameObjects[0].transform.position[(int)axis];
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Align Objects");
            Vector3 pos = obj.transform.position;
            pos[(int)axis] = targetValue;
            obj.transform.position = pos;
        }
    }

    private void DistributeObjects()
    {
        if (Selection.gameObjects.Length < 3) return;

        Transform firstObj = Selection.gameObjects[0].transform;
        Transform lastObj = Selection.gameObjects[Selection.gameObjects.Length - 1].transform;

        float totalDistance = Vector3.Distance(firstObj.position, lastObj.position);
        float step = totalDistance / (Selection.gameObjects.Length - 1);

        for (int i = 1; i < Selection.gameObjects.Length - 1; i++)
        {
            Undo.RecordObject(Selection.gameObjects[i].transform, "Distribute Objects");
            Selection.gameObjects[i].transform.position = Vector3.Lerp(firstObj.position, lastObj.position, (float)i / (Selection.gameObjects.Length - 1));
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

    private void DrawGrid(Vector3 origin, float gridSize, int gridLines = 10)
    {
        Handles.color = Color.gray;
        int halfGridLines = gridLines / 2;

        for (int i = -halfGridLines; i <= halfGridLines; i++)
        {
            Vector3 startPosX = origin + new Vector3(i * gridSize, 0, -halfGridLines * gridSize);
            Vector3 endPosX = origin + new Vector3(i * gridSize, 0, halfGridLines * gridSize);
            Handles.DrawLine(startPosX, endPosX);

            Vector3 startPosZ = origin + new Vector3(-halfGridLines * gridSize, 0, i * gridSize);
            Vector3 endPosZ = origin + new Vector3(halfGridLines * gridSize, 0, i * gridSize);
            Handles.DrawLine(startPosZ, endPosZ);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Selection.activeTransform != null)
        {
            Transform activeTransform = Selection.activeTransform;

            if (snapMode == SnapMode.Grid && snapToGrid)
            {
                DrawGrid(activeTransform.position, gridSize);

                // Snap to grid in real-time
                Vector3 newPosition = SnapToGrid(activeTransform.position);
                if (activeTransform.position != newPosition)
                {
                    Undo.RecordObject(activeTransform, "Grid Snap");
                    activeTransform.position = newPosition;
                    GUI.changed = true;
                }
            }

            Vector3 previewPosition = activeTransform.position;

            if (snapMode == SnapMode.Vertex || snapMode == SnapMode.Edge)
            {
                if (snapMode == SnapMode.Vertex)
                {
                    previewPosition = SnapToNearestVertex(activeTransform.position, activeTransform);
                }
                else if (snapMode == SnapMode.Edge)
                {
                    previewPosition = SnapToNearestEdge(activeTransform.position);
                }

                Handles.color = Color.green;
                Handles.DrawWireCube(previewPosition, Vector3.one * 0.1f);
                SceneView.RepaintAll();

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    activeTransform.position = previewPosition;
                    Event.current.Use(); // Consume the event so it doesn't propagate
                }
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
