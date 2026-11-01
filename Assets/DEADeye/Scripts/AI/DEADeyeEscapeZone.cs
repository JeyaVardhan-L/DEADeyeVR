using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Designated escape zone (e.g. getaway van, subway stairs, sewer grate).
    /// Triggers hostile escape when reached.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DEADeyeEscapeZone : MonoBehaviour
    {
        [Header("Zone Identification")]
        [SerializeField] private string zoneName = "Alleyway Extraction Van";

        public string ZoneName => zoneName;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            DEADeyeHostileAI hostile = other.GetComponentInParent<DEADeyeHostileAI>();
            if (hostile != null && hostile.CurrentStateType != HostileStateType.Eliminated)
            {
                hostile.OnReachedEscapeZone(this);
            }
        }
    }
}

