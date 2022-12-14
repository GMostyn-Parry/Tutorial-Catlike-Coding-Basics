# [Tutorial] Catlike Coding - Basics
A repository of [Catlike Coding's basics tutorial series](https://catlikecoding.com/unity/tutorials/basics/ "Catlike Coding's basics tutorial series"); fully completed January 2021.

*Last compiled with Unity version 2019.4.40f1.*

## Project Description

This tutorial series focuses on introducing various features of the Unity engine and updating a large amount of objects using; first using the CPU, then compute shaders, then Unity's job system.

The series covers:
* The Unity Editor & Packages
* Game Objects & Components
* Scripting
* Prefabs
* Shaders & Shader Graph
* Compute shaders
* Unity jobs system & Burst
* Measuring Performance
* Using the Unity profiling tools

## Controls

There is a single scene containing three game objects that manage and animate a large amount of objects:
* Graph - CPU animated graphs.
* GPU Graph - GPU animated graphs.
* Fractal - Animated fractal.

Only one of these objects should be active at a time.

Properties of their animation can be controlled by exported properties in the editor.
