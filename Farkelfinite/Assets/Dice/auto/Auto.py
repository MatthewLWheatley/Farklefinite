import csv
import os

# Trigger type mappings
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

# Default pip sprite GUIDs (from Fire example)
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


def generate_asset_file(dice_name, ability_name, trigger, description):
    """Generate Unity .asset file content as a template."""
    
    dice_type_num = DICE_TYPE_MAP.get(dice_name, 0)
    trigger_num = TRIGGER_MAP.get(trigger, 0)
    
    # Generate pip sprites section
    pip_sprites = ""
    for i in range(6):
        pip_sprites += f"  - {{fileID: {DEFAULT_PIP_FILEIDS[i]}, guid: {DEFAULT_PIP_GUIDS[i]}, type: 3}}\n"
    
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
  - abilityName: {ability_name}
    abilityDescription: {description}
    trigger: {trigger_num}
    abilityAnimation: {{fileID: 0}}
    conditions: []
    effects:
    - effectType: 0
      targetVariable: 8
      sourceVariable: 0
      sourceValue: 0
      useSourceVariable: 0
      retriggerCount: 0
    heldVariable: 0
"""
    
    return asset_content


def main():
    csv_file = input("Enter CSV file path (or press Enter for 'Farkel_Sheets_-_Dice__1_.csv'): ").strip()
    if not csv_file:
        csv_file = "Farkel_Sheets_-_Dice__1_.csv"
    
    output_dir = input("Enter output directory (or press Enter for 'DiceAssets'): ").strip()
    if not output_dir:
        output_dir = "DiceAssets"
    
    # Create output directory
    os.makedirs(output_dir, exist_ok=True)
    
    # Read CSV
    with open(csv_file, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        for row in reader:
            dice_name = row['name'].strip()
            ability_name = row['ability name'].strip()
            trigger = row.get('Trigger', '').strip()
            
            # Skip empty rows
            if not dice_name or not ability_name:
                continue
            
            # Use OnSetAside as default if no trigger specified
            if not trigger:
                trigger = "OnSetAside"
                print(f"⚠️  {dice_name}: No trigger specified, defaulting to OnSetAside")
            
            # Use ability name as description for now
            description = ability_name
            
            # Generate asset file template
            asset_content = generate_asset_file(
                dice_name, 
                ability_name,
                trigger,
                description
            )
            
            # Create folder for this dice
            dice_folder = os.path.join(output_dir, dice_name)
            os.makedirs(dice_folder, exist_ok=True)
            
            # Write to file
            filename = f"{dice_name}Dice.asset"
            filepath = os.path.join(dice_folder, filename)
            
            with open(filepath, 'w', encoding='utf-8') as asset_file:
                asset_file.write(asset_content)
            
            print(f"✅ Created {dice_name}/{filename}")
    
    dice_count = len([d for d in os.listdir(output_dir) if os.path.isdir(os.path.join(output_dir, d))])
    print(f"\n🎉 Done! Created {dice_count} dice assets in '{output_dir}' directory")


if __name__ == "__main__":
    main()