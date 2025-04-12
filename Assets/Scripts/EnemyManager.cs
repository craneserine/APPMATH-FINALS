using System.Collections.Generic;
using UnityEngine;

/// Manages the spawning, movement, collision detection, and rendering of enemies in the game.
public class EnemyManager : MonoBehaviour
{
    // Public variables for enemy properties
    public Material enemyMaterial; // Material used for rendering enemies
    public int enemyCount = 5; // Number of enemies to spawn
    public float enemySize = 1f; // Size of each enemy
    public float moveSpeed = 2f; // Movement speed of enemies
    public float minMoveDistance = 3f; // Minimum distance enemies can move
    public float maxMoveDistance = 8f; // Maximum distance enemies can move
    public float spawnPadding = 2f; // Padding to ensure enemies do not spawn too close to each other
    public int damageToPlayer = 1; // Damage dealt to the player upon collision
    public float damageCooldown = 1f; // Time between damage instances to the player

    // Private variables for internal management
    private Mesh enemyMesh; // Mesh used for rendering enemies
    private List<Matrix4x4> enemyMatrices = new List<Matrix4x4>(); // Transformation matrices for enemy positions
    private List<int> enemyColliderIds = new List<int>(); // Collider IDs for enemy collision detection
    private List<float> moveDirections = new List<float>(); // Directions in which enemies will move
    private List<float> moveDistances = new List<float>(); // Distances enemies will move
    private List<Vector3> startPositions = new List<Vector3>(); // Starting positions of enemies
    private float lastDamageTime; // Time when the last damage was dealt to the player
    private EnhancedMeshGenerator meshGen; // Reference to the mesh generator for ground and player information

    /// Initializes the enemy manager, creates the enemy mesh, and spawns enemies.
    void Start()
    {
        if (enemyMaterial != null)
        {
            enemyMaterial.enableInstancing = true; // Enable instancing for better performance
        }

        meshGen = FindObjectOfType<EnhancedMeshGenerator>(); // Get reference to the mesh generator
        CreateEnemyMesh(); // Create the mesh for enemies
        SpawnEnemies(); // Spawn the enemies in the game
        lastDamageTime = -damageCooldown; // Initialize last damage time
    }

   /// Creates the mesh used for rendering enemies.
    void CreateEnemyMesh()
    {
        enemyMesh = new Mesh();
        
        // Define vertices for a cube-shaped enemy
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
        
        // Define triangles for the cube
        int[] triangles = new int[36]
        {
            0, 4, 1, 1, 4, 5,
            2, 6, 3, 3, 6, 7,
            0, 3, 4, 4, 3, 7,
            1, 5, 2, 2, 5, 6,
            0, 1, 3, 3, 1, 2,
            4, 7, 5, 5, 7, 6
        };
        
        enemyMesh.vertices = vertices; // Set the vertices for the mesh
        enemyMesh.triangles = triangles; // Set the triangles for the mesh
        enemyMesh.RecalculateNormals(); // Recalculate normals for lighting
        enemyMesh.RecalculateBounds(); // Recalculate bounds for rendering
    }


    /// Spawns enemies at random positions within the defined area.
    void SpawnEnemies()
    {
        if (meshGen == null) return; // Exit if mesh generator is not found

        float playerStartX = 0f; // Starting X position for the player
        float rightSideLength = meshGen.maxX - playerStartX; // Calculate the right side length
        float sectionLength = rightSideLength / enemyCount; // Calculate the length of each section for enemy spawning
    
        for (int i = 0; i < enemyCount; i++)
        {
                        float sectionStart = playerStartX + (i * sectionLength); // Start of the section for this enemy
            float sectionEnd = sectionStart + sectionLength; // End of the section for this enemy
            
            // Generate a random position for the enemy within the section
            Vector3 position = new Vector3(
                Random.Range(sectionStart + spawnPadding, sectionEnd - spawnPadding),
                meshGen.groundY + meshGen.height, // Set height above the ground
                meshGen.constantZPosition // Use a constant Z position
            );

            Quaternion rotation = Quaternion.identity; // No rotation for enemies
            Vector3 scale = Vector3.one * enemySize; // Scale the enemy

            // Register the collider for the enemy
            int id = CollisionManager.Instance.RegisterCollider(
                position,
                new Vector3(enemySize, meshGen.height, enemySize),
                false
            );

            // Create a transformation matrix for the enemy
            Matrix4x4 enemyMatrix = Matrix4x4.TRS(position, rotation, scale);
            enemyMatrices.Add(enemyMatrix); // Store the matrix
            enemyColliderIds.Add(id); // Store the collider ID
            moveDirections.Add(Random.value > 0.5f ? 1f : -1f); // Randomly set movement direction
            moveDistances.Add(Random.Range(minMoveDistance, maxMoveDistance)); // Randomly set movement distance
            startPositions.Add(position); // Store the starting position

            // Update the collider's matrix in the CollisionManager
            CollisionManager.Instance.UpdateMatrix(id, enemyMatrix);
        }
    


    /// Updates the state of the enemies each frame.

    void Update()
    {
        MoveEnemies(); // Move the enemies
        CheckPlayerCollisions(); // Check for collisions with the player
        RenderEnemies(); // Render the enemies
    }


    /// Moves the enemies back and forth within their defined movement range.
    void MoveEnemies()
    {
        if (meshGen == null) return; // Exit if mesh generator is not found

        for (int i = 0; i < enemyMatrices.Count; i++)
        {
            Vector3 currentPos = enemyMatrices[i].GetPosition(); // Get the current position of the enemy
            Vector3 startPos = startPositions[i]; // Get the starting position of the enemy
            
            // Calculate the target position based on movement direction and distance
            float targetX = startPos.x + (moveDistances[i] * moveDirections[i]);
            float newX = Mathf.MoveTowards(currentPos.x, targetX, moveSpeed * Time.deltaTime); // Move towards the target position
            
            // Check if the enemy has reached the target position
            if (Mathf.Approximately(newX, targetX))
            {
                moveDirections[i] *= -1f; // Reverse the movement direction
                startPositions[i] = currentPos; // Update the starting position
            }

            // Create a new position for the enemy
            Vector3 newPos = new Vector3(
                newX, 
                meshGen.groundY + meshGen.height, // Maintain height above ground
                currentPos.z // Keep the Z position unchanged
            );
            
            // Update the enemy's transformation matrix
            enemyMatrices[i] = Matrix4x4.TRS(newPos, enemyMatrices[i].rotation, enemyMatrices[i].lossyScale);
            
            // Update the collider's position in the CollisionManager
            CollisionManager.Instance.UpdateCollider(
                enemyColliderIds[i], 
                newPos, 
                new Vector3(enemySize, meshGen.height, enemySize)
            );
        }
    }


    /// Checks for collisions between enemies and the player.

    void CheckPlayerCollisions()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPlayerInvincible()) return; // Exit if the player is invincible
        if (meshGen == null || meshGen.GetPlayerID() == -1) return; // Exit if mesh generator is not found or player ID is invalid
        if (Time.time - lastDamageTime < damageCooldown) return; // Exit if damage cooldown is active

