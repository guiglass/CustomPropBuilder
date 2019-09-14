# Animation Prep Studio (Prop Builder)

This project contains tools which help automate the process of converting .blend models into prop assets compatible with [Animation Prep Studio](https://drive.google.com/open?id=17MyFQ75dfBuaf5IL4ba-4BH8klWj6-5r "Animation Prep Studio Direct Download"). The builder tool can import .blend files which were created using blender 2.79b. After successful import there will be a new asset folder which you simply drag and drop into the `VR_MocapAssets` folder to make it available in the game.

## Getting Started

![Test Image 4](https://raw.githubusercontent.com/guiglass/PropBuilder/master/Documentation/menu.png)

![Test Image 4](https://raw.githubusercontent.com/guiglass/PropBuilder/master/Documentation/builder.png)
* First be sure that the `Blender Application` field points to the valid blender.exe installed on your PC (V2.79).
* Then click the "Import Prop Model" button to locate the .blend file containing the model you would like to import.

![Test Image 4](https://raw.githubusercontent.com/guiglass/PropBuilder/master/Documentation/select.png)
* The automation should do most of the work creating the assetbundle.

![Test Image 4](https://raw.githubusercontent.com/guiglass/PropBuilder/master/Documentation/asset.png)
___
Copy the entire folder:

`prop$00000000-0000-0000-0000-000000000000$screwdriver`

And paste it into:

`C:\Users\[User]\AppData\LocalLow\Animation Prep Studios\AnimPrep\VR_MocapAssets`
___
### Installing

It is recommended to open this project requires Unity [Unity 2019.2.4f1](https://unity3d.com/unity/whats-new/2019.2.4 "Unity Engine Download") to be installed.
You will also require [Blender 2.79b](https://www.blender.org/download/ "Blender Download") to be installed.

Start Unity hub and navigate to this project, then select the `project` directory to start the scene.

## License

This project is licensed under the GNU GENERAL PUBLIC LICENSE - see the [LICENSE.md](LICENSE.md) file for details