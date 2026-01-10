using UnityEngine;

public enum DiceType
{
    Normal,
    Party,
    Fire,
    Rock,
    Gear,
    Stars,
    Nature,
    Wave,
    Wind,
    Obsidian
}

public enum TriggerType
{
    OnSetAside,      // When this die is set aside
    OnBank,          // When banking with this die set aside
    OnRoll,          // When this die is rolled
    OnFarkle,        // When a farkle happens
    OnHotDice,       // When hot dice triggers
    OnTurnStart,     // At start of turn
    OnTurnEnd,       // At end of turn
    Passive          // Always active
}

public enum VariableType
{
    ThisDicePipValue,
    ThisDiceGroupSize,
    ThisDiceGroupPosition,

    CurrentGroupPipCount,
    CurrentGroupDiceCount,

    SetAsideScore,
    BankScore,
    TotalScore,
    Lives,

    TotalDiceCount,
    ActiveDiceCount,
    SetAsideDiceCount,
    GroupCount,

    RandomNumber,
    HeldCardVariable,
    ArbitraryNumber
}

public enum ComparatorType
{
    LessThan,
    LessThanOrEqual,
    EqualTo,
    GreaterThan,
    GreaterThanOrEqual,
    NotEqual
}

public enum EffectType
{
    AddToVariable,
    SubtractFromVariable,
    MultiplyVariable,
    DivideVariable,
    SetVariable,
    RetriggerAbility,
    RerollDice,
    AddDiceToGroup,
    RemoveDiceFromGroup,
    GainLife,
    LoseLife,
    LinearScaleByVariable,
    ExponentialScaleByVariable
}