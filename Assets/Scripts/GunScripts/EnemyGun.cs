using System.Collections;
using UnityEngine;

namespace GunScripts
{
    public class EnemyGun : MonoBehaviour
    {
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 15f;

        public Transform enemyCamera;
        public ParticleSystem muzzleFlash;
        public GameObject bulletHolePrefab;

        public void Shoot()
        {
            if (muzzleFlash != null)
                muzzleFlash.Play();

            Vector3 direction = enemyCamera.forward + 
                                enemyCamera.right * Random.Range(-0.1f, 0.1f) +
                                enemyCamera.up * Random.Range(-0.1f, 0.1f);;

            RaycastHit hit;
            Vector3 shootOrigin = enemyCamera.position;

            if (Physics.Raycast(shootOrigin, direction, out hit, range))
            {
                PlayerMovement player = hit.transform.GetComponent<PlayerMovement>();
                if (bulletHolePrefab != null && player == null)
                {
                    Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
                    GameObject bulletHole = Instantiate(
                        bulletHolePrefab,
                        hit.point + hit.normal * 0.001f,
                        hitRotation
                    );
                    bulletHole.transform.SetParent(hit.transform);
                }
                
                else if (player != null)
                {
                    GameManager.Instance.TakeDamage(10);
                }
            }
        }
    }
}