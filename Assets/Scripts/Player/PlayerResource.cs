using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [Header("Health & Mana Settings")]
    [Range(0f, 1f)]
    public float startingHealth = 1f;
    [Range(0f, 1f)]
    public float startingMana = 1f;
    [Tooltip("Normalized mana cost per shot (0.1 = 10%).")]
    [Range(0f, 1f)]
    public float manaCostPerShot = 0.1f;
    [Tooltip("Normalized mana regeneration per second (0.1 = 10% per second).")]
    [Range(0f, 1f)]
    public float manaRegenPerSecond = 0.1f;

    [Header("UI References")]
    public HealthBar healthBar;
    public ManaBar manaBar;

    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    void Awake()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>(includeInactive: true);
        }

        if (manaBar == null)
        {
            manaBar = GetComponentInChildren<ManaBar>(includeInactive: true);
        }

        CurrentHealth = Mathf.Clamp01(startingHealth);
        CurrentMana = Mathf.Clamp01(startingMana);
        UpdateHealthUI();
        UpdateManaUI();
    }

    public void Tick(float deltaTime)
    {
        if (CurrentMana >= 1f || manaRegenPerSecond <= 0f) return;
        CurrentMana = Mathf.Min(1f, CurrentMana + manaRegenPerSecond * deltaTime);
        UpdateManaUI();
    }

    public bool TryConsumeMana()
    {
        if (CurrentMana < manaCostPerShot) return false;
        CurrentMana = Mathf.Clamp01(CurrentMana - manaCostPerShot);
        UpdateManaUI();
        return true;
    }

    public void SetHealthNormalized(float value)
    {
        CurrentHealth = Mathf.Clamp01(value);
        UpdateHealthUI();
    }

    public void SetManaNormalized(float value)
    {
        CurrentMana = Mathf.Clamp01(value);
        UpdateManaUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.SetValue(CurrentHealth);
        }
    }

    private void UpdateManaUI()
    {
        if (manaBar != null)
        {
            manaBar.SetValue(CurrentMana);
        }
    }
}
