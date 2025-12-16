using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AbilityCondition
{
    public VariableType leftVariable;
    public int leftVariableValue = 0;
    public ComparatorType comparator;
    public VariableType rightVariable;
    public int rightVariableValue = 0;

    public bool useRightVariable = false;
}

[System.Serializable]
public class AbilityEffect
{
    public EffectType effectType;
    public VariableType targetVariable;
    public VariableType sourceVariable;
    public float sourceValue = 0;
    public bool useSourceVariable = false;

    [Tooltip("For retrigger effects - how many times to retrigger")]
    public int retriggerCount = 1;
}

[System.Serializable]
public class DiceAbility
{
    public string abilityName;
    [TextArea(2, 4)]
    public string abilityDescription;
    public TriggerType trigger;

    [Header("Animation")]
    [Tooltip("Animation to play when this ability triggers")]
    public AbilityAnimation abilityAnimation;

    [Header("Conditions")]
    [Tooltip("All conditions must be true for ability to activate")]
    public List<AbilityCondition> conditions = new List<AbilityCondition>();

    [Header("Effects")]
    [Tooltip("All effects will be applied when conditions are met")]
    public List<AbilityEffect> effects = new List<AbilityEffect>();

    [Header("State")]
    [Tooltip("Held variable that persists across turns")]
    public int heldVariable = 0;
}

[CreateAssetMenu(fileName = "New Dice Config", menuName = "Farkle/Dice Configuration")]
public class DiceConfig : ScriptableObject
{
    public DiceType diceType;
    public string diceName;
    [TextArea(3, 6)]
    public string description;

    public Sprite diceSprite;
    public List<GameObject> pipSprites = new List<GameObject>();

    [Tooltip("Can this dice have its pips changed by the player?")]
    public bool canChangeFaces = false;

    [Tooltip("Custom pip layout (leave empty for standard 1-6)")]
    public List<int> customPips = new List<int>();

    public List<DiceAbility> abilities = new List<DiceAbility>();
}