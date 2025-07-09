using System.Collections;
using EnemyScripts;
using UiScripts;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GunScripts
{
    public class Gun : MonoBehaviour
    {
        public bool isActive = false;
        public bool isAutomatic = false;
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 15f;
        public int bulletAmount = 32;
        public int magCapacity = 8;
        public int bulletsLeft = 8;
        public float reloadTime = 1.5f;
        public Vector3 reloadOffset = new Vector3(0, -0.5f, -0.3f); // kierunek "schowania"
        public float recoilDistance = 0.1f;
        public float recoilSpeed = 10f;
        public Vector3 originalPosition;
        public float spread = 0.01f;
        public float spreadResetTime = 0.3f;
        public float maxSpreadResetTime = 3f;

        private bool isReloading;
        private Vector3 targetRecoilPosition;
        private bool isRecoiling;
        private float currentSpreadTime = 0;
        private float currentSpread = 0;
        
        public float dropCooldown = 1.0f;
        public float lastDropTime = -10f;
        
        public Transform weaponTransform;
        public Rigidbody weaponRigidbody;
        public Camera fpsCam;
        public ParticleSystem muzzleFlash;
        public GameObject bulletHolePrefab;

        private MouseLook mouseLook;
        
        private float nextTimeToFire = 0f;
        
        public void Initialize(Camera cam, Vector3 newPosition)
        {
            weaponRigidbody.isKinematic = true;
            fpsCam = cam;
            mouseLook = fpsCam.GetComponent<MouseLook>();
            
            if (mouseLook == null)
                mouseLook = fpsCam.GetComponentInParent<MouseLook>();
            
            isActive = true;
            originalPosition = newPosition;
            isRecoiling = false;
            UiManager.Instance.UpdateAmmoUI(bulletsLeft, bulletAmount);
        }
        
        private void Update()
        {
            currentSpreadTime -= Time.deltaTime;
            if (currentSpreadTime > 0)
            {
                currentSpreadTime -= Time.deltaTime;
                currentSpread = Mathf.Lerp(currentSpread, 0, (Time.deltaTime / spreadResetTime) / 8);
            }

            if (currentSpreadTime < 0)
            {
                currentSpreadTime = 0;
                currentSpread = 0;
            }
            
            
            if (isActive)
            {
                if (isAutomatic == true)
                {
                    if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire && !isReloading)
                    {
                        nextTimeToFire = Time.time + 1f / fireRate;
                        Shoot();
                    }
                }
                else
                {
                    if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire && !isReloading)
                    {
                        nextTimeToFire = Time.time + 1f / fireRate;
                        Shoot();
                    }
                }
                // przeladowanie
                if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magCapacity && bulletAmount > 0 && !isReloading)
                {
                    Reload();
                    UiManager.Instance.UpdateAmmoUI(bulletsLeft, bulletAmount);
                } 
            }
            
            if (mouseLook != null)
            {
                // Im większy currentSpreadTime, tym mniejszy recoilReturnSpeed
                float t = Mathf.InverseLerp(0f, maxSpreadResetTime, currentSpreadTime);
                float recoilReturn = Mathf.Lerp(15f, 3f, t); // szybki powrót przy 0, wolny przy max
                mouseLook.SetRecoilRecoverySpeed(recoilReturn);
            }
        }
        
        private void Shoot()
        {
            if (bulletsLeft > 0)
            {
                bulletsLeft--;
                muzzleFlash.Play();
                if (mouseLook != null)
                {
                    mouseLook.AddRecoil(Random.Range(1f, 2f), Random.Range(-0.5f, 0.5f));
                }
                TriggerRecoil();
                RaycastHit hit;
                
                Vector3 direction = fpsCam.transform.forward +
                                    fpsCam.transform.right * Random.Range(-currentSpread / 4f, currentSpread / 4f) +
                                    fpsCam.transform.up * Random.Range(currentSpread / 3f, currentSpread);

                direction.Normalize();
                
                if (currentSpreadTime < maxSpreadResetTime)
                {
                    currentSpreadTime += spreadResetTime;
                    currentSpread += spread;
                }
                                            
                if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range))
                {
                    Hitbox hitbox = hit.transform.GetComponent<Hitbox>();
                    if (hitbox != null)
                    {
                        hitbox.Hit(damage);
                    }
                    
                    Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
                    GameObject bulletHole = Instantiate(bulletHolePrefab,
                        hit.point + hit.normal * 0.001f, // lekko wysunięte, żeby nie zniknęło w obiekcie
                        hitRotation
                    );
                    bulletHole.transform.SetParent(hit.transform);
                }
            }
            UiManager.Instance.UpdateAmmoUI(bulletsLeft, bulletAmount);
        }

        private void Reload()
        {
            isReloading = true;
            int bulletsNeeded = magCapacity - bulletsLeft;
            if (bulletsNeeded > bulletAmount)
            {
                bulletsLeft = bulletAmount;
                bulletAmount = 0;
            }
            else
            {
                bulletsLeft += bulletsNeeded;
                bulletAmount -= bulletsNeeded;
            }

            StartCoroutine(ReloadAnimation());
        }

        private IEnumerator ReloadAnimation()
        {
            Vector3 startPos = weaponTransform.localPosition;
            Vector3 hiddenPos = startPos + reloadOffset;

            // Animacja schowania
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                weaponTransform.localPosition = Vector3.Lerp(startPos, hiddenPos, t);
                yield return null;
            }

            // Czekanie (symulacja ładowania)
            yield return new WaitForSeconds(reloadTime);

            // Powrót broni
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                weaponTransform.localPosition = Vector3.Lerp(hiddenPos, startPos, t);
                yield return null;
            }
            isReloading = false;
        }

        public IEnumerator PickupAnimation()
        {
            isReloading = true;
            Vector3 startPos = weaponTransform.localPosition;
            float t = 0f;
            
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                weaponTransform.localPosition = Vector3.Lerp(startPos, startPos - reloadOffset, t);
                yield return null;
            }
            isReloading = false;
        }
        
        
        
        private void TriggerRecoil()
        {
            if (!isRecoiling)
                StartCoroutine(DoRecoil());
        }

        private IEnumerator DoRecoil()
        {
            isRecoiling = true;

            // Cofnij broń
            targetRecoilPosition = originalPosition + new Vector3(0, 0, -recoilDistance);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * recoilSpeed;
                weaponTransform.localPosition = Vector3.Lerp(originalPosition, targetRecoilPosition, t);
                yield return null;
            }

            // Wracaj do oryginalnej pozycji
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * recoilSpeed;
                weaponTransform.localPosition = Vector3.Lerp(targetRecoilPosition, originalPosition, t);
                yield return null;
            }

            isRecoiling = false;
        }

        public void ResetLastDropTime()
        {
            lastDropTime = Time.time;
        }

        public bool IsPickable()
        {
            if (Time.time - lastDropTime < dropCooldown) return false;
            return true;
        }
    }
}