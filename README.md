# Content Management System (CMS) for Unity
A modular and lightweight Content Management System designed for Unity projects. This plugin enables developers to define, visualize, and edit structured game data directly within the Unity Editor through a clean and customizable interface.
Based on [XK's repository](https://github.com/koster/CMS), with bug fixes and an explorer implementation.

## Features
* Entity-based architecture to define reusable and editable data assets
* Custom Editor windows for data selection, visualization, and editing
* Flexible structure to support different content types (e.g. characters, items, dialogues)
* Search and filtering tools for quickly locating specific entries
* Well-organized runtime and editor separation (Runtime/, Editor/)
* Navigation on CMSEntityPfb searching
  
<img width="370" height="233" alt="image" src="https://github.com/user-attachments/assets/fcd741e9-d7d5-4e23-b0b4-fbf4a1a4e40d" />

## Installation
1. Download [latest release](https://github.com/megurte/ContentManagementSystem/releases/latest) and install to your project as package 
2. Package already contains SerializeReferenceExtensions `https://github.com/mackysoft/Unity-SerializeReferenceExtensions`. If you already have it, exclude the import of Mackysoft’s files
3. Create inside of **Resource** folder **CMS** directory to fetch data from there

Or use UPM installation:
1. Open Unity Package Manager (`Window > Package Manager`).
2. Click `+` > `Add package from git URL`.
3. Paste link to package: 

```
https://github.com/megurte/ContentManagementSystem.git?path=/src#1.5.0
```

## Usage

### Creating Entities
Define new entities that represent your data structure, such as characters, items, or dialogs.  
Each entity is a data model that can be edited via the provided editor UI.

To initialize CMS and load all entities use `CMS.Init()` command when game launches before any interaction with CMS. Or use `CMSHelpers.ReloadCMS()` to complitely realod data in CMS.

CMS provides you:
* **CMSEntity** - base class for defining game entities in code
* **CMSEntityPfb** - ScriptableObject that holds serialized data and prefab-based definitions

### Example
![image](https://github.com/user-attachments/assets/722b7989-fa07-4a5b-86d0-0cc3573b486c)

### Editing Data
Use the provided editor windows to add, remove, and modify entity data.  
The UI supports live filtering and search to navigate large datasets efficiently.

![image](https://github.com/user-attachments/assets/6d6a4997-a50b-475a-a8ef-8377b716d1cd)

## CMS Entity Explorer
The CMS Explorer provides an in-Editor interface for managing your CMS entities with the following features:

* Add / Delete Entities directly from the tree view
* Rename Entities inline (F2 support)
* Templates: save any entity as a reusable JSON-based template and instantiate new entities from it with all component data preserved
* Smart folder targeting when creating new prefabs (based on selected entity or folder)

All functionality is integrated into a single streamlined window designed to speed up content iteration and reduce manual asset handling.

![image](https://github.com/user-attachments/assets/f24f7fe8-e1e0-4e4b-90a3-4e9780d16b9b)


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
In case of abstraction use `CMS.GetAbstract<T>()` or `CMS.GetInterface<T>()`. If you want seporate instance use DeepCoty() function.

Code generation allows you to automatically generate constant paths for all CMS prefabs.
To regenerate these constants, use the menu option under the CMS tab.
After that, you can reference prefabs using strongly-typed constants: `CMS.Get<CMSEntity>(Models.MyNewUnit);`
<img width="318" height="127" alt="image" src="https://github.com/user-attachments/assets/6703a517-f5b2-46ba-a19e-72e71e48e015" />


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

## Feedback

Feel free to open an [Issue](https://github.com/megurte/ContentManagementSystem/issues) if you encounter bugs or have suggestions.
