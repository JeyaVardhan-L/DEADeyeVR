using System;
using UnityEngine;

namespace DEADeye
{
    public enum SpotClassification
    {
        Hostile,
        HostileCommander,
        Friendly,
        FriendlyVIP,
        Civilian,
        Objective,
        Environmental
    }

    /// <summary>
    /// Attach to characters, devices, and environmental opportunities to enable
    /// precision overwatch reconnaissance and target identification.
    /// </summary>
    public class DEADeyeSpotable : MonoBehaviour
    {
        [Header("Tactical Recon Data")]
        [SerializeField] private string targetName = "UNKNOWN TARGET";
        [SerializeField] private SpotClassification classification = SpotClassification.Hostile;
        [TextArea(2, 3)]
        [SerializeField] private string tacticalDescription = "No tactical intelligence available.";
        [SerializeField] private int reconIntelValue = 50;

        [Header("State")]
        [SerializeField] private bool isIdentified = false;

        public string TargetName => targetName;
        public SpotClassification Classification => classification;
        public string TacticalDescription => tacticalDescription;
        public int ReconIntelValue => reconIntelValue;
        public bool IsIdentified => isIdentified;

        public event Action<DEADeyeSpotable> OnTargetIdentified;

        public void SetTargetInfo(string name, SpotClassification classType, string desc, int intelScore = 50)
        {
            targetName = name;
            classification = classType;
            tacticalDescription = desc;
            reconIntelValue = intelScore;
        }

        public void MarkIdentified()
        {
            if (isIdentified) return;
            isIdentified = true;

            OnTargetIdentified?.Invoke(this);

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.AddScore(reconIntelValue, isHeadshot: false);
            }
        }
    }
}
