using UnityEngine;
using GunScripts;
using Other;
using UiScripts;

namespace PlayerScripts
{
    public class PickupGun : MonoBehaviour
    {
        public float pickupRange = 1.5f;
        public Camera fpsCam;
        public Transform weaponHolder;
        
        private float dropForwardForce = 4f;
        private float dropUpwardForce = 2f;
        
        private void Update()
        {
            if (!IsGunEquiped())
            {
                PickupOnEnter();
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                UseObject();
            }

            if (Input.GetKeyDown(KeyCode.G) && IsGunEquiped())
            {
                Drop();
            }
        }

        private void UseObject()
        {
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, pickupRange))
            {
                Gun gun = hit.collider.GetComponentInParent<Gun>();
                if (gun != null)
                {
                    if (IsGunEquiped())
                    {
                        Drop();
                    }
                    AssignWeapon(gun);
                    return;
                }
                
                Key key = hit.collider.GetComponentInParent<Key>();
                if (key != null)
                {
                    GameManager.Instance.PickupKey();
                    Destroy(key.gameObject);
                    return;
                }

                Doors doors = hit.collider.GetComponentInParent<Doors>();
                if (doors != null)
                {
                    GameManager.Instance.OpenDoors();
                    return;
                }

                AmmoBox ammoBox = hit.collider.GetComponentInParent<AmmoBox>();
                if (ammoBox != null)
                {
                    if (IsGunEquiped())
                    {
                        Gun playerGun = weaponHolder.GetChild(0).GetComponent<Gun>();
                        playerGun.bulletAmount += 20;
                        Destroy(ammoBox.gameObject);
                        UiManager.Instance.UpdateAmmoUI(playerGun.bulletsLeft, playerGun.bulletAmount);
                        return;
                    }
                }
            }
        }

        private void PickupOnEnter()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRange);
            foreach (var collider in colliders)
            {
                Gun gun = collider.GetComponentInParent<Gun>();
                if (gun != null && gun.IsPickable())
                {
                    AssignWeapon(gun);
                    break;
                }
            }
        }

        private void Drop()
        {
            Gun gun = weaponHolder.GetChild(0).GetComponent<Gun>();
            gun.ResetLastDropTime();
            gun.isActive = false;
            gun.weaponRigidbody.isKinematic = false;
            gun.weaponRigidbody.linearVelocity = GameManager.Instance.playerController.velocity;
            gun.weaponRigidbody.AddForce(fpsCam.transform.forward * dropForwardForce, ForceMode.Impulse);
            gun.weaponRigidbody.AddForce(fpsCam.transform.up * dropUpwardForce, ForceMode.Impulse);
            float random = Random.Range(-1f, 1f);
            gun.weaponRigidbody.AddTorque(new Vector3(random, random, random) * 10);
            gun.transform.SetParent(null);
        }
        
        private bool IsGunEquiped()
        {
            if (weaponHolder.childCount == 1)
            {
                return true;
            }

            return false;
        }

        public void AssignWeapon(Gun gun)
        {
            gun.Initialize(fpsCam, Vector3.zero);
            gun.transform.SetParent(weaponHolder);
            gun.transform.localPosition = Vector3.zero + gun.reloadOffset;
            gun.transform.localRotation = Quaternion.identity;
            gun.StartCoroutine(gun.PickupAnimation());
        }

        public void DropWeapon()
        {
            Gun gun = weaponHolder.GetChild(0).GetComponent<Gun>();
            gun.isActive = false;
            gun.weaponRigidbody.isKinematic = false;
            gun.weaponRigidbody.linearVelocity = GameManager.Instance.playerController.velocity;
            gun.weaponRigidbody.AddForce(fpsCam.transform.forward * dropForwardForce, ForceMode.Impulse);
            gun.weaponRigidbody.AddForce(fpsCam.transform.up * dropUpwardForce, ForceMode.Impulse);
            float random = Random.Range(-1f, 1f);
            gun.weaponRigidbody.AddTorque(new Vector3(random, random, random) * 10);
            gun.transform.SetParent(null);
        }
    }
}