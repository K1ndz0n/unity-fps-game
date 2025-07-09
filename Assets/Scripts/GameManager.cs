using EnemyScripts;
using GunScripts;
using MapScripts;
using UiScripts;
using UnityEngine;
using Grid = MapScripts.Grid;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform weaponHolder;
    public Camera fpsCam;
    public CharacterController playerController;
    public GameObject enemyPrefab;
    public GameObject playerPrefab;
    public Grid grid;
    public MapCreator mapCreator;
    public GameObject keyPrefab;
    public GameObject ammoBoxPrefab;
    public GameObject gunPrefab;
    public int playerHp = 100;

    private GameObject playerInstance;
    
    public float spawnRange = 50f;

    public int enemyCounter = 0;

    private int failedPlayerSpawn = 0;
    private int failedEnemySpawn = 0;
    private int failedKeySpawn = 0;
    private int failedAmmoSpawn = 0;
    private int failedGunSpawn = 0;
    private int keyAmount = 0;
    
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
        
        mapCreator.CreateMap();
        grid.CreateGrid();
        
        SpawnKey();
        SpawnKey();
        SpawnKey();

        int ammoBoxAmount = Random.Range(4, 8);
        for (int i = 0; i < ammoBoxAmount; i++)
        {
            SpawnAmmoBox();
        }
        
        int gunAmount = Random.Range(4, 8);
        for (int i = 0; i < gunAmount; i++)
        {
            SpawnGun();
        }

        SpawnPlayer();
        UiManager.Instance.UpdateHp();
        
        enemyPrefab.GetComponent<EnemyBehaviour>().playerTransform = playerInstance.transform;
        enemyPrefab.GetComponent<EnemyBehaviour>().grid = grid;
        SpawnEnemy();
    }

    private void Update()
    {
        if (enemyCounter == 0)
        {
            SpawnEnemy();
        }

        if (weaponHolder.childCount == 0)
        {
            UiManager.Instance.UpdateAmmoUI(0, 0);
        }
    }

    public void SpawnEnemy()
    {
        while (true)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            Node node = grid.NodeFromWorldPoint(spawnPos);

            if (node.walkable)
            {
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                enemyCounter++;
                failedEnemySpawn = 0;
                return;
            }

            failedEnemySpawn++;
            if (failedEnemySpawn > 50)
            {
                break;
            }
        }
    }
    
    public void SpawnPlayer()
    {
        while (true)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            Node node = grid.NodeFromWorldPoint(spawnPos);

            if (node.walkable)
            {
                playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                playerController = playerInstance.GetComponent<CharacterController>();
                fpsCam = playerInstance.GetComponentInChildren<Camera>();
                weaponHolder = fpsCam.transform.Find("Weapon Holder");
                UiManager.Instance.fpsCam = fpsCam;
                
                if (weaponHolder.childCount <= 0) return;
                Gun startingGun = weaponHolder.GetChild(0).GetComponent<Gun>();
                if (startingGun != null)
                {
                    startingGun.Initialize(fpsCam, Vector3.zero);
                }
                return;
            }

            failedPlayerSpawn++;
            if (failedPlayerSpawn > 50)
            {
                break;
            }
        }
    }

    private void SpawnKey()
    {
        while (true)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            Node node = grid.NodeFromWorldPoint(spawnPos);

            if (node.walkable)
            {
                Instantiate(keyPrefab, spawnPos, Quaternion.identity);
                failedKeySpawn = 0;
                return;
            }

            failedKeySpawn++;
            if (failedKeySpawn > 50)
            {
                break;
            }
        }
    }
    
    private void SpawnAmmoBox()
    {
        while (true)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            Node node = grid.NodeFromWorldPoint(spawnPos);

            if (node.walkable)
            {
                Instantiate(ammoBoxPrefab, spawnPos, Quaternion.identity);
                failedAmmoSpawn = 0;
                return;
            }

            failedAmmoSpawn++;
            if (failedAmmoSpawn > 50)
            {
                break;
            }
        }
    }
    
    private void SpawnGun()
    {
        while (true)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            Node node = grid.NodeFromWorldPoint(spawnPos);

            if (node.walkable)
            {
                Instantiate(gunPrefab, spawnPos, Quaternion.identity);
                failedAmmoSpawn = 0;
                return;
            }

            failedGunSpawn++;
            if (failedGunSpawn > 50)
            {
                break;
            }
        }
    }

    private Vector3 GetRandomSpawnPoint()
    {
        float x = Random.Range(-spawnRange, spawnRange);
        float z = Random.Range(-spawnRange, spawnRange);
        Vector3 spawnPos = new Vector3(x, 0, z);

        return spawnPos;
    }

    public void PickupKey()
    {
        keyAmount++;
    }

    public void OpenDoors()
    {
        if (keyAmount == 3)
        {
            UiManager.Instance.ShowEndingScreen();
        }
    }

    public int GetKeyAmount()
    {
        return keyAmount;
    }

    public void TakeDamage(int damage)
    {
        playerHp -= damage;
        UiManager.Instance.UpdateHp();
        if (playerHp <= 0)
        {
            UiManager.Instance.ShowDeathMenu();
        }
    }
}