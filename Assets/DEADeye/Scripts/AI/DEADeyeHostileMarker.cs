using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Overhead tactical indicator displaying a red downward-pointing triangle above hostile targets.
    /// Billboards dynamically toward the active player camera.
    /// </summary>
    public class DEADeyeHostileMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [SerializeField] private float heightOffset = 2.2f;
        [SerializeField] private Vector3 markerScale = new Vector3(0.35f, 0.45f, 0.35f);
        [SerializeField] private Color markerColor = new Color(0.95f, 0.15f, 0.15f, 0.95f);

        private GameObject markerGO;
        private Camera targetCamera;

        private void Start()
        {
            CreateProceduralMarker();
        }

        private void CreateProceduralMarker()
        {
            markerGO = new GameObject("HostileIndicator_RedTriangle");
            markerGO.transform.SetParent(transform, false);
            markerGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            markerGO.transform.localScale = markerScale;

            MeshFilter mf = markerGO.AddComponent<MeshFilter>();
            MeshRenderer mr = markerGO.AddComponent<MeshRenderer>();

            // Procedural inverted downward-pointing triangle mesh
            Mesh mesh = new Mesh();
            mesh.name = "InvertedTriangle";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0.5f, 0f),  // Top Left
                new Vector3(0.5f, 0.5f, 0f),   // Top Right
                new Vector3(0f, -0.5f, 0f),    // Bottom Tip pointing down
                // Back face
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0f, -0.5f, 0f)
            };
            mesh.triangles = new int[] { 0, 1, 2, 3, 4, 5 };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader) { name = "HostileMarkerMat" };
            mat.color = markerColor;
            mr.sharedMaterial = mat;
        }

        private void LateUpdate()
        {
            if (markerGO == null) return;

            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                // Face the camera
                markerGO.transform.rotation = Quaternion.LookRotation(markerGO.transform.position - targetCamera.transform.position);

                // Subtle bobbing animation
                float bob = Mathf.Sin(Time.time * 4f) * 0.05f;
                markerGO.transform.localPosition = new Vector3(0f, heightOffset + bob, 0f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (markerGO != null)
            {
                markerGO.SetActive(visible);
            }
        }
    }
}

