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
