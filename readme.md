[Join the APS Discord!!](https://discord.com/invite/ErZcKaQ "Animation Prep Studio Discord Server (for Support and Discussion)")

# Animation Prep Studio (Prop Builder) V2.2.1

This project contains tools which help automate the process of converting .blend models into prop assets compatible with [Animation Prep Studio (Lite)](https://drive.google.com/open?id=17MyFQ75dfBuaf5IL4ba-4BH8klWj6-5r "Animation Prep Studio Direct Download"). The builder tool can import .blend files which were created using blender [version 2.79b](https://download.blender.org/release/Blender2.79/ "Blender Downloads"). After successful import there will be a new asset folder, then to make your item available in the game simply drag and drop the asset folder into the `VR_MocapAssets` folder located at:

`%USERPROFILE%\appdata\LocalLow\Animation Prep Studios\AnimationPrepStudio_Lite\VR_MocapAssets`

___
## Getting Started

![Test Image 4](https://raw.githubusercontent.com/guiglass/CustomPropBuilder/master/Documentation/menu.png)

![Test Image 4](https://raw.githubusercontent.com/guiglass/CustomPropBuilder/master/Documentation/builder.png)
* First be sure that the `Blender Application` field points to the valid blender.exe installed on your PC ([version 2.79b](https://download.blender.org/release/Blender2.79/ "Blender Downloads")).
* Then click the "Import Prop Model" button to locate the .blend file containing the model you would like to import.

![Test Image 4](https://raw.githubusercontent.com/guiglass/CustomPropBuilder/master/Documentation/select.png)
* The automation should do most of the work creating the assetbundle.

![Test Image 4](https://raw.githubusercontent.com/guiglass/CustomPropBuilder/master/Documentation/asset.png)
___
Copy the entire folder:

`prop$00000000-0000-0000-0000-000000000000$screwdriver`

And paste it into:

`%USERPROFILE%\appdata\LocalLow\Animation Prep Studios\AnimationPrepStudio_Lite\VR_MocapAssets`
___
### Installing

It is recommended to open this project using [Unity 2019.2.4f1](https://unity3d.com/unity/whats-new/2019.2.4 "Unity Engine Download").
You will also require [Blender 2.79b](https://download.blender.org/release/Blender2.79/ "Blender Downloads") to be installed.

Start Unity hub and navigate to this project, then select the `project` directory to open the project. Then load `Builder.unity`

![Test Image 4](https://raw.githubusercontent.com/guiglass/CustomPropBuilder/master/Documentation/scene.png)

## License

This project is licensed under the GNU GENERAL PUBLIC LICENSE - see the [LICENSE.md](LICENSE.md) file for details
