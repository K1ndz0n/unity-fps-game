using System.Collections;
using System.Collections.Generic;
using GunScripts;
using MapScripts;
using UnityEngine;
using Grid = MapScripts.Grid;
using Random = UnityEngine.Random;

namespace EnemyScripts
{
    public class EnemyBehaviour : MonoBehaviour
    {
        public Transform playerTransform;
        public MeshCollider obstacleCheck;
        public MeshCollider heightCheck;
        public LayerMask obstacleMask;
        public EnemyGun enemyGun;
        public Transform enemyCamera;
        public Grid grid;
        
        public float patrolDistance = 20f;
        public float viewAngle = 60f; // szerokość kąta widzenia (w stopniach)
        public float closeCombatRange = 10f;
        public float pathPointReachThreshold = 0.6f;
        public int hideSpotSearchRange = 15;
        
        private Target health;
        private EnemyMovement movement;
        
        private Vector3 walkPoint;
        private Vector3 peekSpot;
        private Vector3 playerLastPos;
        private List<Vector3> currentPath;
        private List<Vector3> patrolPointPath;
        private List<Vector3> hidePath;
        private List<Vector3> peekPath;
        private List<Vector3> playerLastPosPath;

        private bool attackMode = false;
        private bool walkPointSet = false;
        private bool isInCloseCombat = false;
        private bool isLookingAround = false;
        private bool hideSpotSet = false;
        private bool peekSpotSet = false;
        private bool hideSpotReached = false;
        private bool peekSpotReached = false;
        private bool playerLastPosSet = false;
        private bool gunBlocked = false;
        
        private float nextFireTime = 0f;
        private int currentPathIndex = 0;
        private float currentHp;
        private int shotCount = 0;

        private float shotTimer = 0f;
        private float maxStuckTime = 3f;
        private float playerSpotTime = 0f;
        
        private float moveDirectionTimer = 0f;
        private float moveDirectionInterval = 1f;
        private float damageTakenTimer = 0f;
        private float damageTaken = 0f;
        private Vector3 currentRandomDirection = Vector3.zero;
        
        private void Start()
        {
            movement = GetComponent<EnemyMovement>();
            health = GetComponent<Target>();
            currentHp = health.hp;
        }

        private void Update()
        {
            if (shotCount >= 5)
            {
                gunBlocked = true;
                shotTimer += Time.deltaTime;
                if (shotTimer >= 1f)
                {
                    gunBlocked = false;
                    shotCount = 0;
                    shotTimer = 0f;
                }
            }
            
            damageTakenTimer += Time.deltaTime;
            Vector3 distanceToPlayer = playerTransform.position - transform.position;
            distanceToPlayer.y = 0;
            
            float distance = distanceToPlayer.magnitude;

            if (damageTakenTimer >= 1f)
            {
                damageTakenTimer = 0;
                damageTaken = 0;
            }

            if (distanceToPlayer.magnitude > closeCombatRange) // sprawdzamy czy wyszlismy z zasiegu close combat
            {
                isInCloseCombat = false;
            }

            if (currentHp > health.hp)
            {
                damageTaken += currentHp - health.hp;
                currentHp = health.hp;
                hideSpotSet = false;
                peekSpotSet = false;
                hideSpotReached = false;
                peekSpotReached = false;
                playerLastPos = playerTransform.position;
                attackMode = true;

                if (damageTaken >= 30)
                {
                    SearchHideSpot();
                    if (hideSpotSet)
                    {
                        Hide();
                    }
                    else
                    {
                        SearchPlayer();
                    }
                }
                else
                {
                    SearchPlayer();
                }
            }

            if (CanSeePlayer())
            {
                if (!hideSpotSet && !peekSpotSet)
                {
                    walkPointSet = false;
                }
                attackMode = true;
                
                if (!isInCloseCombat)
                {
                    movement.rotationLocked = true;
                }
                playerLastPos = playerTransform.position;
                playerSpotTime += Time.deltaTime;

                if (playerSpotTime >= 3f && !hideSpotSet)
                {
                    playerSpotTime = 0f;
                    SearchHideSpot();
                    if (hideSpotSet)
                    {
                        Hide();
                    }
                }
                
                if (distanceToPlayer.magnitude <= closeCombatRange)
                {
                    isInCloseCombat = true;
                }
                AttackPlayer();
            }
            
            else if (!CanSeePlayer() && attackMode)
            {
                playerSpotTime = 0f;
                if (hideSpotReached)
                {
                    Peek();
                    hideSpotReached = false;
                }

                else if (peekSpotReached)
                {
                    SearchPlayer();
                    peekSpotReached = false;
                    attackMode = false;
                }

                else if(!hideSpotSet && !peekSpotSet)
                {
                    SearchPlayer();
                    attackMode = false;
                }
            }
            
            else if (!CanSeePlayer() && !attackMode && !isInCloseCombat)
            {
                playerSpotTime = 0f;
                movement.rotationLocked = false;
                Patrol();
            }
            // jak jestesmy w zasiegu close combat i enemy nas zauwazyl to jezeli z tego zasiegu nie wyszlismy enemy nas goni mimo ze nie jestesmy w jego fov                     
            if (isInCloseCombat)
            {
                if (distance > 4f)
                {
                    movement.SetInputDirection(distanceToPlayer);
                    movement.rotationLocked = false;
                }
                else
                {
                    movement.SetInputDirection(Vector3.zero);
                    movement.rotationLocked = true;
                }

                hideSpotSet = false;
                peekSpotSet = false;
                hideSpotReached = false;
                peekSpotReached = false;
                playerLastPos = playerTransform.position;
                walkPointSet = false;
            }
            
            WalkToPoint();
            SetObstacleJumping();
        }
        
