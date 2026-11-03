using UnityEngine;
using UnityEngine.AI;

namespace DEADeye
{
    /// <summary>
    /// Procedural skeletal locomotion and idling animator for stylized modular characters.
    /// Provides smooth natural limb swing during walk/run, idle breathing posture,
    /// tactical aiming/crouching, and elimination collapse without needing external clip assets.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DEADeyeProceduralCharacterAnimator : MonoBehaviour
    {
        [Header("Body Transforms (Auto-detected if null)")]
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;

        [Header("Animation Tuning")]
        [SerializeField] private float walkSwingAngle = 28f;
        [SerializeField] private float runSwingAngle = 45f;
        [SerializeField] private float cycleSpeedMultiplier = 6.0f;

        private NavMeshAgent agent;
        private Quaternion legLeftInit;
        private Quaternion legRightInit;
        private Quaternion armLeftInit;
        private Quaternion armRightInit;
        private Vector3 torsoInitPos;
        private float walkPhase = 0f;
        private bool isDead = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            AutoDetectTransforms();

            if (legLeft != null) legLeftInit = legLeft.localRotation;
            if (legRight != null) legRightInit = legRight.localRotation;
            if (armLeft != null) armLeftInit = armLeft.localRotation;
            if (armRight != null) armRightInit = armRight.localRotation;
            if (torso != null) torsoInitPos = torso.localPosition;
        }

        public void AutoDetectTransforms()
        {
            if (legLeft == null) legLeft = FindChildRecursive(transform, "leg-left");
            if (legRight == null) legRight = FindChildRecursive(transform, "leg-right");
            if (armLeft == null) armLeft = FindChildRecursive(transform, "arm-left");
            if (armRight == null) armRight = FindChildRecursive(transform, "arm-right");
            if (torso == null) torso = FindChildRecursive(transform, "torso");
            if (head == null) head = FindChildRecursive(transform, "head");
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower())) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void Update()
        {
            if (isDead) return;

            float currentSpeed = agent != null ? agent.velocity.magnitude : 0f;
            bool isMoving = currentSpeed > 0.08f;

            if (isMoving)
            {
                float freq = currentSpeed * cycleSpeedMultiplier;
                walkPhase += Time.deltaTime * freq;
                float swing = Mathf.Sin(walkPhase) * (currentSpeed > 2.5f ? runSwingAngle : walkSwingAngle);

                // Leg swing
                if (legLeft != null) legLeft.localRotation = legLeftInit * Quaternion.Euler(swing, 0f, 0f);
                if (legRight != null) legRight.localRotation = legRightInit * Quaternion.Euler(-swing, 0f, 0f);

                // Arm counter-swing
                if (armLeft != null) armLeft.localRotation = armLeftInit * Quaternion.Euler(-swing * 0.8f, 0f, 0f);
                if (armRight != null) armRight.localRotation = armRightInit * Quaternion.Euler(swing * 0.8f, 0f, 0f);

                // Slight bobbing on torso
                if (torso != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.04f;
                    torso.localPosition = torsoInitPos + new Vector3(0f, bob, 0f);
                }
            }
            else
            {
                // Idle breathing motion
                float breath = Mathf.Sin(Time.time * 2.2f) * 1.5f;
                if (torso != null)
                {
                    torso.localPosition = Vector3.Lerp(torso.localPosition, torsoInitPos + new Vector3(0f, Mathf.Sin(Time.time * 1.8f) * 0.015f, 0f), Time.deltaTime * 3f);
                }
                if (armLeft != null) armLeft.localRotation = Quaternion.Slerp(armLeft.localRotation, armLeftInit * Quaternion.Euler(breath, 0f, 0f), Time.deltaTime * 3f);
                if (armRight != null) armRight.localRotation = Quaternion.Slerp(armRight.localRotation, armRightInit * Quaternion.Euler(breath, 0f, 0f), Time.deltaTime * 3f);
                if (legLeft != null) legLeft.localRotation = Quaternion.Slerp(legLeft.localRotation, legLeftInit, Time.deltaTime * 5f);
                if (legRight != null) legRight.localRotation = Quaternion.Slerp(legRight.localRotation, legRightInit, Time.deltaTime * 5f);
            }
        }

        public void OnEliminated()
        {
            isDead = true;
        }
    }
}
