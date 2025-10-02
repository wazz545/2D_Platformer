using UnityEngine;

[CreateAssetMenu(fileName = "CharStats_", menuName = "2DPlatformer/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    [Header("Ownership")]
    public bool isPlayer = true;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Player-Only")]
    public int maxMana = 100;
    public int currentMana = 100;
    public int lives = 3;
    public int experiencePoints = 0;
}
