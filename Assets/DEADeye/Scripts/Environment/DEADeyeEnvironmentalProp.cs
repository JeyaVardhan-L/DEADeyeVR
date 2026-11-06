using System;
using UnityEngine;

namespace DEADeye
{
    public enum EnvironmentalPropType
    {
        PowerJunction,          // Disabling powers down the hostile Jammer
        AlarmBell,              // Destructible alarm before guards can activate it
        DistractionTransformer, // Creates loud sparks/distraction, luring guards away
        SecurityGate            // Disabling opens the locked security alley gate
    }

    /// <summary>
    /// Interactive environmental opportunity that can be shot or triggered by the player.
    /// Provides tactical manipulation of the mission environment (Hitman / Sniper Ghost Warrior style).
    /// </summary>
    public class DEADeyeEnvironmentalProp : MonoBehaviour
    {
        [Header("Prop Configuration")]
        [SerializeField] private EnvironmentalPropType propType;
        [SerializeField] private string propTitle = "TACTICAL OPPORTUNITY";
        [SerializeField] private int hitPoints = 1;
        [SerializeField] private float distractionRadius = 32f;

        [Header("State")]
        [SerializeField] private bool isTriggered = false;

        public EnvironmentalPropType PropType => propType;
        public string PropTitle => propTitle;
        public bool IsTriggered => isTriggered;

        public event Action<DEADeyeEnvironmentalProp> OnPropTriggered;

        public void Initialize(EnvironmentalPropType type, string title)
        {
            propType = type;
            propTitle = title;
            hitPoints = 1;
            isTriggered = false;

            // Ensure Spotable component is attached for reconnaissance
            var spotable = GetComponent<DEADeyeSpotable>();
            if (spotable == null) spotable = gameObject.AddComponent<DEADeyeSpotable>();

            switch (type)
            {
                case EnvironmentalPropType.PowerJunction:
                    spotable.SetTargetInfo("POWER JUNCTION", SpotClassification.Environmental, "High-voltage breaker powering the hostile jammer array. Disable to cut power.");
                    break;
                case EnvironmentalPropType.AlarmBell:
                    spotable.SetTargetInfo("ALARM SYSTEM", SpotClassification.Environmental, "District emergency siren bell. Shoot to destroy before hostiles raise alert.");
                    break;
                case EnvironmentalPropType.DistractionTransformer:
                    spotable.SetTargetInfo("ELECTRICAL TRANSFORMER", SpotClassification.Environmental, "Volatile electrical transformer. Shoot to create loud distraction and pull guards.");
                    break;
                case EnvironmentalPropType.SecurityGate:
                    spotable.SetTargetInfo("GATE CONTROL PANEL", SpotClassification.Environmental, "Locked security gate junction box. Shoot to open alternate transit path.");
                    break;
            }
        }

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isTriggered) return;

            hitPoints -= damage;
            if (hitPoints <= 0)
            {
                TriggerOpportunity(hitPoint);
            }
        }

        private void TriggerOpportunity(Vector3 hitPoint)
        {
            if (isTriggered) return;
            isTriggered = true;

            OnPropTriggered?.Invoke(this);

            // Audio & Visual feedback
            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayEnvironmentalInteraction();
            }

            CreateImpactVisuals(hitPoint);

            // Dispatch prop-specific consequence
            switch (propType)
            {
                case EnvironmentalPropType.PowerJunction:
                    Debug.Log("[DEADeyeEnvironmentalProp] POWER JUNCTION DESTROYED -> Hostile Jammer shut down!");
                    if (DEADeyeMissionDirector.Instance != null)
                    {
                        DEADeyeMissionDirector.Instance.HandlePowerJunctionDisabled();
                    }
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue("CONTROL", "Power junction destroyed! The jammer signal is fading!");
                    }
                    break;

                case EnvironmentalPropType.AlarmBell:
                    Debug.Log("[DEADeyeEnvironmentalProp] ALARM BELL SILENCED -> Prevented district-wide siren!");
                    if (DEADeyeMissionDirector.Instance != null)
                    {
                        DEADeyeMissionDirector.Instance.HandleAlarmSilenced();
                    }
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue("CONTROL", "Alarm bell neutralized. Reinforcement siren offline.");
                    }
                    break;

                case EnvironmentalPropType.DistractionTransformer:
                    Debug.Log("[DEADeyeEnvironmentalProp] TRANSFORMER EXPLODED -> Distracting nearby hostiles!");
                    LureNearbyHostiles(transform.position, distractionRadius);
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue("ELENA", "Transformer blew! The plaza guards are turning to check it out!");
                    }
                    break;

                case EnvironmentalPropType.SecurityGate:
                    Debug.Log("[DEADeyeEnvironmentalProp] SECURITY GATE OPENED -> Path unlocked for technicians!");
                    OpenGateAnimation();
                    if (DEADeyeMissionDirector.Instance != null)
                    {
                        DEADeyeMissionDirector.Instance.HandleSecurityGateOpened();
                    }
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue("MARCUS", "Gate's open! We're taking the clear alleyway!");
                    }
                    break;
            }

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.AddScore(150, isHeadshot: false);
            }
        }

        private void CreateImpactVisuals(Vector3 hitPoint)
        {
            // Create electrical spark / flash burst
            GameObject sparkGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkGO.name = "SparkBurst";
            sparkGO.transform.position = hitPoint != Vector3.zero ? hitPoint : transform.position;
            sparkGO.transform.localScale = Vector3.one * 0.45f;

            var rend = sparkGO.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) unlit = Shader.Find("Unlit/Color");
                Material sparkMat = new Material(unlit) { color = new Color(0.2f, 0.85f, 1.0f) };
                rend.material = sparkMat;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DestroyImmediate(sparkGO.GetComponent<Collider>());
                DestroyImmediate(sparkGO);
                return;
            }
#endif
            Destroy(sparkGO.GetComponent<Collider>());
            Destroy(sparkGO, 0.6f);
        }

        private void LureNearbyHostiles(Vector3 epicenter, float radius)
        {
            DEADeyeHostileAI[] allHostiles = FindObjectsByType<DEADeyeHostileAI>(FindObjectsSortMode.None);
            foreach (var h in allHostiles)
            {
                if (h != null && !h.IsEliminated)
                {
                    float dist = Vector3.Distance(h.transform.position, epicenter);
                    if (dist <= radius)
                    {
                        h.InvestigateLocation(epicenter);
                    }
                }
            }
        }

        private void OpenGateAnimation()
        {
            // Lift gate geometry up 4 meters
            Transform gateArm = transform.Find("GateArm");
            if (gateArm != null)
            {
                gateArm.localPosition += new Vector3(0f, 3.5f, 0f);
            }
        }
    }
}

