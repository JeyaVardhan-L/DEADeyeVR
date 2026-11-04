using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative audio manager for DEADeye VR.
    /// Provides spatial sound effects, procedural audio synthesis fallbacks, and ambient city sound.
    /// Features:
    /// - High-caliber sniper rifle gunshot (supersonic crack + sub-bass punch + urban reverb tail).
    /// - Scope optic toggle mechanical click.
    /// - Dry fire empty click.
    /// - Bolt cycling and magazine reloading.
    /// - Surface-differentiated bullet impacts (concrete, metal, body).
    /// - Ambient city audio bed.
    /// </summary>
    public class DEADeyeAudioManager : MonoBehaviour
    {
        public static DEADeyeAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Audio Clips (Procedural fallback if unassigned)")]
        [SerializeField] private AudioClip gunshotClip;
        [SerializeField] private AudioClip boltCycleClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip hitConfirmClip;
        [SerializeField] private AudioClip headshotConfirmClip;
        [SerializeField] private AudioClip scopeToggleClip;
        [SerializeField] private AudioClip dryFireClip;
        [SerializeField] private AudioClip concreteImpactClip;
        [SerializeField] private AudioClip metalImpactClip;
        [SerializeField] private AudioClip fleshImpactClip;
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
            if (gunshotClip == null) gunshotClip = SynthesizeSniperGunshot();
            if (boltCycleClip == null) boltCycleClip = SynthesizeBoltCycle();
            if (reloadClip == null) reloadClip = SynthesizeReload();
            if (hitConfirmClip == null) hitConfirmClip = SynthesizeHitConfirm();
            if (headshotConfirmClip == null) headshotConfirmClip = SynthesizeHeadshotConfirm();
            if (scopeToggleClip == null) scopeToggleClip = SynthesizeScopeClick();
            if (dryFireClip == null) dryFireClip = SynthesizeDryClick();
            if (concreteImpactClip == null) concreteImpactClip = SynthesizeConcreteImpact();
            if (metalImpactClip == null) metalImpactClip = SynthesizeMetalImpact();
            if (fleshImpactClip == null) fleshImpactClip = SynthesizeFleshImpact();
            if (enemyAlertClip == null) enemyAlertClip = SynthesizeAlert();
            if (enemyEliminatedClip == null) enemyEliminatedClip = SynthesizeEliminated();
            if (missionStartClip == null) missionStartClip = SynthesizeChime(523.25f, 0.4f);
            if (missionCompleteClip == null) missionCompleteClip = SynthesizeChime(659.25f, 0.6f);
            if (missionFailClip == null) missionFailClip = SynthesizeChime(311.13f, 0.8f);
            if (ambientCityClip == null) ambientCityClip = SynthesizeAmbientCity();
        }

        public void PlayGunshot(Vector3 position)
        {
            PlaySpatial(gunshotClip, position, 1.0f);
        }

        public void PlayBoltCycle(Vector3 position)
        {
            PlaySpatial(boltCycleClip, position, 0.75f);
        }

        public void PlayReload(Vector3 position)
        {
            PlaySpatial(reloadClip, position, 0.85f);
        }

        public void PlayScopeToggle(bool entering)
        {
            if (sfxSource != null && scopeToggleClip != null)
            {
                sfxSource.pitch = entering ? 1.05f : 0.95f;
                sfxSource.PlayOneShot(scopeToggleClip, 0.65f);
                sfxSource.pitch = 1.0f;
            }
        }

        public void PlayDryFire()
        {
            if (sfxSource != null && dryFireClip != null)
            {
                sfxSource.PlayOneShot(dryFireClip, 0.6f);
            }
        }

        public void PlayConcreteImpact(Vector3 position)
        {
            PlaySpatial(concreteImpactClip, position, 0.85f);
        }

        public void PlayMetalImpact(Vector3 position)
        {
            PlaySpatial(metalImpactClip, position, 0.9f);
        }

        public void PlayFleshImpact(Vector3 position)
        {
            PlaySpatial(fleshImpactClip, position, 0.95f);
        }

        public void PlayHitConfirm()
        {
            if (sfxSource != null && hitConfirmClip != null)
            {
                sfxSource.PlayOneShot(hitConfirmClip, 0.85f);
            }
        }

        public void PlayHeadshotConfirm()
        {
            if (sfxSource != null && headshotConfirmClip != null)
            {
                sfxSource.PlayOneShot(headshotConfirmClip, 0.95f);
            }
        }

        public void PlayBodyHit()
        {
            PlayHitConfirm();
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
            if (sfxSource != null && scopeToggleClip != null)
                sfxSource.PlayOneShot(scopeToggleClip, 0.45f);
            else if (sfxSource != null && boltCycleClip != null)
                sfxSource.PlayOneShot(boltCycleClip, 0.45f);
        }

        public void PlaySpottingChirp()
        {
            if (sfxSource != null)
            {
                if (!proceduralCache.TryGetValue("SpottingChirp", out AudioClip clip))
                {
                    clip = SynthesizeChime(1850f, 0.15f);
                    proceduralCache["SpottingChirp"] = clip;
                }
                sfxSource.PlayOneShot(clip, 0.8f);
            }
        }

        public void PlayEnvironmentalInteraction()
        {
            if (sfxSource != null)
            {
                if (!proceduralCache.TryGetValue("EnvInteraction", out AudioClip clip))
                {
                    clip = SynthesizeChime(880f, 0.3f);
                    proceduralCache["EnvInteraction"] = clip;
                }
                sfxSource.PlayOneShot(clip, 0.9f);
            }
        }

        public void PlayRadioChatter()
        {
            if (sfxSource != null && missionStartClip != null)
                sfxSource.PlayOneShot(missionStartClip, 0.6f);
        }

        public void PlayObjectiveUpdate()
        {
            if (sfxSource != null)
            {
                if (!proceduralCache.TryGetValue("ObjectiveUpdate", out AudioClip clip))
                {
                    clip = SynthesizeChime(1046.5f, 0.35f);
                    proceduralCache["ObjectiveUpdate"] = clip;
                }
                sfxSource.PlayOneShot(clip, 0.9f);
            }
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

        // =========================================================================
        // PROCEDURAL AUDIO SYNTHESIZERS
        // =========================================================================

        /// <summary>
        /// High-power .50 cal / high-caliber sniper rifle gunshot:
        /// 1. Supersonic crack (high-frequency burst).
        /// 2. Concussive sub-bass body thump (55Hz pitch-swept sine wave).
        /// 3. Urban architectural echo tail (1.2s decaying reflections).
        /// </summary>
        private AudioClip SynthesizeSniperGunshot()
        {
            int sampleRate = 44100;
            float duration = 1.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Initial Supersonic Crack (0 - 35ms)
                float crack = 0f;
                if (t < 0.045f)
                {
                    float crackEnv = Mathf.Exp(-t * 90f);
                    float hfNoise = (Random.value * 2f - 1f);
                    crack = hfNoise * crackEnv * 0.95f;
                }

                // 2. Sub-bass concussive punch (0 - 180ms)
                float bassThump = 0f;
                if (t < 0.22f)
                {
                    float bassEnv = Mathf.Exp(-t * 22f);
                    float bassFreq = Mathf.Lerp(65f, 32f, t / 0.22f);
                    bassThump = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 1.0f;
                }

                // 3. Mid-range explosion body (0 - 350ms)
                float midBody = 0f;
                if (t < 0.40f)
                {
                    float midEnv = Mathf.Exp(-t * 14f);
                    float noise = (Random.value * 2f - 1f) * 0.45f;
                    midBody = noise * midEnv;
                }

                // 4. Urban architectural echo / canyon reverberation tail (0.12s - 1.35s)
                float echoTail = 0f;
                if (t >= 0.08f)
                {
                    float echoEnv = Mathf.Exp(-(t - 0.08f) * 3.8f);
                    // Multiple reflection taps simulating surrounding building walls
                    float tap1 = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-Mathf.Abs(t - 0.14f) * 45f) * 0.35f;
                    float tap2 = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-Mathf.Abs(t - 0.28f) * 35f) * 0.28f;
                    float tap3 = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-Mathf.Abs(t - 0.48f) * 25f) * 0.22f;
                    float diffusedNoise = (Random.value * 2f - 1f) * 0.18f;
                    echoTail = (tap1 + tap2 + tap3 + diffusedNoise) * echoEnv;
                }

                float sample = crack + bassThump + midBody + echoTail;
                data[i] = Mathf.Clamp(sample, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_HighCaliberSniper", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeBoltCycle()
        {
            int sampleRate = 44100;
            float duration = 0.42f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Stage 1: Bolt handle up & unlock (t ~ 0.04s)
                float click1 = (t > 0.03f && t < 0.10f) ? Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-(t - 0.03f) * 65f) : 0f;
                // Stage 2: Chamber slide back & brass eject (t ~ 0.18s)
                float slide = (t > 0.15f && t < 0.24f) ? ((Random.value * 2f - 1f) * 0.3f + Mathf.Sin(2f * Mathf.PI * 650f * t)) * Mathf.Exp(-(t - 0.15f) * 40f) : 0f;
                // Stage 3: Bolt slide forward & lock down (t ~ 0.32s)
                float click2 = (t > 0.30f && t < 0.38f) ? Mathf.Sin(2f * Mathf.PI * 1250f * t) * Mathf.Exp(-(t - 0.30f) * 75f) : 0f;

                data[i] = Mathf.Clamp((click1 + slide + click2) * 0.75f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_BoltCycle", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeReload()
        {
            int sampleRate = 44100;
            float duration = 1.25f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // 1. Mag release click (t ~ 0.15s)
                float magOut = (t > 0.12f && t < 0.25f) ? Mathf.Sin(2f * Mathf.PI * 550f * t) * Mathf.Exp(-(t - 0.12f) * 35f) : 0f;
                // 2. Mag insert slap (t ~ 0.65s)
                float magIn = (t > 0.62f && t < 0.80f) ? (Mathf.Sin(2f * Mathf.PI * 280f * t) * 0.8f + (Random.value * 2f - 1f) * 0.4f) * Mathf.Exp(-(t - 0.62f) * 30f) : 0f;
                // 3. Bolt chamber snap (t ~ 1.05s)
                float boltSnap = (t > 1.02f && t < 1.15f) ? Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-(t - 1.02f) * 60f) : 0f;

                data[i] = Mathf.Clamp((magOut + magIn + boltSnap) * 0.8f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_Reload", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeScopeClick()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float click = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t * 90f);
                data[i] = Mathf.Clamp(click * 0.6f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_ScopeClick", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeDryClick()
        {
            int sampleRate = 44100;
            float duration = 0.07f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float click = Mathf.Sin(2f * Mathf.PI * 1450f * t) * Mathf.Exp(-t * 110f);
                data[i] = Mathf.Clamp(click * 0.7f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_DryClick", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeConcreteImpact()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Crack + rock dust ricochet
                float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 35f);
                float ricochet = Mathf.Sin(2f * Mathf.PI * (1600f - t * 3000f) * t) * Mathf.Exp(-t * 20f) * 0.3f;
                data[i] = Mathf.Clamp((crack + ricochet) * 0.85f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_ConcreteImpact", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeMetalImpact()
        {
            int sampleRate = 44100;
            float duration = 0.40f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Resonant metallic ping (3200Hz ringing down)
                float ping = Mathf.Sin(2f * Mathf.PI * 3150f * t) * Mathf.Exp(-t * 15f) * 0.65f;
                float chime = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 20f) * 0.35f;
                data[i] = Mathf.Clamp(ping + chime, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_MetalImpact", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeFleshImpact()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Heavy dull organic thud (140Hz with fast dampening)
                float thud = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 38f) * 0.8f;
                float pop = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f) * 0.4f;
                data[i] = Mathf.Clamp(thud + pop, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_FleshImpact", totalSamples, 1, sampleRate, false);
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
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 30f) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Procedural_HitConfirm", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeHeadshotConfirm()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float tone1 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * Mathf.Exp(-t * 18f); // C6
                float tone2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * Mathf.Exp(-t * 22f); // E6
                data[i] = Mathf.Clamp((tone1 + tone2) * 0.5f, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("Procedural_HeadshotConfirm", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeAlert()
        {
            return SynthesizeChime(440f, 0.25f);
        }

        private AudioClip SynthesizeEliminated()
        {
            return SynthesizeChime(330f, 0.35f);
        }

        private AudioClip SynthesizeChime(float frequency, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * (5f / duration));
            }

            AudioClip clip = AudioClip.Create($"Procedural_Chime_{frequency}", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeAmbientCity()
        {
            int sampleRate = 22050;
            float duration = 3.0f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float lowDrone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.08f;
                float windNoise = (Random.value * 2f - 1f) * 0.04f;
                data[i] = lowDrone + windNoise;
            }

            AudioClip clip = AudioClip.Create("Procedural_AmbientCity", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