        private void SetObstacleJumping()
        {
            Bounds heightCheckBounds = heightCheck.bounds;
            Bounds obstacleCheckBounds = obstacleCheck.bounds;
            Collider[] heightColliders = Physics.OverlapBox(heightCheckBounds.center, heightCheckBounds.extents, Quaternion.identity, obstacleMask);
            Collider[] obstacleColliders = Physics.OverlapBox(obstacleCheckBounds.center, obstacleCheckBounds.extents, Quaternion.identity, obstacleMask);
            
            if (obstacleColliders.Length > 0 && heightColliders.Length == 0)
            {
                movement.Jump();
            }
        }

        private List<Vector3> FindPath(Vector3 startPos, Vector3 endPos)
        {
            int safetyCounter = 0;
            int maxIterations = 100000;
            
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node endNode = grid.NodeFromWorldPoint(endPos);

            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                safetyCounter++;
                if (safetyCounter > maxIterations)
                {
                    return null;
                }

                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                
                if (currentNode == endNode)
                {
                    return SetPath(startNode, endNode);
                }

                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
                    
                    int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newCost;
                        neighbour.hCost = GetDistance(neighbour, endNode);
                        neighbour.parent = currentNode;
                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }

            return null;
        }

        private List<Vector3> SetPath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Add(startNode); // dodaj punkt początkowy
            path.Reverse();

            grid.path = path;
            grid.fullPath = new List<Node>(grid.path);

