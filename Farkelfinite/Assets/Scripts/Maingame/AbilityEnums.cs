using UnityEngine;

public enum TriggerType
{
    OnSetAside = 0,
    OnBank = 1,
    OnRoll = 2,
    OnFarkle = 3,
    OnHotDice = 4,
    OnTurnStart = 5,
    OnTurnEnd = 6,
    Passive = 7
}

public enum VariableType
{
    ThisDicePipValue = 0,
    ThisDiceGroupSize = 1,
    ThisDiceGroupPosition = 2,
    ThisDiceIsSetAside = 3,
    ThisDiceWasJustRolled = 4,
    CurrentGroupPipSum = 5,
    CurrentGroupScore = 6,
    CurrentGroupUniqueValues = 7,
    SetAsideScore = 8,
    BankScore = 9,
    TotalScore = 10,
    Lives = 11,
    TotalDiceCount = 12,
    ActiveDiceCount = 13,
    SetAsideDiceCount = 14,
    GroupCount = 15,
    TotalDiceWithAbilities = 16,
    ActiveDiceWithAbilities = 17,
    SetAsideDiceWithAbilities = 18,
    CurrentGroupDiceWithAbilities = 19,
    RandomNumber = 20,
    HeldCardVariable = 21,
    ArbitraryNumber = 22,
    TotalPipCount = 23,
    ActivePipCount = 24,
    SetAsidePipCount = 25,
    Money = 26,
}

public enum ComparatorType
{
    LessThan = 0,
    LessThanOrEqual = 1,
    EqualTo = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    NotEqual = 5,
    IsEven = 6,
}

public enum EffectType
{
    AddToVariable = 0,
    SubtractFromVariable = 1,
    MultiplyVariable = 2,
    DivideVariable = 3,
    SetVariable = 4,
    RetriggerAbility = 5,
    RerollDice = 6,
    AddDiceToGroup = 7,
    RemoveDiceFromGroup = 8,
    GainLife = 9,
    LoseLife = 10,
    LinearScaleByVariable = 11,
    ExponentialScaleByVariable = 12
}