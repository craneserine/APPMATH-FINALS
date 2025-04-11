using UnityEngine;
using System.Collections.Generic;

public class Fireball : MonoBehaviour
{
    public float speed = 15f; // Fast speed for fireball
    public float lifetime = 2f; // Shorter lifetime
    public int damage = 1;
    public float size = 0.5f;
    public Material fireballMaterial; // Assign in inspector
    
    private int colliderId;
    private float spawnTime;
    private Vector3 direction;
    private Mesh fireballMesh;
    private Matrix4x4 fireballMatrix;
    private Camera mainCamera;

    void Awake()
    {
        CreateFireballMesh();
        mainCamera = Camera.main;
    }

    void CreateFireballMesh()
    {
        fireballMesh = new Mesh();
        Vector3[] vertices = new Vector3[8];
        float halfSize = size * 0.5f;
        
        // Bottom vertices
        vertices[0] = new Vector3(-halfSize, -halfSize, -halfSize);
        vertices[1] = new Vector3(halfSize, -halfSize, -halfSize);
        vertices[2] = new Vector3(halfSize, -halfSize, halfSize);
        vertices[3] = new Vector3(-halfSize, -halfSize, halfSize);
        
        // Top vertices
        vertices[4] = new Vector3(-halfSize, halfSize, -halfSize);
        vertices[5] = new Vector3(halfSize, halfSize, -halfSize);
        vertices[6] = new Vector3(halfSize, halfSize, halfSize);
        vertices[7] = new Vector3(-halfSize, halfSize, halfSize);
        
        int[] triangles = new int[36]
        {
            // Bottom
            0, 3, 2, 2, 1, 0,
            // Top
            4, 5, 6, 6, 7, 4,
            // Front
            0, 1, 5, 5, 4, 0,
            // Back
            2, 3, 7, 7, 6, 2,
            // Left
            3, 0, 4, 4, 7, 3,
            // Right
            1, 2, 6, 6, 5, 1
        };
        
        fireballMesh.vertices = vertices;
        fireballMesh.triangles = triangles;
        fireballMesh.RecalculateNormals();
    }

    public void Initialize(Vector3 startPos, Vector3 fireDirection)
    {
        spawnTime = Time.time;
        direction = fireDirection.normalized;
        
        // Register with collision system
        colliderId = CollisionManager.Instance.RegisterCollider(
            startPos, 
            Vector3.one * size, 
            false);
        
        // Set initial position and matrix
        fireballMatrix = Matrix4x4.TRS(startPos, Quaternion.LookRotation(direction), Vector3.one * size);
        CollisionManager.Instance.UpdateMatrix(colliderId, fireballMatrix);
    }

    void Update()
    {
        // Check lifetime
        if (Time.time - spawnTime > lifetime)
        {
            DestroyFireball();
            return;
        }

        // Move fireball
        Vector3 currentPos = fireballMatrix.GetPosition();
        Vector3 newPos = currentPos + direction * speed * Time.deltaTime;
        
        // Update collision and matrix
        fireballMatrix = Matrix4x4.TRS(newPos, Quaternion.LookRotation(direction), Vector3.one * size);
        
        // Check collisions
        if (CollisionManager.Instance.CheckCollision(colliderId, newPos, out List<int> collidedIds))
        {
            foreach (int id in collidedIds)
            {
                // Damage enemies
                EnemyManager.Instance?.DestroyEnemy(id);
            }
            DestroyFireball();
            return;
        }

        // Update collision system
        CollisionManager.Instance.UpdateCollider(colliderId, newPos, Vector3.one * size);
        CollisionManager.Instance.UpdateMatrix(colliderId, fireballMatrix);
        
        // Render with visibility check using dot product
        RenderFireball();
    }

    void RenderFireball()
    {
        if (mainCamera == null || fireballMaterial == null) return;
        
        Vector3 fireballPos = fireballMatrix.GetPosition();
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 toFireball = (fireballPos - mainCamera.transform.position).normalized;
        
        // Calculate dot product for visibility
        float dot = Vector3.Dot(cameraForward, toFireball);
        bool isVisible = dot > 0; // Only render if in front of camera
        
        // Scale to zero if not visible
        Vector3 renderScale = isVisible ? Vector3.one * size : Vector3.zero;
        Matrix4x4 renderMatrix = Matrix4x4.TRS(fireballPos, Quaternion.LookRotation(direction), renderScale);
        
        // Draw the mesh
        Graphics.DrawMesh(
            fireballMesh,
            renderMatrix,
            fireballMaterial,
            0, // Default layer
            null,
            0, // Submesh index
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false
        );
    }

    void DestroyFireball()
    {
        CollisionManager.Instance.RemoveCollider(colliderId);
        Destroy(gameObject);
    }
}