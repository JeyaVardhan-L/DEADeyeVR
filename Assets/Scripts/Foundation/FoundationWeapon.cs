using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DEADeyeVR.Foundation
{
    /// <summary>
    /// Simple weapon-shaped interactable for Phase 1 clean foundation testing.
    /// Can be picked up, aimed, and fired using standard XRI triggers or simulated inputs.
    /// Emits a raycast tracer and notifies FoundationTarget upon impact.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FoundationWeapon : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private Transform barrelPoint;
        [SerializeField] private float maxRange = 50f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Tracer & Visuals")]
        [SerializeField] private LineRenderer tracerRenderer;
        [SerializeField] private float tracerDuration = 0.08f;
        [SerializeField] private Color tracerColor = new Color(0.2f, 0.8f, 1f, 0.9f);

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private XRGrabInteractable grabInteractable;
        private Coroutine tracerRoutine;

        public int TotalShotsFired { get; private set; }
        public int TotalHitsRegistered { get; private set; }
        public bool IsHeld => grabInteractable != null && grabInteractable.isSelected;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (barrelPoint == null)
            {
                // Create a default barrel point at the front of the weapon model
                GameObject bp = new GameObject("BarrelPoint");
                bp.transform.SetParent(transform);
                bp.transform.localPosition = new Vector3(0f, 0f, 0.35f);
                bp.transform.localRotation = Quaternion.identity;
                barrelPoint = bp.transform;
            }

            SetupTracer();
            SetupAudio();

            grabInteractable.activated.AddListener(OnActivated);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
                grabInteractable.activated.RemoveListener(OnActivated);
        }

        private void SetupTracer()
        {
            if (tracerRenderer == null)
            {
                tracerRenderer = GetComponent<LineRenderer>();
                if (tracerRenderer == null)
                {
                    tracerRenderer = gameObject.AddComponent<LineRenderer>();
                    tracerRenderer.startWidth = 0.015f;
                    tracerRenderer.endWidth = 0.005f;
                    tracerRenderer.useWorldSpace = true;
                    tracerRenderer.positionCount = 2;
                    tracerRenderer.enabled = false;

                    // Unlit color material
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = tracerColor;
                    tracerRenderer.material = mat;
                }
            }
        }

        private void SetupAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f;
                }
            }
        }

        private void OnActivated(ActivateEventArgs args)
        {
            Fire();
        }

        /// <summary>
        /// Public entry point for weapon firing (works via XRI Trigger or direct method call).
        /// </summary>
        public bool Fire()
        {
            TotalShotsFired++;

            Vector3 origin = barrelPoint != null ? barrelPoint.position : transform.position;
            Vector3 direction = barrelPoint != null ? barrelPoint.forward : transform.forward;
            Vector3 endPoint = origin + direction * maxRange;

            bool hitTarget = false;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;

                FoundationTarget target = hit.collider.GetComponentInParent<FoundationTarget>();
                if (target != null)
                {
                    target.RegisterHit(hit.point, hit.normal);
                    TotalHitsRegistered++;
                    hitTarget = true;
                }
            }

            PlayShotAudio();

            if (tracerRenderer != null)
            {
                if (tracerRoutine != null)
                    StopCoroutine(tracerRoutine);
                tracerRoutine = StartCoroutine(ShowTracer(origin, endPoint));
            }

            Debug.Log($"[FoundationWeapon] Shot #{TotalShotsFired} fired from {origin} towards {direction}. Hit: {(hitTarget ? "TARGET REGISTERED" : "None/Surface")}");
            return hitTarget;
        }

        private IEnumerator ShowTracer(Vector3 start, Vector3 end)
        {
            tracerRenderer.SetPosition(0, start);
            tracerRenderer.SetPosition(1, end);
            tracerRenderer.enabled = true;

            yield return new WaitForSeconds(tracerDuration);

            tracerRenderer.enabled = false;
            tracerRoutine = null;
        }

        private void PlayShotAudio()
        {
            if (audioSource == null) return;

            if (audioSource.clip == null)
            {
                int sampleRate = 44100;
                float duration = 0.12f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-30f * t);
                    float noise = (Random.value * 2f - 1f) * 0.6f;
                    float tone = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.4f;
                    samples[i] = (noise + tone) * env;
                }

                AudioClip clip = AudioClip.Create("GunshotAudio", sampleCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                audioSource.clip = clip;
            }

            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(audioSource.clip);
        }
    }
}

