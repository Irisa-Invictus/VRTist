# VRtist

This is an extended version of VRtist by Université de Rennes 1.
We aim to add animation tools to help creating and editing animations.

In this project you will find an animation tool (currently replacing the gun tool in the palette).
This tool give options to move a character joints and edit animation curves.
Currently only characters from Mixamo.com are meant to be imported.

**Known issues**: 
- Some textures might not be identical between runs.
- Projects from previous version can not be imported
- This version has not been tested with Mixer.


**Disclaimer**: This project is in alpha state and actively developed. Do not use it to edit your production assets without a backup or you might break them.

## Introduction

VRtist is a Virtual Reality tool for storytelling.

Directors and artists can setup a 3D scene being immersed into the virtual world. That will allow them to have a better understanding of the 3D scene.


## Documentation

- User documentation: https://ubisoft-vrtist.readthedocs.io/en/latest/#
- Youtube channel: https://www.youtube.com/channel/UCD4bfXzGNI7prl01Z2BfFfw

## Main Features

- Set dressing: import 3D objects and compose the virtual set.
- Camera: naturally move around and find the best camera angles and depth of field.
- Animation: use the record mode or key frames to create animations.
- Lighting: use gradient sky or fine-tune the lighting using lights (directional, point and cone).
- Nonlinear editing: nonlinear editing using multi-camera footages.
- Live link: Blender and VRtist scene live link.

## Supported VR Devices

For now, VRtist has been tested with the Valve Index, HP Reverb G2, Oculus Quest 2, Oculus Rift S and Oculus Quest devices.
HTC vive wand controllers will not be supported.

## How to Install & Launch

Just unzip the release file. Then launch the VRtist.exe and put your headset.

## How to Build

VRtist is using Unity version: 2020.3.33

For now, VRtist has been tested on Windows 64bit only. Build the "Main" scene in the Unity editor:

- Platform: PC, Mac & Linux Standalone
- Target Platform: Windows
- Architecture: x86_64

### Dependencies

VRtist uses the following libraries as DLLs:

- Assimp: http://www.assimp.org
- OpenImageIO: http://www.openimageio.org

### Settings & Logs

On Windows OS, VRtist writes settings, logs and saves to the %userprofile%/AppData/LocalLow/Ubisoft/VRtist/ directory (Unity.Application.persistentDataPath).

## Asset Bank

VRtist is ditributed with a predefined set of 3D objects.
It also supports FBX files import from a specified directory (default: D:\VRtistData). This can be overriden in the advanced settings.
FBX files may be exported from Blender using the following options:

- scale: 0.01
- Y Forward
- Z Up
- Apply Unit: unchecked
- Apply Transform: checked is advised

## License and copyright

The original code is Copyright (C) 2021 Ubisoft.

All code of the VRtist project is under the MIT license.

All 3D models (FBX files or Unity prefabs) are licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License. To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-nd/4.0/ or send a letter to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.

## Acknowledgements

This work is part of the INVICTUS project that has received funding from the European Union’s Horizon 2020 research and innovation programme under grant agreement No 952147. 