using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public Material obstacleMaterial; // Renamed from nonLethalMaterial
    public int obstacleCount = 10;
    public float obstacleSize = 1f;
    public float spawnPadding = 2f;

    private Mesh obstacleMesh;
    private List<Matrix4x4> obstacleMatrices = new List<Matrix4x4>();
    private List<int> obstacleColliderIds = new List<int>();

    void Start()
    {
        if (obstacleMaterial != null) obstacleMaterial.enableInstancing = true;

        CreateObstacleMesh();
        SpawnObstacles();
    }

    void CreateObstacleMesh()
    {
        obstacleMesh = new Mesh();
        
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(0, 0, 0),
            new Vector3(obstacleSize, 0, 0),
            new Vector3(obstacleSize, 0, obstacleSize),
            new Vector3(0, 0, obstacleSize),
            new Vector3(0, obstacleSize, 0),
            new Vector3(obstacleSize, obstacleSize, 0),
            new Vector3(obstacleSize, obstacleSize, obstacleSize),
            new Vector3(0, obstacleSize, obstacleSize)
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
        
        obstacleMesh.vertices = vertices;
        obstacleMesh.triangles = triangles;
        obstacleMesh.RecalculateNormals();
        obstacleMesh.RecalculateBounds();
    }

    void SpawnObstacles()
    {
        float playerStartX = 0f;
        float rightSideLength = 10f; // Replace with your logic to get the right side length
        float sectionLength = rightSideLength / obstacleCount;
    
        for (int i = 0; i < obstacleCount; i++)
        {
            float sectionStart = playerStartX + (i * sectionLength);
            float sectionEnd = sectionStart + sectionLength;
        
            Vector3 position = new Vector3(
                Random.Range(sectionStart + spawnPadding, sectionEnd - spawnPadding),
                obstacleSize * 0.5f, // Half height to sit on ground
                0f // Replace with your constant Z position
            );

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = Vector3.one * obstacleSize;

            int id = CollisionManager.Instance.RegisterCollider(
                position,
                new Vector3(obstacleSize, obstacleSize, obstacleSize),
                false);

            Matrix4x4 obstacleMatrix = Matrix4x4.TRS(position, rotation, scale);
            obstacleMatrices.Add(obstacleMatrix);
            obstacleColliderIds.Add(id);

            CollisionManager.Instance.UpdateMatrix(id, obstacleMatrix);
        }
    }

    void Update()
    {
        CheckPlayerCollisions();
        RenderObstacles();
    }

    void CheckPlayerCollisions()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPlayerInvincible()) return;
        if (obstacleMatrices.Count == 0) return;

        var playerMatrix = CollisionManager.Instance.GetMatrix(GameManager.Instance.GetPlayerID());
        Vector3 playerPos = playerMatrix.GetPosition();
        Vector3 playerSize = new Vector3(1f, 1f, 1f); // Replace with your logic to get player size
        float playerRadius = Mathf.Max(playerSize.x, playerSize.y, playerSize.z) * 0.5f;

        for (int i = 0; i < obstacleMatrices.Count; i++)
        {
            Vector3 obstaclePos = obstacleMatrices[i].GetPosition();
            
            float dx = playerPos.x - obstaclePos.x;
            float dy = playerPos.y - obstaclePos.y;
            float dz = playerPos.z - obstaclePos.z;
            float sqrDistance = dx * dx + dy * dy + dz * dz;
            
            float combinedRadius = playerRadius + obstacleSize;
            if (sqrDistance < combinedRadius * combinedRadius)
            {
                Debug.Log("Collision detected with obstacle");
                // Collision response is handled here
            }
        }
    }

    void RenderObstacles()
    {
        if (obstacleMatrices.Count == 0) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        List<Matrix4x4> visibleObstacles = new List<Matrix4x4>();

        for (int i = 0; i < obstacleMatrices.Count; i++)
        {
            Vector3 obstaclePos = obstacleMatrices[i].GetPosition();
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(obstaclePos);
            
            bool isVisible = viewportPos.x > -0.5f && viewportPos.x < 1.5f && 
                             viewportPos.y > -0.5f && viewportPos.y < 1.5f &&
                             viewportPos.z > mainCamera.nearClipPlane;

            Matrix4x4 matrixToRender;
            if (isVisible)
            {
                matrixToRender = obstacleMatrices[i];
            }
            else
            {
                matrixToRender = Matrix4x4.TRS(
                    obstaclePos,
                    obstacleMatrices[i].rotation,
                    Vector3.zero
                );
            }

            visibleObstacles.Add(matrixToRender);
        }

        // Render obstacles
        if (obstacleMaterial != null && visibleObstacles.Count > 0)
        {
            Matrix4x4[] obstacleArray = visibleObstacles.ToArray();
            for (int i = 0; i < obstacleArray.Length; i += 1023)
            {
                int batchSize = Mathf.Min(1023, obstacleArray.Length - i);
                Graphics.DrawMeshInstanced(
                    obstacleMesh,
                    0,
                    obstacleMaterial,
                    obstacleArray,
                    batchSize,
                    null,
                    UnityEngine.Rendering.ShadowCastingMode.Off,
                    false
                );
            }
        }
    }
}