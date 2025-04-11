using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Settings")]
    public int maxHealth = 5;
    public float invincibilityDuration = 5f;
    private int currentHealth;
    private bool isInvincible = false;
    public int CurrentHealth => currentHealth;

    [Header("UI Elements")]
    public TMP_Text healthText;
    public TMP_Text timerText;
    public TMP_Text gameOverText; // Changed from gameOverPanel to TMP_Text

    [Header("Game Settings")]
    public float gameDuration = 180f;
    private float currentTime;
    private bool gameRunning = true;

    private PowerUpManager powerUpManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        powerUpManager = FindObjectOfType<PowerUpManager>();
        InitializeGame();
    }

    void InitializeGame()
    {
        currentHealth = maxHealth;
        currentTime = gameDuration;
        gameOverText.gameObject.SetActive(false); // Hide game over text at start
        UpdateHealthUI();
        UpdateTimerUI();
        gameRunning = true;
        StartCoroutine(GameTimer());
    }

    IEnumerator GameTimer()
    {
        while (gameRunning && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();
            
            if (currentTime <= 0)
            {
                GameOver("Time's up!");
            }
            
            yield return null;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            GameOver("You died!");
        }
    }

    public void HealPlayer(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
    }

    public void ActivateInvincibility()
    {
        if (!isInvincible)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        UpdateHealthUI(); // Update color
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
        UpdateHealthUI(); // Revert color
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth}/{maxHealth}";
            healthText.color = isInvincible ? Color.yellow : Color.white;
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void GameOver(string reason)
    {
        gameRunning = false;
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = $"GAME OVER\n{reason}";
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public bool IsPlayerInvincible() => isInvincible;
}