using System.Collections;
using UnityEngine;

namespace DEADeyeVR.Foundation
{
    /// <summary>
    /// Foundation target component that visibly and audibly registers hits.
    /// Used to verify weapon interaction and projectile/raycast hit detection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FoundationTarget : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer targetRenderer;
        [SerializeField] private Color normalColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color hitColor = new Color(0.2f, 1.0f, 0.4f);
        [SerializeField] private float flashDuration = 0.25f;

        [Header("Readout & Scoring")]
        [SerializeField] private TextMesh readoutText;
        [SerializeField] private int hitCount = 0;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;

        private Material targetMaterial;
        private Coroutine flashRoutine;

        public int HitCount => hitCount;
        public Vector3 LastHitPoint { get; private set; }
        public Vector3 LastHitNormal { get; private set; }

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<MeshRenderer>();

            if (targetRenderer != null)
            {
                targetMaterial = targetRenderer.material;
                targetMaterial.color = normalColor;
            }

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

            UpdateReadout();
        }

        public void RegisterHit(Vector3 point, Vector3 normal)
        {
            hitCount++;
            LastHitPoint = point;
            LastHitNormal = normal;

            Debug.Log($"[FoundationTarget] Target '{name}' HIT #{hitCount} at {point} with normal {normal}");

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashColor());

            PlayHitChime();
            UpdateReadout();
        }

        private IEnumerator FlashColor()
        {
            if (targetMaterial != null)
                targetMaterial.color = hitColor;

            yield return new WaitForSeconds(flashDuration);

            if (targetMaterial != null)
                targetMaterial.color = normalColor;

            flashRoutine = null;
        }

        private void PlayHitChime()
        {
            if (audioSource == null) return;

            // Generate simple audio beep procedurally if no clip is assigned
            if (audioSource.clip == null)
            {
                int sampleRate = 44100;
                float duration = 0.15f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                float freq = 880f + (hitCount % 5) * 110f; // Pentatonic ascending tone

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = 1f - (t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
                }

                AudioClip clip = AudioClip.Create("HitTone", sampleCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                audioSource.clip = clip;
            }

            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(audioSource.clip);
        }

        private void UpdateReadout()
        {
            if (readoutText != null)
            {
                readoutText.text = $"TARGET HIT\nCOUNT: {hitCount}\nSTATUS: REGISTERED";
                readoutText.color = hitCount > 0 ? Color.green : Color.white;
            }
        }
    }
}

