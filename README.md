# Content Management System (CMS) for Unity
<p align="left">
<a>
  <img alt="Made With Unity" src="https://img.shields.io/badge/made%20with-Unity-57b9d3.svg?logo=Unity">
</a>

<a href="https://github.com/megurte/ContentManagementSystem/releases/latest">
  <img alt="Last Release" src="https://img.shields.io/github/v/release/megurte/ContentManagementSystem?include_prereleases&logo=Dropbox&color=yellow">
</a>

<a href="https://github.com/megurte/ContentManagementSystem/stargazers">
  <img alt="GitHub stars" src="https://img.shields.io/github/stars/megurte/ContentManagementSystem?branch=main&label=Stars&logo=GitHub&logoColor=ffffff&labelColor=282828&color=informational&style=flat">
</a>
</p>

A modular and lightweight Content Management System designed for Unity projects. This plugin enables developers to define, visualize, and edit structured game data directly within the Unity Editor through a clean and customizable interface.
Based on [XK's repository](https://github.com/koster/CMS), with bug fixes and an explorer implementation.

## Features
* Entity-based architecture to define reusable and editable data assets
* Custom Editor windows for data selection, visualization, and editing
* Flexible structure to support different content types (e.g. characters, items, dialogues)
* Search and filtering tools for quickly locating specific entries
* Well-organized runtime and editor separation (Runtime/, Editor/)
* Navigation on CMSEntityPfb searching
* Component-list entity inspector with per-component search, reordering and script shortcuts
* Copy, paste and duplicate for `[SerializeReference]` components, nested graphs included
* Content validation in one menu click (`CMS -> Validate`)
  
<img width="370" height="233" alt="image" src="https://github.com/user-attachments/assets/fcd741e9-d7d5-4e23-b0b4-fbf4a1a4e40d" />

## Installation
1. Download the `.unitypackage` from the [latest release](https://github.com/megurte/ContentManagementSystem/releases/latest) and import it into your project 
2. Package already contains SerializeReferenceExtensions `https://github.com/mackysoft/Unity-SerializeReferenceExtensions`. If you already have it, exclude the import of Mackysoft’s files
3. Create a **CMS** directory inside **Assets/Resources** — that exact path is what CMS loads from
4. Create new game object on scene and add new component **CMSEntityPfb**
5. Save this game object as prefab to **CMS** folder
6. Update it's ID by using menu **CMS -> Auto-Fill IDs**

Or use UPM installation:
1. Open Unity Package Manager (`Window > Package Manager`).
2. Click `+` > `Add package from git URL`.
3. Paste link to package: 

```
https://github.com/megurte/ContentManagementSystem.git?path=/src#1.6.0
```

## Usage

### Creating Entities
Define new entities that represent your data structure, such as characters, items, or dialogs.  
Each entity is a data model that can be edited via the provided editor UI.

To initialize CMS and load all entities use `CMS.Init()` command when game launches before any interaction with CMS. `Init()` is idempotent — call it once, before anything touches CMS. To rebuild the table from scratch use `CMS.Unload()` followed by `CMS.Init()`; the `CMSHelpers.ReloadCMS()` shortcut does exactly that, but it lives in the editor assembly and is not available at runtime.

CMS provides you:
* **CMSEntity** - base class for defining game entities in code
* **CMSEntityPfb** - MonoBehaviour you put on a prefab; it holds the serialized component list that becomes an entity at load time

### Entity ids
An id is what `CMS.Get<T>(id)` takes, and it is assigned for you:

* **prefab entities** — the path under `Assets/Resources` without the extension, so `Assets/Resources/CMS/Enemies/Goblin.prefab` becomes `CMS/Enemies/Goblin`. Move a prefab and the id goes stale until you re-run **CMS -> Auto-Fill IDs**; `CMS -> Validate` reports the mismatch
* **code entities** — the full type name of the class, filled in automatically

`CMS.Get<T>()` throws when an id does not resolve, rather than returning null. Use `CMS.GetAll<T>()` and filter when the entity may legitimately be absent.

### Entity sprite
`GetSprite()` on an entity looks for a `TagSprite` component and returns its sprite; on a prefab it also falls back to a preview rendered from `TagMesh`. This is the same sprite the explorer, the entity dropdown and the inspector header draw, so an entity shows an icon only if it carries one of those tags. `Runtime/CMS/CommonTags.cs` ships `TagSprite`, `TagMesh`, `TagCMSEntity` and `TagListCMSEntity` — reuse them before writing your own.

### Example
![image](https://github.com/user-attachments/assets/722b7989-fa07-4a5b-86d0-0cc3573b486c)

### Editing Data
Use the provided editor windows to add, remove, and modify entity data.  
The UI supports live filtering and search to navigate large datasets efficiently.

![image](https://github.com/user-attachments/assets/6d6a4997-a50b-475a-a8ef-8377b716d1cd)

## Entity Inspector
Selecting a CMSEntityPfb shows a header with its sprite and id, then one card per component:

* Foldout, reorder arrows, remove button, and a button that opens the component script
* A search field above the list filters components by type name, field name or field value
* Components are added from a menu grouped by namespace, not from a raw type dropdown
* Right-click a card to copy, duplicate or paste a component below it

## SerializeReference Clipboard
Any `[SerializeReference]` field or list element can be copied, pasted and duplicated through its
right-click menu. The clipboard keeps the concrete type, so paste is only offered where the field
can actually hold it, and nested managed-reference graphs survive the round trip.

List elements also get paste-below, duplicate and delete, plus an inline delete button and a
shortcut to the script of the currently selected type.

## CMS Entity Explorer
The CMS Explorer provides an in-Editor interface for managing your CMS entities with the following features:

* Add / Delete Entities directly from the tree view
* Right-click menu: open, rename, duplicate, delete, show in project, copy id
* Search matches entity names, ids and component type names, and every row lists its components underneath
* Templates: save any entity as a reusable JSON template and create new entities from it
* Smart folder targeting when creating new prefabs (based on selected entity or folder)

Open it with **CMS -> CMS Entity Explorer** (`Shift+Alt+C`). Shortcuts inside the tree:

| Key | Action |
| --- | --- |
| `Enter` | open the entity in its own inspector window |
| `F2` | rename inline |
| `Ctrl+D` | duplicate, with the id rewritten to the new path |
| `Del` | delete the selection, after a confirmation |
| `Ctrl+Z` | restore the last delete, same asset and same guid |
| `↑` from the top row | jump back to the search field |

Templates are written to `Assets/Resources/CMS/Templates/<name>.json`. Two things to know about them: they are stored under `Resources`, so they end up in a build unless you move or strip that folder, and each component is serialized with `JsonUtility`. That means plain fields survive, but nested `[SerializeReference]` fields and references to assets do not — check a template-made entity before relying on it. The clipboard described above does not have this limitation.

All functionality is integrated into a single streamlined window designed to speed up content iteration and reduce manual asset handling.

![image](https://github.com/user-attachments/assets/f24f7fe8-e1e0-4e4b-90a3-4e9780d16b9b)

## Validation
`CMS -> Validate` walks every entity under `Resources/CMS` in one pass and reports:

* ids that no longer match the asset path, and ids shared by two entities — as errors
* null components left in an entity — as errors
* entities without a sprite — as warnings

Worth running before a build, where a null component or a stale id otherwise surfaces as a null
reference at runtime.


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
### Example of access to entities
```csharp
var bossEnemy = CMS.GetAll<CMSEntity>().FirstOrDefault(ent => ent.Is<TagBossHard>());

var tier1Cards = CMS.GetAll<CMSEntity>().Where(ent => ent.Get<TagRarity>().rarity == CardRarity.Tier1).ToList();

var concreteDataModel = CMS.Get<CMSEntity>(id); // ID is a path to your data model that shows in CMSEntityPfb component

if (bossEnemy.Is<TagSampleBehaviour>(out var behav))
  behav.Initialize();
```
`Is<T>()` and `Get<T>()` need a concrete component type. For an abstract base use `entity.GetAbstract<T>()` or `entity.IsAbstract<T>(out var value)`, and for an interface use `entity.IsInterface<T>(out var value)`. If you want a separate instance use the `DeepCopy()` extension.

Code generation allows you to automatically generate constant paths for all CMS prefabs.
Run **CMS -> Generate Models Constants**; it writes `Assets/GeneratedCodeBase/Constants.Models.cs` with a
`Constants.Models` class, and groups the constants by the first folder under `Resources/CMS/Models/`
(anything outside that path lands in an `Ungrouped` block). Regenerate after adding or moving prefabs
and do not hand-edit the output. After that, you can reference prefabs using strongly-typed constants:

```csharp
using Constants;

var unit = CMS.Get<CMSEntity>(Models.MyNewUnit);
```
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

### Field filtration
To limit the selection in the CMSEntityPfb slot, there is a filtering mechanism based on the types that contain the components of the data model. `FilterTagsAttribute` lives in the `Runtime` namespace, and the filter matches base classes and interfaces too, so filtering by an abstract tag accepts every entity carrying a subclass of it.

```csharp
[Serializable]
public class TagMeleeWeapon: EntityComponentDefinition { }
[Serializable]
public class TagTwoHandSword : EntityComponentDefinition { }

[Serializable]
public class TagEnemyStartEquipment : EntityComponentDefinition
{
    [FilterTags(typeof(TagMeleeWeapon), typeof(TagTwoHandSword))]
    public CMSEntityPfb weapon;
}
```
<img width="561" height="302" alt="image" src="https://github.com/user-attachments/assets/d89b6e4e-6e82-49f2-b757-616f4c70a54b" />

## Editor menu

| Item | What it does |
| --- | --- |
| `CMS -> CMS Entity Explorer` (`Shift+Alt+C`) | opens the explorer window |
| `CMS -> Auto-Fill IDs` | rewrites every entity id from its asset path |
| `CMS -> Validate` | reports id mismatches, duplicate ids, null components, missing sprites |
| `CMS -> Generate Models Constants` | writes `Assets/GeneratedCodeBase/Constants.Models.cs` |
| `CMS -> Reload` | `CMS.Unload()` + `CMS.Init()` without leaving the editor |

Ids are auto-filled before a **WebGL** build by a build preprocessor. Other build targets are left
alone, so run `CMS -> Auto-Fill IDs` (or `CMS -> Validate`) yourself before shipping on them.

## Feedback

Feel free to open an [Issue](https://github.com/megurte/ContentManagementSystem/issues) if you encounter bugs or have suggestions.
