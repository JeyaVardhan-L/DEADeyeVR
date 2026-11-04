using System;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative manager for active weapon status, ammunition, and reloading.
    /// </summary>
    public class DEADeyeWeaponManager : MonoBehaviour
    {
        public static DEADeyeWeaponManager Instance { get; private set; }

        [Header("Ammunition")]
        [SerializeField] private int magazineCapacity = 5;
        [SerializeField] private int currentAmmo = 5;
        [SerializeField] private float reloadTime = 1.8f;
        [SerializeField] private bool isReloading = false;

        public int CurrentAmmo => currentAmmo;
        public int MagazineCapacity => magazineCapacity;
        public bool IsReloading => isReloading;

        public event Action<int, int> OnAmmoChanged; // (current, max)
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            currentAmmo = magazineCapacity;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanFire()
        {
            return currentAmmo > 0 && !isReloading;
        }

        public void ResetAmmo()
        {
            currentAmmo = magazineCapacity;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }

        public void ConsumeAmmo()
        {
            if (currentAmmo > 0)
            {
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
            }
        }

        public void Reload()
        {
            if (isReloading || currentAmmo >= magazineCapacity) return;
            StartCoroutine(ReloadRoutine());
        }

        public void StartReload()
        {
            Reload();
        }

        private System.Collections.IEnumerator ReloadRoutine()
        {
            isReloading = true;
            OnReloadStarted?.Invoke();

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayReload(transform.position);
            }

            float elapsed = 0f;
            while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
            }

            currentAmmo = magazineCapacity;
            isReloading = false;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }

        public void RefillInstantly()
        {
            currentAmmo = magazineCapacity;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }
    }
}

