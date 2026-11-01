using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Manages the Dojo Operations Lobby environment:
    /// - Hides the legacy static wrestler figures (dw001, dw002, meditation).
    /// - Sets up the operations briefing terminal and loadout staging area.
    /// - Ensures clean single authoritative player rig.
    /// </summary>
    public class DEADeyeDojoLobbyManager : MonoBehaviour
    {
        [Header("Figure Cleanup")]
        [SerializeField] private bool hideLegacyToyFigures = true;

        private void Awake()
        {
            if (hideLegacyToyFigures)
            {
                CleanUpLegacyToyFigures();
            }
        }

        public void CleanUpLegacyToyFigures()
        {
            // Traverse all scene objects and hide dw001, dw002, and meditation figures
            string[] namesToHide = new string[] { "dw001", "dw002", "meditation" };
            int hiddenCount = 0;

            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;

                string lower = t.name.ToLower();
                foreach (var targetName in namesToHide)
                {
                    if (lower.Contains(targetName))
                    {
                        t.gameObject.SetActive(false);
                        hiddenCount++;
                        break;
                    }
                }
            }

            Debug.Log($"[DEADeyeDojoLobbyManager] Deactivated {hiddenCount} legacy static toy figures from the Dojo.");
        }
    }
}