            return grid.ConvertNodesToWorldPositions(path);
        }

        private void MoveAlongPath()
        {
            // dotarł do końca ścieżki albo nie ma ścieżki
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                movement.SetInputDirection(Vector3.zero);
                if (hideSpotSet) // dotarł do punktu ukrycia
                {
                    hideSpotReached = true;
                    hideSpotSet = false;
                }

                if (peekSpotSet) // dotarł do punktu peekowania
                {
                    peekSpotReached = true;
                    peekSpotSet = false;
                }
                
                walkPointSet = false;
                currentPath = null;
                return;
            }

            Vector3 playerTransformPoint = currentPath[currentPathIndex];
            Vector3 direction = (playerTransformPoint - transform.position);
            direction.y = 0f; // ignorujemy wysokość

            if (direction.magnitude < pathPointReachThreshold)
            {
                currentPathIndex++;
            }
            else
            {
                movement.SetInputDirection(direction.normalized);
            }
        }

        private int GetDistance(Node startNode, Node endNode)
        {
            int distanceX = Mathf.Abs(startNode.gridX - endNode.gridX);
            int distanceY = Mathf.Abs(startNode.gridY - endNode.gridY);

            if (distanceX > distanceY)
            {
                return 14 * distanceY + 10 * (distanceX - distanceY);
            }

            return 14 * distanceX + 10 * (distanceY - distanceX);
        }

        private void Patrol()
        {
            if (isLookingAround) return;
            
            if (!walkPointSet) // 7/10 szansy ze bedzie szedl, 3/10 ze rozejzy sie
            {
                int randomVal = Random.Range(0, 10);
                if (randomVal > 2)
                {
                    SearchPatrolPoint(patrolDistance);
                }
                else
                {
                    int degrees = Random.Range(120, 361);
                    StartCoroutine(LookAround(degrees)); // enemy sie rozglada o losowa ilosc stopnii
                }
            }
        }
        
        private void SearchPatrolPoint(float range)
        {
            float randomZ = Random.Range(-range, range);
            float randomX = Random.Range(-range, range);

            walkPoint = new Vector3(transform.position.x + randomX, 2f, transform.position.z + randomZ);
            Node node = grid.NodeFromWorldPoint(walkPoint);
            if (node.walkable)
            {
                patrolPointPath = FindPath(transform.position, walkPoint);
                if (patrolPointPath != null)
                {
                    walkPointSet = true;
                    currentPath = patrolPointPath;
                    currentPathIndex = 0;
                }
            }
        }

        private void WalkToPoint()
        {
            if (walkPointSet)
            {
                MoveAlongPath();
            }
        }
        
        private IEnumerator LookAround(int degrees)
        {
            isLookingAround = true;
            yield return StartCoroutine(movement.RotateEnemy(degrees));
            isLookingAround = false;
        }
        
        private bool CanSeePlayer()
        {
            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;

            // 1. Sprawdzenie kąta widzenia
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleToPlayer > viewAngle / 2f)
            {
                return false;
            }

            // 2. Sprawdzenie czy coś nie zasłania widoku (raycast)
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, distToPlayer, obstacleMask))
            {
                return false;
            }

            return true;
        }
        
        private bool CanSeePlayerTransformFrom(Vector3 position)
        {
            Vector3 dirToPlayer = (playerTransform.position - position).normalized;
            float distToPlayer = Vector3.Distance(position, playerTransform.position);

            // Raycast z pozycji (z lekkim offsetem w górę, żeby nie było podłogi)
            if (Physics.Raycast(position + Vector3.up, dirToPlayer, out RaycastHit hit, distToPlayer, obstacleMask))
            {
                return false; // coś zasłania
            }

            return true;
        }

        private void AttackPlayer()
        {
            movement.rotationLocked = true;
            if (!hideSpotSet && !isInCloseCombat)
            {
                moveDirectionTimer += Time.deltaTime;
                if (moveDirectionTimer >= moveDirectionInterval)
                {
                    moveDirectionTimer = 0f;
    
                    // Losuj nowy kierunek w poziomie
                    Vector2 randomDir2D = Random.insideUnitCircle.normalized;
                    currentRandomDirection = new Vector3(randomDir2D.x, 0f, randomDir2D.y);
                }

                movement.SetInputDirection(currentRandomDirection);
            }
            
            // 1. Obracanie całego ciała w osi Y
            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0f; // tylko poziom
            Quaternion bodyRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, bodyRotation, Time.deltaTime * 15);

            // Obracanie firePoint w osi X (góra-dół)
            Vector3 aimplayerTransform = playerTransform.position + Vector3.down * 2.5f; // lub Vector3.up * 0.5f jeśli celujesz za nisko
            Vector3 localDir = enemyCamera.parent.InverseTransformPoint(aimplayerTransform);
            float angleX = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            float clampedAngleX = Mathf.Clamp(-angleX, -45f, 45f);
            enemyCamera.localRotation = Quaternion.Euler(clampedAngleX, 0f, 0f);

            if (!gunBlocked)
            {
                if (Time.time >= nextFireTime)
                {
                    shotCount++;
                    enemyGun.Shoot();
                    nextFireTime = Time.time + 1f / 15; // np. 15 strzałów na sekundę
                }
            }
        }

        private void SearchHideSpot()
        {
            Node currentPos = grid.NodeFromWorldPoint(transform.position);
            List<Node> candidates = new List<Node>();

            for (int x = -hideSpotSearchRange; x <= hideSpotSearchRange; x++)
            {
                for (int y = -hideSpotSearchRange; y <= hideSpotSearchRange; y++)
                {
                    int checkX = currentPos.gridX + x;
                    int checkY = currentPos.gridY + y;

                    bool horizontalFound = true;
                    bool verticalFound = true;

                    Node wallNode = grid.TryGetNode(checkX, checkY);
                    if (wallNode != null && !wallNode.walkable)
                    {
                        for (int i = wallNode.gridX - 3; i <= wallNode.gridX + 3; i++)
                        {
                            Node temp = grid.TryGetNode(i, wallNode.gridY);
                            if (temp == null || temp.walkable)
                            {
                                horizontalFound = false;
                            }
                        }
                        
                        for (int i = wallNode.gridY - 3; i <= wallNode.gridY + 3; i++)
                        {
                            Node temp = grid.TryGetNode(wallNode.gridX, i);
                            if (temp == null || temp.walkable)
                            {
                                verticalFound = false;
                            }
                        }

                        if (horizontalFound || verticalFound)
                        {
                            foreach (Node n in grid.GetCardinalNeighbours(wallNode))
                            {
                                if (n.walkable && !CanSeePlayerTransformFrom(n.worldPosition))
                                {
                                    candidates.Add(n);
                                }
                            }
                        }
                    }
                }
            }

            if (candidates.Count == 0) return;

            // Sortuj po odległości w linii prostej
            candidates.Sort((a, b) =>
                Vector3.Distance(a.worldPosition, transform.position)
                    .CompareTo(Vector3.Distance(b.worldPosition, transform.position)));

            int shortestPathCost = int.MaxValue;
            List<Vector3> bestPath = null;
            int maxChecks = Mathf.Min(10, candidates.Count);

            for (int i = 0; i < maxChecks; i++)
            {
                List<Vector3> candidatePath = FindPath(transform.position, candidates[i].worldPosition);
                if (candidatePath != null)
                {
                    int cost = candidatePath.Count;
                    if (cost < shortestPathCost)
                    {
                        shortestPathCost = cost;
                        bestPath = candidatePath;
                        
                    }
                }
            }

            if (bestPath != null)
            {
                hidePath = bestPath;
                hideSpotSet = true;
            }
        }
        
        private void Hide()
        {
            peekSpot = transform.position;
            walkPointSet = true;
            currentPath = hidePath;
            currentPathIndex = 0;
        }

        private void Peek()
        {
            peekPath = FindPath(transform.position, peekSpot);
            if (peekPath != null)
            {
                peekSpotSet = true;
                walkPointSet = true;
                currentPath = peekPath;
                currentPathIndex = 0;
            }
        }

        private void SearchPlayer()
        {
            playerLastPosPath = FindPath(transform.position, playerLastPos);
            if (playerLastPosPath != null)
            {
                playerLastPosSet = true;
                walkPointSet = true;
                currentPath = playerLastPosPath;
                currentPathIndex = 0;
            }
        }
        
        public void OnDrawGizmos() {
            if (currentPath != null) {
                for (int i = currentPathIndex; i < currentPath.Count; i ++) {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(currentPath[i], Vector3.one);

                    if (i == currentPathIndex) {
                        Gizmos.DrawLine(transform.position, currentPath[i]);
                    }
                    else {
                        Gizmos.DrawLine(currentPath[i-1],currentPath[i]);
                    }
                }
            }
        }
    }
}