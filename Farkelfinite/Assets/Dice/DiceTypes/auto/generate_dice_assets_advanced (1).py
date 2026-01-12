import csv
import os
import re

# Enum mappings
TRIGGER_MAP = {
    "OnSetAside": 0,
    "OnBank": 1,
    "OnRoll": 2,
    "OnFarkle": 3,
    "OnHotDice": 4,
    "OnTurnStart": 5,
    "OnTurnEnd": 6,
    "Passive": 7
}

VARIABLE_MAP = {
    "ThisDicePipValue": 0,
    "ThisDiceGroupSize": 1,
    "CurrentGroupSize": 1,  # Alias
    "ThisDiceGroupPosition": 2,
    "ThisDiceIsSetAside": 3,
    "ThisDiceWasJustRolled": 4,
    "CurrentGroupPipSum": 5,
    "CurrentGroupScore": 6,
    "CurrentGroupUniqueValues": 7,
    "SetAsideScore": 8,
    "BankScore": 9,
    "TotalScore": 10,
    "Lives": 11,
    "TotalDiceCount": 12,
    "ActiveDiceCount": 13,
    "SetAsideDiceCount": 14,
    "GroupCount": 15,
    "SetAsideGroupCount": 15,  # Alias
    "TotalDiceWithAbilities": 16,
    "ActiveDiceWithAbilities": 17,
    "SetAsideDiceWithAbilities": 18,
    "CurrentGroupDiceWithAbilities": 19,
    "RandomNumber": 20,
    "HeldCardVariable": 21,
    "ArbitraryNumber": 22,
    "TotalPipCount": 23,
    "ActivePipCount": 24,
    "SetAsidePipCount": 25,
}

COMPARATOR_MAP = {
    "<": 0,
    "<=": 1,
    "=": 2,
    "==": 2,
    ">": 3,
    ">=": 4,
    "!=": 5,
}

EFFECT_MAP = {
    "AddToVariable": 0,
    "SubtractFromVariable": 1,
    "MultiplyVariable": 2,
    "DivideVariable": 3,
    "SetVariable": 4,
    "RetriggerAbility": 5,
    "RerollDice": 6,
    "AddDiceToGroup": 7,
    "RemoveDiceFromGroup": 8,
    "GainLife": 9,
    "LoseLife": 10,
    "LinearScaleByVariable": 11,
    "ExponentialScaleByVariable": 12
}

DICE_TYPE_MAP = {
    "Normal": 0,
    "Party": 1,
    "Fire": 2,
    "Rock": 3,
    "Gear": 4,
    "Stars": 5,
    "Nature": 6,
    "Wave": 7,
    "Wind": 8,
    "Obsidian": 9
}

DEFAULT_PIP_GUIDS = [
    "37be34c36ffc4ae41a9198f3fbc46e0a",
    "65c974d0eddda024286c3f5052802ed0",
    "551a995738647cb4796c1ff72fc4bdd1",
    "34498b2fb085acf4895a9bfc3c401f77",
    "012e92c2fa2912d4ea733703882aea92",
    "eb27ac6a26037e14aa407f23e410ede0"
]

DEFAULT_PIP_FILEIDS = [
    "1965599317052140067",
    "8211243896810743751",
    "1350188232839117535",
    "5123967086785334009",
    "2371431799608282939",
    "6470227490501812004"
]


def parse_condition(condition_str):
    """Parse a single condition string."""
    condition_str = condition_str.strip()
    
    # Try each comparator
    for comp_str, comp_val in COMPARATOR_MAP.items():
        if comp_str in condition_str:
            parts = condition_str.split(comp_str, 1)
            if len(parts) == 2:
                left_var = parts[0].strip()
                right_val = parts[1].strip()
                
                # Check if left side is a variable
                if left_var in VARIABLE_MAP:
                    # Check if right side is a variable or a number
                    if right_val in VARIABLE_MAP:
                        return {
                            'leftVariable': VARIABLE_MAP[left_var],
                            'comparator': comp_val,
                            'rightVariable': VARIABLE_MAP[right_val],
                            'rightValue': 0,
                            'useRightVariable': 1
                        }
                    elif right_val.replace('.', '').replace('-', '').isdigit():
                        return {
                            'leftVariable': VARIABLE_MAP[left_var],
                            'comparator': comp_val,
                            'rightVariable': 0,
                            'rightValue': int(float(right_val)),
                            'useRightVariable': 0
                        }
    return None


