Outline Effect But It's Faster
======================
This is a fork of [Outline Effect](https://github.com/cakeslice/Outline-Effect) by cakeslice. The primary aim of this fork is to improve performance, but I am also adding a few features and cleaning up code.

IMPROVEMENTS:
------------
* added a checkbox to the OutlineEffect component to disable the automatic enabling of all scene Outlines at startup. ([thank you](https://github.com/cakeslice/Outline-Effect/pull/30) Claytonious)
* don't use GetComponent calls in OutlineEffect.OnPreRender ([thank you](https://github.com/cakeslice/Outline-Effect/pull/38) hobnob)
* only allow there to be one OutlineEffect camera
* enabling and disabling Outline components is much faster and produces much less garbage
* OutlineEffect.OnPreRender produces much less garbage
* OutlineEffect.OnPreRender returns immediately if there are no active Outlines in the scene
* added ability to install project via the Unity Package Manager
* reorganized project folder structure to something nicer
* various code improvements

INSTALLATION:
------------
Download the files and place them anywhere in the Assets folder of your Unity project. Alternatively, you can install it via the Unity Package Manager:

1. open the file `Your Unity Project/Packages/manifest.json`
2. add the following line to the `"dependencies"` array: `"com.jimmycushnie.outline-effect-but-its-faster": "https://github.com/JimmyCushnie/Outline-Effect-but-its-faster.git#unity-package-manager"`
3. open Unity and let it download the package

USAGE:
------------
* Add "Outline Effect" component to camera
* Add "Outline" component to renderers