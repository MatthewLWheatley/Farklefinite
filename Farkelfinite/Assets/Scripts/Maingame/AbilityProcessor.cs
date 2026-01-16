using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class AbilityProcessor
{
    private GameManager gameManager;
    private AbilityAnimationController animController;

    public AbilityProcessor(GameManager gm)
    {
        gameManager = gm;
        animController = Object.FindFirstObjectByType<AbilityAnimationController>();
        if (animController == null)
        {
            Debug.LogWarning("No AbilityAnimationController found in scene!");
        }
    }

    public IEnumerator ProcessAbilitiesAsync(TriggerType trigger, int diceIndex = -1, List<int> diceGroup = null)
    {
        List<DiceData> relevantDice = new List<DiceData>();

        if (diceIndex >= 0)
        {
            relevantDice.Add(gameManager.diceDataList[diceIndex]);
        }
        else if (diceGroup != null)
        {
            foreach (int idx in diceGroup)
            {
                relevantDice.Add(gameManager.diceDataList[idx]);
            }
        }
        else
        {
            for (int i = 0; i < gameManager.diceDataList.Count; i++)
            {
                if (!gameManager.setAsideDice[i])
                {
                    relevantDice.Add(gameManager.diceDataList[i]);
                }
            }

            foreach (var group in gameManager.setAsideGroups)
            {
                foreach (int diceIdx in group)
                {
                    relevantDice.Add(gameManager.diceDataList[diceIdx]);
                }
            }
        }

        foreach (var dice in relevantDice)
        {
            if (dice.diceConfig == null) continue;

            foreach (var ability in dice.diceConfig.abilities)
            {
                if (ability.trigger == trigger)
                {
                    yield return gameManager.StartCoroutine(TryActivateAbilityAsync(ability, dice, diceGroup));
                }
            }
        }
    }

    private IEnumerator TryActivateAbilityAsync(DiceAbility ability, DiceData sourceDice, List<int> currentGroup)
    {
        if (!CheckConditions(ability.conditions, sourceDice, currentGroup))
            yield break;

        Debug.Log($"Activating ability: {ability.abilityName} on {sourceDice.diceConfig.diceName}");

        if (ability.abilityAnimation != null && animController != null)
        {
            yield return gameManager.StartCoroutine(
                animController.PlayAbilityAnimation(ability, sourceDice.gameObject)
            );
        }

        ApplyEffects(ability.effects, sourceDice, currentGroup);

        yield return new WaitForSeconds(0.2f);
    }

    private bool CheckConditions(List<AbilityCondition> conditions, DiceData sourceDice, List<int> currentGroup)
    {
        if (conditions.Count == 0) return true;

        foreach (var condition in conditions)
        {
            float leftValue = GetVariableValue(condition.leftVariable, sourceDice, currentGroup, condition.leftVariableValue);
            float rightValue = condition.useRightVariable
                ? GetVariableValue(condition.rightVariable, sourceDice, currentGroup, condition.rightVariableValue)
                : condition.rightVariableValue;

            if (!EvaluateComparison(leftValue, condition.comparator, rightValue))
                return false;
        }

        return true;
    }

    private bool EvaluateComparison(float left, ComparatorType comparator, float right)
    {
        switch (comparator)
        {
            case ComparatorType.LessThan:
                return left < right;
            case ComparatorType.LessThanOrEqual:
                return left <= right;
            case ComparatorType.EqualTo:
                return left == right;
            case ComparatorType.GreaterThan:
                return left > right;
            case ComparatorType.GreaterThanOrEqual:
                return left >= right;
            case ComparatorType.NotEqual:
                return left != right;
            default:
                return false;
        }
    }

    private void ApplyEffects(List<AbilityEffect> effects, DiceData sourceDice, List<int> currentGroup)
    {
        foreach (var effect in effects)
        {
            float sourceValue = effect.useSourceVariable
                ? GetVariableValue(effect.sourceVariable, sourceDice, currentGroup, effect.sourceValue)
                : effect.sourceValue;

            ApplyEffect(effect, sourceValue, sourceDice, currentGroup);
        }
    }

    private void ApplyEffect(AbilityEffect effect, float value, DiceData sourceDice, List<int> currentGroup)
    {
        switch (effect.effectType)
        {
            case EffectType.AddToVariable:
                if (effect.useSourceVariable)
                {
                    float variableValue0 = GetVariableValue(effect.sourceVariable, sourceDice, currentGroup, 0);
                    float finalValue = variableValue0 * effect.sourceValue;
                    ModifyVariable(effect.targetVariable, sourceDice, currentGroup, finalValue, true);
                }
                else
                {
                    ModifyVariable(effect.targetVariable, sourceDice, currentGroup, value, true);
                }
                break;

            case EffectType.SubtractFromVariable:
                ModifyVariable(effect.targetVariable, sourceDice, currentGroup, -value, true);
                break;

            case EffectType.MultiplyVariable:
                    ModifyVariable(effect.targetVariable, sourceDice, currentGroup, value, false, true);
                break;

            case EffectType.DivideVariable:
                ModifyVariable(effect.targetVariable, sourceDice, currentGroup, value, false, false, true);
                break;

            case EffectType.SetVariable:
                SetVariable(effect.targetVariable, sourceDice, value);
                break;

            case EffectType.RetriggerAbility:
                for (int i = 0; i < effect.retriggerCount; i++)
                {
                    foreach (var ability in sourceDice.diceConfig.abilities)
                    {
                        if (ability != GetCurrentAbility(sourceDice))
                        {
                            gameManager.StartCoroutine(TryActivateAbilityAsync(ability, sourceDice, currentGroup));
                        }
                    }
                }
                break;

            case EffectType.GainLife:
                gameManager.lives += (int)value;
                gameManager.UpdateScoreUI();
                break;

            case EffectType.LoseLife:
                gameManager.lives -= (int)value;
                gameManager.UpdateScoreUI();
                break;

            case EffectType.LinearScaleByVariable:
                float increment = effect.sourceValue;  // e.g., 0.1
                float variableValue = GetVariableValue(effect.sourceVariable, sourceDice, currentGroup, 0);
                float linearMultiplier = 1 + (increment * variableValue);
                ModifyVariable(effect.targetVariable, sourceDice, currentGroup, linearMultiplier, false, true);
                break;

            case EffectType.ExponentialScaleByVariable:
                float baseValue = effect.sourceValue;  // e.g., 1.1
                float exponent = GetVariableValue(effect.sourceVariable, sourceDice, currentGroup, 0);
                float expMultiplier = Mathf.Pow(baseValue, exponent);
                ModifyVariable(effect.targetVariable, sourceDice, currentGroup, expMultiplier, false, true);
                break;
        }
    }

    private float GetVariableValue(VariableType variable, DiceData sourceDice, List<int> currentGroup, float defaultValue)
    {
        switch (variable)
        {
            case VariableType.ThisDicePipValue:
                return sourceDice.pips[sourceDice.currentFace];

            case VariableType.ThisDiceGroupSize:
                return currentGroup != null ? currentGroup.Count : 1;

            case VariableType.ThisDiceGroupPosition:
                if (currentGroup != null)
                {
                    int sourceIndex = gameManager.diceDataList.IndexOf(sourceDice);
                    return currentGroup.IndexOf(sourceIndex);
                }
                return 0;

            case VariableType.SetAsideScore:
                return gameManager.setAsideScore;

            case VariableType.BankScore:
                return gameManager.totalScore;

            case VariableType.TotalScore:
                return gameManager.totalScore;

            case VariableType.Lives:
                return gameManager.lives;

            case VariableType.TotalDiceCount:
                return gameManager.diceDataList.Count;

            case VariableType.ActiveDiceCount:
                return gameManager.setAsideDice.FindAll(s => !s).Count;

            case VariableType.SetAsideDiceCount:
                return gameManager.setAsideDice.FindAll(s => s).Count;

            case VariableType.GroupCount:
                return gameManager.GetSetAsideGroupCount();

            case VariableType.HeldCardVariable:
                foreach (var ability in sourceDice.diceConfig.abilities)
                {
                    return ability.heldVariable;
                }
                return 0;

            case VariableType.RandomNumber:
                return Random.Range(0, defaultValue + 1);

            case VariableType.ArbitraryNumber:
                return defaultValue;

            case VariableType.ThisDiceIsSetAside:
                int sourceIndex2 = gameManager.diceDataList.IndexOf(sourceDice);
                return gameManager.setAsideDice[sourceIndex2] ? 1 : 0;

            case VariableType.ThisDiceWasJustRolled:
                int diceIdx = gameManager.diceDataList.IndexOf(sourceDice);
                return gameManager.setAsideDice[diceIdx] ? 0 : 1;

            case VariableType.CurrentGroupPipSum:
                if (currentGroup != null)
                {
                    int sum = 0;
                    foreach (int idx in currentGroup)
                    {
                        sum += gameManager.diceDataList[idx].pips[gameManager.diceDataList[idx].currentFace];
                    }
                    return sum;
                }
                return 0;

            case VariableType.CurrentGroupScore:
                if (currentGroup != null && currentGroup.Count > 0)
                {
                    int groupIndex = -1;
                    for (int i = 0; i < gameManager.setAsideGroups.Count; i++)
                    {
                        if (gameManager.setAsideGroups[i].SequenceEqual(currentGroup))
                        {
                            groupIndex = i;
                            break;
                        }
                    }
                    if (groupIndex >= 0 && groupIndex < gameManager.setAsideGroupScores.Count)
                    {
                        return gameManager.setAsideGroupScores[groupIndex];
                    }
                }
                return 0;

            case VariableType.CurrentGroupUniqueValues:
                if (currentGroup != null)
                {
                    HashSet<int> uniquePips = new HashSet<int>();
                    foreach (int idx in currentGroup)
                    {
                        uniquePips.Add(gameManager.diceDataList[idx].pips[gameManager.diceDataList[idx].currentFace]);
                    }
                    return uniquePips.Count;
                }
                return 0;

            case VariableType.TotalDiceWithAbilities:
                int totalWithAbilities = 0;
                foreach (var die in gameManager.diceDataList)
                {
                    if (die.diceConfig != null && die.diceConfig.abilities.Count > 0)
                        totalWithAbilities++;
                }
                return totalWithAbilities;

            case VariableType.ActiveDiceWithAbilities:
                int activeWithAbilities = 0;
                for (int i = 0; i < gameManager.diceDataList.Count; i++)
                {
                    if (!gameManager.setAsideDice[i] &&
                        gameManager.diceDataList[i].diceConfig != null &&
                        gameManager.diceDataList[i].diceConfig.abilities.Count > 0)
                    {
                        activeWithAbilities++;
                    }
                }
                return activeWithAbilities;

            case VariableType.SetAsideDiceWithAbilities:
                int setAsideWithAbilities = 0;
                for (int i = 0; i < gameManager.diceDataList.Count; i++)
                {
                    if (gameManager.setAsideDice[i] &&
                        gameManager.diceDataList[i].diceConfig != null &&
                        gameManager.diceDataList[i].diceConfig.abilities.Count > 0)
                    {
                        setAsideWithAbilities++;
                    }
                }
                return setAsideWithAbilities;

            case VariableType.CurrentGroupDiceWithAbilities:
                if (currentGroup == null) return 0;
                int groupWithAbilities = 0;
                foreach (int idx in currentGroup)
                {
                    if (gameManager.diceDataList[idx].diceConfig != null &&
                        gameManager.diceDataList[idx].diceConfig.abilities.Count > 0)
                    {
                        groupWithAbilities++;
                    }
                }
                return groupWithAbilities;

            case VariableType.TotalPipCount:
                int totalPips = 0;
                foreach (var die in gameManager.diceDataList)
                {
                    totalPips += die.pips[die.currentFace];
                }
                return totalPips;

            case VariableType.ActivePipCount:
                int activePips = 0;
                for (int i = 0; i < gameManager.diceDataList.Count; i++)
                {
                    if (!gameManager.setAsideDice[i])
                    {
                        activePips += gameManager.diceDataList[i].pips[gameManager.diceDataList[i].currentFace];
                    }
                }
                return activePips;

            case VariableType.SetAsidePipCount:
                int setAsidePips = 0;
                for (int i = 0; i < gameManager.diceDataList.Count; i++)
                {
                    if (gameManager.setAsideDice[i])
                    {
                        setAsidePips += gameManager.diceDataList[i].pips[gameManager.diceDataList[i].currentFace];
                    }
                }
                return setAsidePips;

            case VariableType.Money:
                return PlayerData.Instance != null ? PlayerData.Instance.money : 0;

            default:
                return defaultValue;
        }
    }

    private void ModifyVariable(VariableType variable, DiceData sourceDice, List<int> currentGroup,
        float value, bool add = false, bool multiply = false, bool divide = false)
    {
        float currentValue = GetVariableValue(variable, sourceDice, currentGroup, 0);
        float newValue = currentValue;

        if (add)
            newValue += value;
        else if (multiply)
            newValue *= value;
        else if (divide && value != 0)
            newValue /= value;

        SetVariable(variable, sourceDice, newValue);
    }

    private void SetVariable(VariableType variable, DiceData sourceDice, float value)
    {
        switch (variable)
        {
            case VariableType.SetAsideScore:
                gameManager.setAsideScore = (int)value;
                gameManager.UpdateScoreUI();
                break;

            case VariableType.TotalScore:
                gameManager.totalScore = (int)value;
                gameManager.UpdateScoreUI();
                break;

            case VariableType.Lives:
                gameManager.lives = (int)value;
                gameManager.UpdateScoreUI();
                break;

            case VariableType.HeldCardVariable:
                foreach (var ability in sourceDice.diceConfig.abilities)
                {
                    ability.heldVariable = (int)value;
                }
                break;

            case VariableType.Money:
                if (PlayerData.Instance != null)
                {
                    PlayerData.Instance.money = Mathf.Max(0, (int)value); 
                    Debug.Log($"Dice ability set money to: {PlayerData.Instance.money}");
                }
                break;
        }
    }

    private DiceAbility GetCurrentAbility(DiceData sourceDice)
    {
        return null;
    }

    private float CalculateMultiplier(float baseMultiplier, int exponent)
    {
        return Mathf.Pow(baseMultiplier, exponent);
    }
}