def parse_conditions(conditions_str):
    """Parse conditions string, handling OR by returning list of condition sets."""
    if not conditions_str or conditions_str.strip() == "":
        return [[]]  # No conditions
    
    # Check for OR
    if ' OR ' in conditions_str.upper():
        # Split by OR, each becomes a separate ability
        or_parts = re.split(r'\s+OR\s+', conditions_str, flags=re.IGNORECASE)
        condition_sets = []
        
        for or_part in or_parts:
            # Split by AND within each OR part
            and_parts = re.split(r'\s+&&\s+', or_part)
            conditions = []
            for and_part in and_parts:
                cond = parse_condition(and_part)
                if cond:
                    conditions.append(cond)
            if conditions:
                condition_sets.append(conditions)
        
        return condition_sets if condition_sets else [[]]
    else:
        # Just AND conditions
        and_parts = re.split(r'\s+&&\s+', conditions_str)
        conditions = []
        for and_part in and_parts:
            cond = parse_condition(and_part)
            if cond:
                conditions.append(cond)
        return [conditions] if conditions else [[]]


def parse_effect(effect_str):
    """Parse effect string and return effect dict."""
    effect_str = effect_str.strip()
    
    # Skip multi-step effects
    if 'THEN' in effect_str.upper() or 'BUT' in effect_str.upper() or 'ELSE' in effect_str.upper():
        return None
    
    # Handle LinearScaleByVariable: *= 1 + (0.1 * Variable)
    linear_match = re.search(r'(\w+)\s*\*=\s*1\s*\+\s*\(([\d.]+)\s*\*\s*(\w+)\)', effect_str)
    if linear_match:
        target_var = linear_match.group(1)
        increment = float(linear_match.group(2))
        source_var = linear_match.group(3)
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            return {
                'effectType': EFFECT_MAP['LinearScaleByVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': increment,
                'useSourceVariable': 1
            }
    
    # Handle variable multiplication in add: += (50 * Variable) or += (Variable * 50) or += 50 * Variable
    var_mult_match = re.search(r'(\w+)\s*\+=\s*\(?\s*(\d+\.?\d*)\s*\*\s*(\w+)\s*\)?', effect_str)
    if var_mult_match:
        target_var = var_mult_match.group(1)
        multiplier = float(var_mult_match.group(2))
        source_var = var_mult_match.group(3)
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            return {
                'effectType': EFFECT_MAP['AddToVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': multiplier,
                'useSourceVariable': 1
            }
    
    # Also try reversed order: += (Variable * 50)
    var_mult_rev = re.search(r'(\w+)\s*\+=\s*\(?\s*(\w+)\s*\*\s*(\d+\.?\d*)\s*\)?', effect_str)
    if var_mult_rev:
        target_var = var_mult_rev.group(1)
        source_var = var_mult_rev.group(2)
        multiplier = float(var_mult_rev.group(3))
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            return {
                'effectType': EFFECT_MAP['AddToVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': multiplier,
                'useSourceVariable': 1
            }
    
    # Handle variable subtraction: -= (50 * Variable) or -= (Variable * 50)
    var_sub_match = re.search(r'(\w+)\s*-=\s*\(?\s*(\d+\.?\d*)\s*\*\s*(\w+)\s*\)?', effect_str)
    if var_sub_match:
        target_var = var_sub_match.group(1)
        multiplier = float(var_sub_match.group(2))
        source_var = var_sub_match.group(3)
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            return {
                'effectType': EFFECT_MAP['SubtractFromVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': multiplier,
                'useSourceVariable': 1
            }
    
    # Reversed order for subtract: -= (Variable * 50)
    var_sub_rev = re.search(r'(\w+)\s*-=\s*\(?\s*(\w+)\s*\*\s*(\d+\.?\d*)\s*\)?', effect_str)
    if var_sub_rev:
        target_var = var_sub_rev.group(1)
        source_var = var_sub_rev.group(2)
        multiplier = float(var_sub_rev.group(3))
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            return {
                'effectType': EFFECT_MAP['SubtractFromVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': multiplier,
                'useSourceVariable': 1
            }
    
    # Handle multiply with variable: *= 2 * Variable (Greed pattern)
    mult_var_match = re.search(r'(\w+)\s*\*=\s*(\d+\.?\d*)\s*\*\s*(\w+)', effect_str)
    if mult_var_match:
        target_var = mult_var_match.group(1)
        base_mult = float(mult_var_match.group(2))
        source_var = mult_var_match.group(3)
        
        if target_var in VARIABLE_MAP and source_var in VARIABLE_MAP:
            # This needs ExponentialScaleByVariable with base = base_mult
            return {
                'effectType': EFFECT_MAP['ExponentialScaleByVariable'],
                'targetVariable': VARIABLE_MAP[target_var],
                'sourceVariable': VARIABLE_MAP[source_var],
                'sourceValue': base_mult,
                'useSourceVariable': 1
            }
    
    # Handle simple multiply: SetAsideScore *= 2
    if '*=' in effect_str:
        parts = effect_str.split('*=')
        if len(parts) == 2:
            target_var = parts[0].strip()
            value_str = parts[1].strip()
            
            if target_var in VARIABLE_MAP:
                try:
                    value = float(value_str)
                    return {
                        'effectType': EFFECT_MAP['MultiplyVariable'],
                        'targetVariable': VARIABLE_MAP[target_var],
                        'sourceVariable': 0,
                        'sourceValue': value,
                        'useSourceVariable': 0
                    }
                except ValueError:
                    pass
    
    # Handle simple add: SetAsideScore += 50
    if '+=' in effect_str:
        parts = effect_str.split('+=')
        if len(parts) == 2:
            target_var = parts[0].strip()
            value_str = parts[1].strip()
            
            if target_var in VARIABLE_MAP:
                try:
                    value = float(value_str)
                    return {
                        'effectType': EFFECT_MAP['AddToVariable'],
                        'targetVariable': VARIABLE_MAP[target_var],
                        'sourceVariable': 0,
                        'sourceValue': value,
                        'useSourceVariable': 0
                    }
                except ValueError:
                    pass
    
    # Handle simple subtract: Lives -= 1
    if '-=' in effect_str:
        parts = effect_str.split('-=')
        if len(parts) == 2:
            target_var = parts[0].strip()
            value_str = parts[1].strip()
            
            if target_var in VARIABLE_MAP:
                try:
                    value = float(value_str)
                    return {
                        'effectType': EFFECT_MAP['SubtractFromVariable'],
                        'targetVariable': VARIABLE_MAP[target_var],
                        'sourceVariable': 0,
                        'sourceValue': value,
                        'useSourceVariable': 0
                    }
                except ValueError:
                    pass
    
    return None


def generate_ability_yaml(ability_name, trigger_num, conditions, effect, description):
    """Generate YAML for a single ability."""
    
    # Generate conditions YAML
    conditions_yaml = ""
    if conditions:
        for cond in conditions:
            conditions_yaml += f"""    - leftVariable: {cond['leftVariable']}
      leftVariableValue: 0
      comparator: {cond['comparator']}
      rightVariable: {cond.get('rightVariable', 0)}
      rightVariableValue: {cond['rightValue']}
      useRightVariable: {cond.get('useRightVariable', 0)}
"""
    else:
        conditions_yaml = "    []\n"
    
    # Generate effect YAML
    if effect:
        effect_yaml = f"""    - effectType: {effect['effectType']}
      targetVariable: {effect['targetVariable']}
      sourceVariable: {effect.get('sourceVariable', 0)}
      sourceValue: {effect['sourceValue']}
      useSourceVariable: {effect.get('useSourceVariable', 0)}
      retriggerCount: 0
"""
    else:
        # Placeholder effect
        effect_yaml = """    - effectType: 0
      targetVariable: 8
      sourceVariable: 0
      sourceValue: 0
      useSourceVariable: 0
      retriggerCount: 0
"""
    
    ability_yaml = f"""  - abilityName: {ability_name}
    abilityDescription: {description}
    trigger: {trigger_num}
    abilityAnimation: {{fileID: 0}}
    conditions:
{conditions_yaml}    effects:
{effect_yaml}    heldVariable: 0
"""
    
    return ability_yaml


def generate_asset_file(dice_name, ability_name, trigger, conditions_str, effect_str, description):
    """Generate Unity .asset file content."""
    
    dice_type_num = DICE_TYPE_MAP.get(dice_name, 0)
    trigger_num = TRIGGER_MAP.get(trigger, 0)
    
    # Parse conditions (may return multiple sets for OR conditions)
    condition_sets = parse_conditions(conditions_str)
    
    # Parse effect
    effect = parse_effect(effect_str)
    
    # Generate pip sprites section
    pip_sprites = ""
    for i in range(6):
        pip_sprites += f"  - {{fileID: {DEFAULT_PIP_FILEIDS[i]}, guid: {DEFAULT_PIP_GUIDS[i]}, type: 3}}\n"
    
    # Generate abilities (multiple if OR conditions)
    abilities_yaml = ""
    for conditions in condition_sets:
        abilities_yaml += generate_ability_yaml(ability_name, trigger_num, conditions, effect, description)
    
    asset_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 8b102ea77a61d4d48ba984471d37322b, type: 3}}
  m_Name: {dice_name}Dice
  m_EditorClassIdentifier: 
  diceType: {dice_type_num}
  diceName: {dice_name}
  description: {description}
  diceSprite: {{fileID: 0}}
  pipSprites:
{pip_sprites}  canChangeFaces: 0
  customPips: 
  abilities:
{abilities_yaml}"""
    
    return asset_content, effect is not None


def main():
    csv_file = input("Enter CSV file path (or press Enter for 'Farkel_Sheets_-_Dice__1_.csv'): ").strip()
    if not csv_file:
        csv_file = "Farkel_Sheets_-_Dice__1_.csv"
    
    output_dir = input("Enter output directory (or press Enter for 'DiceAssets'): ").strip()
    if not output_dir:
        output_dir = "DiceAssets"
    
    # Create output directory
    try:
        os.makedirs(output_dir, exist_ok=True)
    except PermissionError:
        print(f"❌ Permission denied creating '{output_dir}'")
        print(f"   Try:")
        print(f"   1. Run as administrator")
        print(f"   2. Use a different output path (e.g., C:\\Users\\YourName\\Documents\\DiceAssets)")
        print(f"   3. Delete/rename existing '{output_dir}' folder if it exists")
        return
    except Exception as e:
        print(f"❌ Error creating output directory: {e}")
        return
    
    success_count = 0
    partial_count = 0
    skip_count = 0
    
    # Read CSV
    with open(csv_file, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        for row in reader:
            dice_name = row['name'].strip()
            ability_name = row['ability name'].strip()
            trigger = row.get('Trigger', '').strip()
            conditions = row.get('Conditions', '').strip()
            effect = row.get('Effect', '').strip()
            
            # Skip empty rows
            if not dice_name or not ability_name:
                continue
            
            # Use OnSetAside as default if no trigger specified
            if not trigger:
                trigger = "OnSetAside"
                print(f"⚠️  {dice_name}: No trigger specified, defaulting to OnSetAside")
            
            # Use ability name as description
            description = ability_name
            
            # Check if we can parse this
            if not effect or effect.strip() == "":
                print(f"⚠️  {dice_name}: No effect specified, creating template only")
                skip_count += 1
                asset_content = generate_asset_file(dice_name, ability_name, trigger, "", "", description)[0]
            else:
                # Try to generate with parsing
                asset_content, parsed_effect = generate_asset_file(
                    dice_name, 
                    ability_name,
                    trigger,
                    conditions,
                    effect,
                    description
                )
                
                if parsed_effect:
                    success_count += 1
                    print(f"✅ {dice_name}: Fully parsed")
                else:
                    partial_count += 1
                    print(f"⚠️  {dice_name}: Could not parse effect - needs manual setup")
                    print(f"   Effect: {effect}")
            
            # Create folder for this dice
            dice_folder = os.path.join(output_dir, dice_name)
            try:
                os.makedirs(dice_folder, exist_ok=True)
            except Exception as e:
                print(f"❌ {dice_name}: Failed to create folder - {e}")
                continue
            
            # Write to file
            filename = f"{dice_name}Dice.asset"
            filepath = os.path.join(dice_folder, filename)
            
            try:
                with open(filepath, 'w', encoding='utf-8') as asset_file:
                    asset_file.write(asset_content)
            except Exception as e:
                print(f"❌ {dice_name}: Failed to write file - {e}")
                continue
    
    total = success_count + partial_count + skip_count
    print(f"\n🎉 Done! Created {total} dice assets:")
    print(f"   ✅ {success_count} fully parsed")
    print(f"   ⚠️  {partial_count} need manual effect setup")
    print(f"   ⚠️  {skip_count} templates only (no effect specified)")


if __name__ == "__main__":
    main()
