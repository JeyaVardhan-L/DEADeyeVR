using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative audio manager for DEADeye VR.
    /// Provides spatial sound effects, procedural audio synthesis fallbacks, and ambient city sound.
    /// </summary>
    public class DEADeyeAudioManager : MonoBehaviour
    {
        public static DEADeyeAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Audio Clips (Optional - procedural fallback if unassigned)")]
        [SerializeField] private AudioClip gunshotClip;
        [SerializeField] private AudioClip boltCycleClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip hitConfirmClip;
        [SerializeField] private AudioClip enemyAlertClip;
        [SerializeField] private AudioClip enemyEliminatedClip;
        [SerializeField] private AudioClip missionStartClip;
        [SerializeField] private AudioClip missionCompleteClip;
        [SerializeField] private AudioClip missionFailClip;
        [SerializeField] private AudioClip ambientCityClip;

        private Dictionary<string, AudioClip> proceduralCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
                ambientSource.loop = true;
                ambientSource.volume = 0.35f;
            }

            GenerateProceduralAudioClips();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void GenerateProceduralAudioClips()
        {
            if (gunshotClip == null) gunshotClip = SynthesizeGunshot();
            if (boltCycleClip == null) boltCycleClip = SynthesizeBoltCycle();
            if (reloadClip == null) reloadClip = SynthesizeReload();
            if (hitConfirmClip == null) hitConfirmClip = SynthesizeHitConfirm();
            if (enemyAlertClip == null) enemyAlertClip = SynthesizeAlert();
            if (enemyEliminatedClip == null) enemyEliminatedClip = SynthesizeEliminated();
            if (missionStartClip == null) missionStartClip = SynthesizeChime(523.25f, 0.4f); // C5
            if (missionCompleteClip == null) missionCompleteClip = SynthesizeChime(659.25f, 0.6f); // E5
            if (missionFailClip == null) missionFailClip = SynthesizeChime(311.13f, 0.8f); // Eb4
            if (ambientCityClip == null) ambientCityClip = SynthesizeAmbientCity();
        }

        public void PlayGunshot(Vector3 position)
        {
            PlaySpatial(gunshotClip, position, 1.0f);
        }

        public void PlayBoltCycle(Vector3 position)
        {
            PlaySpatial(boltCycleClip, position, 0.7f);
        }

        public void PlayReload(Vector3 position)
        {
            PlaySpatial(reloadClip, position, 0.8f);
        }

        public void PlayHitConfirm()
        {
            if (sfxSource != null && hitConfirmClip != null)
            {
                sfxSource.PlayOneShot(hitConfirmClip, 0.85f);
            }
        }

        public void PlayEnemyAlert(Vector3 position)
        {
            PlaySpatial(enemyAlertClip, position, 0.75f);
        }

        public void PlayEnemyEliminated(Vector3 position)
        {
            PlaySpatial(enemyEliminatedClip, position, 0.8f);
        }

        public void PlayMissionStart()
        {
            if (sfxSource != null && missionStartClip != null)
                sfxSource.PlayOneShot(missionStartClip, 0.9f);
        }

        public void PlayMissionComplete()
        {
            if (sfxSource != null && missionCompleteClip != null)
                sfxSource.PlayOneShot(missionCompleteClip, 1.0f);
        }

        public void PlayMissionFail()
        {
            if (sfxSource != null && missionFailClip != null)
                sfxSource.PlayOneShot(missionFailClip, 1.0f);
        }

        public void PlayButtonClick()
        {
            if (sfxSource != null && boltCycleClip != null)
                sfxSource.PlayOneShot(boltCycleClip, 0.45f);
        }

        public void PlayRadioChatter()
        {
            if (sfxSource != null && missionStartClip != null)
                sfxSource.PlayOneShot(missionStartClip, 0.6f);
        }

        public void StartAmbientCity()
        {
            if (ambientSource != null && ambientCityClip != null)
            {
                ambientSource.clip = ambientCityClip;
                ambientSource.Play();
            }
        }

        public void StopAmbientCity()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        private void PlaySpatial(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        // Procedural Audio Generators
        private AudioClip SynthesizeGunshot()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 18.0f);
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float thud = Mathf.Sin(2f * Mathf.PI * (120f - t * 150f) * t) * 0.9f;
                data[i] = (noise + thud) * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_Gunshot", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeBoltCycle()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float click1 = (t > 0.05f && t < 0.12f) ? Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-(t - 0.05f) * 40f) : 0f;
                float click2 = (t > 0.22f && t < 0.30f) ? Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-(t - 0.22f) * 50f) : 0f;
                data[i] = (click1 + click2) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Procedural_BoltCycle", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeReload()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float magOut = (t > 0.05f && t < 0.18f) ? (Random.value * 2f - 1f) * Mathf.Exp(-(t - 0.05f) * 25f) : 0f;
                float magIn = (t > 0.30f && t < 0.45f) ? (Mathf.Sin(2f * Mathf.PI * 450f * t) + (Random.value * 2f - 1f) * 0.4f) * Mathf.Exp(-(t - 0.30f) * 30f) : 0f;
                data[i] = (magOut + magIn) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Reload", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeHitConfirm()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 28.0f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1480f * t) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Procedural_HitConfirm", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeAlert()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 650f + t * 400f;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Alert", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeEliminated()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 380f - t * 250f;
                float env = Mathf.Exp(-t * 10f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Eliminated", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeChime(float frequency, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 4.5f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create($"Procedural_Chime_{frequency}", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeAmbientCity()
        {
            int sampleRate = 44100;
            float duration = 4.0f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Low rumbling traffic filter + distant hum
                float traffic = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f;
                float wind = (Random.value * 2f - 1f) * 0.04f;
                data[i] = traffic + wind;
            }

            AudioClip clip = AudioClip.Create("Procedural_AmbientCity", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

