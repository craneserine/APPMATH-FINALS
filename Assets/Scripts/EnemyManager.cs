using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EnemyManager");
                _instance = go.AddComponent<EnemyManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public Material enemyMaterial;
    public int enemyCount = 5;
    public float enemySize = 1f;
    public float moveSpeed = 2f;
    public float minMoveDistance = 3f;
    public float maxMoveDistance = 8f;
    public float spawnPadding = 2f;
    public int damageToPlayer = 1;
    public float damageCooldown = 1f;

    private Mesh enemyMesh;
    private List<Matrix4x4> enemyMatrices = new List<Matrix4x4>();
    private List<int> enemyColliderIds = new List<int>();
    private List<float> moveDirections = new List<float>();
    private List<float> moveDistances = new List<float>();
    private List<Vector3> startPositions = new List<Vector3>();
    private float lastDamageTime;

    void Start()
    {
        if (enemyMaterial != null)
        {
            enemyMaterial.enableInstancing = true;
        }

        CreateEnemyMesh();
        SpawnEnemies();
        lastDamageTime = -damageCooldown;
    }

    void CreateEnemyMesh()
    {
        enemyMesh = new Mesh();
        
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(0, 0, 0),
            new Vector3(enemySize, 0, 0),
            new Vector3(enemySize, 0, enemySize),
            new Vector3(0, 0, enemySize),
            new Vector3(0, enemySize, 0),
            new Vector3(enemySize, enemySize, 0),
            new Vector3(enemySize, enemySize, enemySize),
            new Vector3(0, enemySize, enemySize)
        };
        
        int[] triangles = new int[36]
        {
            0, 4, 1, 1, 4, 5,
            2, 6, 3, 3, 6, 7,
            0, 3, 4, 4, 3, 7,
            1, 5, 2, 2, 5, 6,
            0, 1, 3, 3, 1, 2,
            4, 7, 5, 5, 7, 6
        };
        
        enemyMesh.vertices = vertices;
        enemyMesh.triangles = triangles;
        enemyMesh.RecalculateNormals();
        enemyMesh.RecalculateBounds();
    }

    void SpawnEnemies()
    {
        // Your existing enemy spawning logic
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-10f, 10f),
                0,
                Random.Range(-10f, 10f)
            );

            // Register with collision system
            int id = CollisionManager.Instance.RegisterCollider(position, Vector3.one * enemySize, false);
            enemyColliderIds.Add(id);
            enemyMatrices.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * enemySize));
            startPositions.Add(position);
        }
    }

    public void DestroyEnemy(int colliderId)
    {
        // Logic to destroy the enemy
        int index = enemyColliderIds.IndexOf(colliderId);
        if (index >= 0)
        {
            CollisionManager.Instance.RemoveCollider(colliderId);
            enemyColliderIds.RemoveAt(index);
            enemyMatrices.RemoveAt(index);
            startPositions.RemoveAt(index);
        }
    }

    void Update()
    {
        // Update enemy positions and check for player collisions
        for (int i = 0; i < enemyColliderIds.Count; i++)
        {
            // Move enemies in a random direction
            Vector3 currentPosition = enemyMatrices[i].GetPosition();
            float moveDirection = Random.Range(-1f, 1f);
            currentPosition.x += moveDirection * moveSpeed * Time.deltaTime;

            // Check for collisions with the player or other objects
            if (CollisionManager.Instance.CheckCollision(enemyColliderIds[i], currentPosition, out List<int> collidedIds))
            {
                foreach (int id in collidedIds)
                {
                    // Handle collision with player
                    EnhancedMeshGenerator player = FindObjectOfType<EnhancedMeshGenerator>();
                    if (player != null && id == player.GetPlayerID())
                    {
                        // Apply damage logic (you'll need to implement this)
                        Debug.Log("Enemy hit player!");
                        // Example: player.TakeDamage(damageToPlayer);
                    }
                }
            }

            // Update the enemy's position in the collision manager
            enemyMatrices[i] = Matrix4x4.TRS(currentPosition, Quaternion.identity, Vector3.one * enemySize);
            CollisionManager.Instance.UpdateMatrix(enemyColliderIds[i], enemyMatrices[i]);
        }
    }

    void OnRenderObject()
    {
        // Render all enemies
        for (int i = 0; i < enemyMatrices.Count; i++)
        {
            Graphics.DrawMesh(enemyMesh, enemyMatrices[i], enemyMaterial, 0);
        }
    }
}