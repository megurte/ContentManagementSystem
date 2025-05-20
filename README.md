# Content Management System (CMS) for Unity
A modular and lightweight Content Management System designed for Unity projects. This plugin enables developers to define, visualize, and edit structured game data directly within the Unity Editor through a clean and customizable interface.
Based on [XK's repository](https://github.com/koster/CMS), with bug fixes and an explorer implementation.

## Features
* Entity-based architecture to define reusable and editable data assets
* Custom Editor windows for data selection, visualization, and editing
* Flexible structure to support different content types (e.g. characters, items, dialogues)
* Search and filtering tools for quickly locating specific entries
* Well-organized runtime and editor separation (Runtime/, Editor/)

## Installation
1. Download [latest release](https://github.com/megurte/ContentManagementSystem/releases/latest) and install to your project as package 
2. Package already contains SerializeReferenceExtensions `https://github.com/mackysoft/Unity-SerializeReferenceExtensions`. If you already have it, exclude the import of Mackysoft’s files
3. Create inside of **Resource** folder **CMS** directory to fetch data from there

Or use UPM installation:
1. Open Unity Package Manager (`Window > Package Manager`).
2. Click `+` > `Add package from git URL`.
3. Paste link to package: 

```
https://github.com/megurte/ContentManagementSystem.git?path=/src#1.3.0
```

## Usage

### Creating Entities
Define new entities that represent your data structure, such as characters, items, or dialogs.  
Each entity is a data model that can be edited via the provided editor UI.

CMS provides you:
* **CMSEntity** - base class for defining game entities in code
* **CMSEntityPfb** - ScriptableObject that holds serialized data and prefab-based definitions

### Example
![image](https://github.com/user-attachments/assets/722b7989-fa07-4a5b-86d0-0cc3573b486c)

### Editing Data
Use the provided editor windows to add, remove, and modify entity data.  
The UI supports live filtering and search to navigate large datasets efficiently.

![image](https://github.com/user-attachments/assets/6d6a4997-a50b-475a-a8ef-8377b716d1cd)

## Game Integration

Access and load your CMS data at runtime using the provided API.  
This allows you to dynamically load and use content in your game based on CMS definitions.

## Code Examples

### Entity component definition
```csharp
[Serializable]
public class TagStats : EntityComponentDefinition
{
    public int healthVal;
    public int damageVal;
}
```
### New CMS entity definition via code 
```csharp
[Serializable]
public class CharacterEntity : CMSEntity
{
    public CharacterEntity()
    {
        Define<TagName>().loc = "Astolfo";
        Define<TagCost>().soulMana = 3;
        Define<TagStats>().damageVal = 3;
        Define<TagAbilityHealAtEndTurn>();
    }
}
```
### Example of access to entiries
```csharp
var bossEnemy = CMS.GetAll<CMSEntity>().FirstOrDefault(ent => ent.Is<TagBossHard>());

var tier1Cards = CMS.GetAll<CMSEntity>().Where(ent => ent.Get<TagRarity>().rarity == CardRarity.Tier1).ToList();

var concreteDataModel = CMS.Get<CMSEntity>(id); // ID is a path to your data model that shows in CMSEntiryPfb component

if (bossEnemy.Is<TagSampleBehaviour>(out var behav)
  behav.Initialize();
```

### Example of logic component definition
```csharp
    [Serializable]
    public abstract class OpponentAI : EntityComponentDefinition
    {
        public abstract void TurnStartReasoning(CharacterState state, CharacterState target);
    }

    [Serializable]
    public class TagCommonEnemyBehaviour : OpponentAI
    {
        public override void TurnStartReasoning(CharacterState state, CharacterState target)
        {
            foreach (var dice in state.diceStates)
            {
                var randomTargets = target.diceStates[Random.Range(0, target.diceStates.Count)];
                var cardToPlay = state.availableCards[Random.Range(0, state.availableCards.Count)];
                dice.TargetDice = randomTargets;
                dice.CardToPlay = cardToPlay;
                dice.view.As<DiceView>().SetReady(true);
            }
        }
    }
```

## Project Structure

- `Editor/`: Custom editor scripts for UI and data interaction via Unity Editor
- `Runtime/`: Core logic and runtime-accessible data definitions

## Feedback

Feel free to open an [Issue](https://github.com/megurte/ContentManagementSystem/issues) if you encounter bugs or have suggestions.
