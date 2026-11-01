using System;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative scoring and statistics manager for DEADeye VR missions.
    /// Tracks shots, hits, headshots, eliminations, escapes, and calculates accuracy and final scores.
    /// </summary>
    public class DEADeyeScoreManager : MonoBehaviour
    {
        public static DEADeyeScoreManager Instance { get; private set; }

        [Header("Live Metrics")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int shotsFired = 0;
        [SerializeField] private int shotsHit = 0;
        [SerializeField] private int headshots = 0;
        [SerializeField] private int enemiesEliminated = 0;
        [SerializeField] private int enemiesEscaped = 0;
        [SerializeField] private int currentCombo = 0;

        public int CurrentScore => currentScore;
        public int ShotsFired => shotsFired;
        public int ShotsHit => shotsHit;
        public int Headshots => headshots;
        public int EnemiesEliminated => enemiesEliminated;
        public int EnemiesEscaped => enemiesEscaped;
        public int CurrentCombo => currentCombo;

        public float AccuracyPercentage => shotsFired > 0 ? ((float)shotsHit / shotsFired) * 100f : 0f;

        public event Action<int> OnScoreUpdated;
        public event Action<int, int> OnHostileCountUpdated; // (eliminated, escaped)
        public event Action<float> OnAccuracyUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetMetrics()
        {
            currentScore = 0;
            shotsFired = 0;
            shotsHit = 0;
            headshots = 0;
            enemiesEliminated = 0;
            enemiesEscaped = 0;
            currentCombo = 0;

            OnScoreUpdated?.Invoke(currentScore);
            OnHostileCountUpdated?.Invoke(enemiesEliminated, enemiesEscaped);
            OnAccuracyUpdated?.Invoke(0f);
        }

        public void RegisterShotFired()
        {
            shotsFired++;
            OnAccuracyUpdated?.Invoke(AccuracyPercentage);
        }

        public void RegisterHit(bool isHeadshot, int points = 100)
        {
            shotsHit++;
            currentCombo++;

            int finalPoints = points;
            if (isHeadshot)
            {
                headshots++;
                finalPoints = Mathf.RoundToInt(points * 2.5f);
            }

            // Apply combo multiplier bonus
            int comboBonus = Mathf.Min(currentCombo, 5) * 20;
            currentScore += (finalPoints + comboBonus);

            OnScoreUpdated?.Invoke(currentScore);
            OnAccuracyUpdated?.Invoke(AccuracyPercentage);
        }

        public void RegisterMiss()
        {
            currentCombo = 0;
        }

        public void RegisterElimination()
        {
            enemiesEliminated++;
            OnHostileCountUpdated?.Invoke(enemiesEliminated, enemiesEscaped);
        }

        public void RegisterEscape()
        {
            enemiesEscaped++;
            currentScore = Mathf.Max(0, currentScore - 150); // Penalty for escape
            currentCombo = 0;
            OnScoreUpdated?.Invoke(currentScore);
            OnHostileCountUpdated?.Invoke(enemiesEliminated, enemiesEscaped);
        }
    }
}

