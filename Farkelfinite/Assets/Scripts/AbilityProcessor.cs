using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            relevantDice.AddRange(gameManager.diceDataList);
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
                ModifyVariable(effect.targetVariable, sourceDice, currentGroup, value, true);
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
        }
    }

    private DiceAbility GetCurrentAbility(DiceData sourceDice)
    {
        return null;
    }
}