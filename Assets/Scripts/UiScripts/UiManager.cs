using GunScripts;
using Other;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UiScripts
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;
        public Camera fpsCam;
        
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI utilityInfo;
        public TextMeshProUGUI hpInfo;
        public GameObject pauseMenu;
        public GameObject endingScreen;
        public GameObject deathMenu;

        private float range = 5f;
        
        private Gun playerGun; 
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            
            pauseMenu.SetActive(false);
            endingScreen.SetActive(false);
            deathMenu.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance.weaponHolder.childCount > 0)
            {
                playerGun = GameManager.Instance.weaponHolder.GetChild(0).GetComponent<Gun>();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Pause();
            }
            
            OnCameraLook();
        }

        public void UpdateAmmoUI(int bulletsLeft, int bulletAmount)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{bulletsLeft} / {bulletAmount}";
            }
        }

        public void UpdateHp()
        {
            if (GameManager.Instance.playerHp <= 0)
            {
                hpInfo.text ="0 hp";
                return;
            }
            hpInfo.text = GameManager.Instance.playerHp + " hp";
        }

        private void OnCameraLook()
        {
            if (fpsCam == null) return;
            
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                Gun gun = hit.collider.GetComponentInParent<Gun>();
                if (gun != null)
                {
                    utilityInfo.text = "Press [E] to pickup a Gun"; 
                    return;
                }
                
                if (hit.collider.GetComponentInParent<Key>() != null)
                {
                    utilityInfo.text = "Press [E] to pickup a Key";
                    return;
                }
                
                if (hit.collider.GetComponentInParent<Doors>() != null)
                {
                    int keyAmount = GameManager.Instance.GetKeyAmount();
                    
                    utilityInfo.text = keyAmount < 3 ? $"You need {3 - keyAmount} more Key(s) to escape" 
                        : "Press [E] to escape";
                    
                    return;
                }
                
                if (hit.collider.GetComponentInParent<AmmoBox>() != null)
                {
                    utilityInfo.text = GameManager.Instance.weaponHolder.childCount == 0
                        ? "You don't have any gun"
                        : "Press [E] to pickup an Ammo Box";
                    
                    return;
                }
            }

            utilityInfo.text = "";
        }

        private void Pause()
        {
            if (playerGun != null)
            {
                playerGun.isActive = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            pauseMenu.SetActive(false);
            if (playerGun != null)
            {
                playerGun.isActive = true;
            }
        }

        public void Exit()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(1);
        }

        public void ShowEndingScreen()
        {
            if (playerGun != null)
            {
                playerGun.isActive = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            endingScreen.SetActive(true);
            Time.timeScale = 0f;
        }

        public void ShowDeathMenu()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            deathMenu.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}