        var playerMatrix = CollisionManager.Instance.GetMatrix(meshGen.GetPlayerID()); // Get the player's matrix
        Vector3 playerPos = playerMatrix.GetPosition(); // Get the player's position
        Vector3 playerSize = meshGen.GetPlayerSize(); // Get the player's size
        float playerRadius = Mathf.Max(playerSize.x, playerSize.y, playerSize.z) * 0.5f; // Calculate the player's radius

        bool playerHit = false; // Flag to check if the player has been hit

        // Check for collisions
                // Check for collisions between enemies and the player
        for (int i = 0; i < enemyMatrices.Count; i++)
        {
            Vector3 enemyPos = enemyMatrices[i].GetPosition(); // Get the enemy's position
            
            // Calculate the squared distance between the player and the enemy
            float dx = playerPos.x - enemyPos.x;
            float dy = playerPos.y - enemyPos.y;
            float dz = playerPos.z - enemyPos.z;
            float sqrDistance = dx * dx + dy * dy + dz * dz;
            
            // Calculate the combined radius for collision detection
            float combinedRadius = playerRadius + enemySize;
            if (sqrDistance < combinedRadius * combinedRadius) // Check for collision
            {
                playerHit = true; // Set the flag to true if a collision is detected
                break; // Exit the loop as we only need to know if the player was hit
            }
        }

        // If the player was hit, apply damage and reset the damage cooldown
        if (playerHit)
        {
            GameManager.Instance.TakeDamage(damageToPlayer); // Apply damage to the player
            lastDamageTime = Time.time; // Update the last damage time
            Debug.Log($"Enemy hit player! Health: {GameManager.Instance.CurrentHealth}. Next damage in {damageCooldown} seconds.");
        }
    }


    /// Renders the enemies on the screen.

    void RenderEnemies()
    {
        if (enemyMaterial == null || enemyMesh == null || enemyMatrices.Count == 0) return; // Exit if there are no enemies to render

        Camera mainCamera = Camera.main; // Get the main camera
        if (mainCamera == null) return; // Exit if the main camera is not found

        List<Matrix4x4> visibleEnemies = new List<Matrix4x4>(); // List to store visible enemies

        // Check which enemies are visible to the camera
        foreach (var matrix in enemyMatrices)
        {
            Vector3 enemyPos = matrix.GetPosition(); // Get the enemy's position
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemyPos); // Convert world position to viewport position
            
            // Check if the enemy is within the camera's view
            bool isVisible = viewportPos.x > -0.5f && viewportPos.x < 1.5f && 
                             viewportPos.y > -0.5f && viewportPos.y < 1.5f &&
                             viewportPos.z > mainCamera.nearClipPlane;

            if (isVisible)
            {
                visibleEnemies.Add(matrix); // Add the matrix to the visible list if the enemy is visible
            }
            else
            {
                // If not visible, create a zero scale matrix to avoid rendering
                Matrix4x4 zeroScaleMatrix = Matrix4x4.TRS(
                    enemyPos,
                    matrix.rotation,
                    Vector3.zero
                );
                visibleEnemies.Add(zeroScaleMatrix); // Add the zero scale matrix
            }
        }

        // Convert the list of visible enemies to an array for rendering
        Matrix4x4[] matricesArray = visibleEnemies.ToArray();

        // Render the enemies in batches
        for (int i = 0; i < matricesArray.Length; i += 1023)
        {
            int batchSize = Mathf.Min(1023, matricesArray.Length - i); // Determine the batch size
            Graphics.DrawMeshInstanced(
                enemyMesh, // The mesh to render
                0, // Submesh index
                enemyMaterial, // Material to use for rendering
                matricesArray, // Array of transformation matrices
                batchSize, // Number of matrices to render
                null, // No additional properties
                UnityEngine.Rendering.ShadowCastingMode.Off, // Disable shadow casting
                false // Do not receive shadows
            );
        }
    }
}