# Content Management System (CMS) for Unity
A modular and lightweight Content Management System designed for Unity projects. This plugin enables developers to define, visualize, and edit structured game data directly within the Unity Editor through a clean and customizable interface.
Based on [XK's repository](https://github.com/koster/CMS), with bug fixes and an explorer implementation.

## Features
* Entity-based architecture for defining reusable and editable data assets
* Custom Editor windows for data selection, visualization, and editing
* Flexible structure to support different content types (e.g. characters, items, dialogues)
* Search and filtering tools for quickly locating specific entries
* Well-organized runtime and editor separation (Runtime/, Editor/)

## Installation
1. Download [latest release](https://github.com/megurte/ContentManagementSystem/releases/tag/1.0.0) and install to your project as package
2. Package already contains SerializeReferenceExtensions `https://github.com/mackysoft/Unity-SerializeReferenceExtensions`. If you already have it, exclude mackysoft's files import
3. Create inside of **Resource** folder **CMS** directory to fetch data from there 

## Usage

### Creating Entities
Define new entities that represent your data structure, such as characters, items, or dialogs.  
Each entity is a data model that can be edited via the provided editor UI.

CMS provides you:
* **CMSEntiry** - definition of game entity via code 
* **CMSEntiryPfb** - ScriptableObject-like model data which saves data in prefabs

### Example
![image](https://github.com/user-attachments/assets/722b7989-fa07-4a5b-86d0-0cc3573b486c)

### Editing Data
Use the provided editor windows to add, remove, and modify entity data.  
The UI supports live filtering and search to navigate large datasets efficiently.

![image](https://github.com/user-attachments/assets/6d6a4997-a50b-475a-a8ef-8377b716d1cd)

## Game Integration

Access and load your CMS data at runtime using the provided API.  
This allows you to dynamically load and use content in your game based on CMS definitions.

## Code Exanples

### Entiry component definition
```csharp
    [Serializable]
    public class TagStats : EntityComponentDefinition
    {
        public int healthVal;
        public int damageVal;
    }
```
### New CMS entiry difinition via code 
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
var bossEnemy = CMS.GetAll<CMSEntity>().FirstOrDefault(ent => ent.Is<TagBossHard>();

var tier1Cards = CMS.GetAll<CMSEntity>().Where(ent => ent.Get<TagRarity>().rarity == CardRarity.Tier1).ToList();

var concreteDataModel = CMS.Get<CMSEntity>(id); // ID is a path to your data model that shows in CMSEntiryPfb component

if (bossEnemy.Is<TagSampleBehaviour>(out var behav)
  behav.Initialize();
```

## Project Structure

- `Editor/`: Custom editor scripts for UI and data interaction via Unity Editor
- `Runtime/`: Core logic and runtime-accessible data definitions
