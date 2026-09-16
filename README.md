# Quest ArUco Marker Tracking

This repository enables **single and multi-marker detection and tracking** using OpenCV for Meta Quest 3/3S.  
It provides sample scenes that support both ArUco(Single/Multi) and ChArUco(Single) markers for augmented reality applications on Quest devices.

## Project Structure

The Unity project is located in the **QuestMarkerTracking** folder.  
It uses the **Passthrough Camera Access** component of the Meta XR SDK to obtain the camera image directly.

> **Looking for the WebCamTexture-based version?**  
> The older `QuestMarkerTracking_Old` project has been removed from this repository.  
> If you still need it, it is available in the [v1.0.0 release](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking/releases/tag/v1.0.0).

For a demonstration, check out the following videos:

- **Single Marker Tracking Demo (ArUco)**  
  [![Single Marker Demo](https://img.youtube.com/vi/cJSjYMuJu8w/0.jpg)](https://www.youtube.com/watch?v=cJSjYMuJu8w)

- **Multi-Marker Tracking Demo (ArUco)**  
  [![Multi-Marker Demo](https://img.youtube.com/vi/Y0mqQ_nxve8/0.jpg)](https://www.youtube.com/watch?v=Y0mqQ_nxve8)

- **ChArUco Marker Tracking Demo**  
  [![ChArUco Demo](https://img.youtube.com/vi/NnHkcNXevxs/0.jpg)](https://www.youtube.com/watch?v=NnHkcNXevxs)

---

## Dependencies

⚠ **Important Notice**  
When opening the project for the first time, you will likely encounter errors. This is because **OpenCV for Unity** is not yet installed.  
**Please ignore the errors initially, proceed to open the project, and install OpenCV for Unity manually.**

This project uses **OpenCV for Unity**.  
Please **PURCHASE** and install it from the Unity Asset Store:  
[OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?locale=en-US)

Tested with **OpenCV for Unity v3.0.3**.  
**v3.0.3 or later is required.** OpenCV for Unity renamed its namespaces and class names
(`OpenCVForUnity.UnityUtils` → `OpenCVForUnity.UnityIntegration`, `Utils` → `OpenCVMatUtils`, `ARUtils` → `OpenCVARUtils`),
so this project will **not compile** with older versions.  
If you need to use OpenCV for Unity **v3.0.0 or earlier**, please use the
[v1.0.0 release](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking/releases/tag/v1.0.0)
of this repository, which targets the previous API.

> **Want to try it before purchasing?**  
> A [free trial version](https://github.com/EnoxSoftware/OpenCVForUnity/releases) is available for evaluation.  
> It works **only inside the Unity Editor** and cannot be built to a Quest device, but you can still run the project in the Editor over **Quest Link** and see how the marker tracking actually behaves before you decide to buy.  
> Note that this requires a Meta XR SDK version that supports camera access via Link (see **SDK Requirements** below).  
> To deploy to an actual Quest device, the paid version from the Asset Store is required.

### SDK Requirements
- **Unity**: `6000.3.2f1`
- **Meta XR SDK**: `com.meta.xr.sdk.all` **v205 or later**  
  Required for Passthrough Camera Access. Earlier versions do not allow camera access over **Quest Link**, so v205 or later is also needed to run the project in the Editor.

---

## Usage

### Opening the Project
Open the **QuestMarkerTracking** folder as a Unity project.

### Marker Preparation
To use this project, please download and install the required marker files from the following links:

- **ArUco Markers**: [ArUcoMarker.pdf](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking/blob/main/ArUcoMarker.pdf)  
- **ChArUco Marker**: [ChArUcoMarker.pdf](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking/blob/main/ChArUcoMarker.pdf)

### Unity Scenes
The project contains the following scenes:

- **Camera Access Test**: `0 - CameraAccessTest.unity`  
- **ArUco Marker Tracking (Single & Multi)**: `1 - ArUcoMarkerTracking.unity`  
- **ChArUco Marker Tracking**: `2 - ChArUcoMarkerTracking.unity`

### Default Settings

#### ArUco Marker
- Marker Dictionary: **DICT_4X4_50**
- Marker Size: **0.1m**  
Feel free to modify these settings to suit your needs.

#### ChArUco Marker
- Marker Dictionary: **DICT_4X4_50**
- Board Configuration: **5 x 4 squares**
- Square Size: **0.05m**  
These values are used by default in the ChArUco sample scene. Adjust them as needed.

### View Mode Switching
You can switch the view mode by pressing the **A button** on the controller.

---

### How to Change the Number of ArUco Markers

If you want to track multiple ArUco markers simultaneously or change the number of markers in the scene, follow these steps. 

1. Open the **1 - ArUcoMarkerTracking.unity** scene and select the `ArUcoTrackingAppCoordinator` GameObject.  
2. In the **Inspector**, locate the **ArUcoMarkerTrackingAppCoordinator** component and find the `MarkerGameObjectPairs` array.  
3. Change the size of this array to match the **number of markers** you want to use.  
4. For each **Element**, specify:  
   - **Marker Id**: The ID of the marker you want to detect.  
   - **Game Object**: The GameObject to display on top of the detected marker.  
5. **Important Notes**:  
   - Make sure there are no duplicate Marker IDs across the Elements.  
   - Keep the Marker IDs within the valid range defined by the dictionary you are using (for example, **DICT_4X4_50** allows IDs from `0` to `49`).

Below is an example of how `Marker Id` and `Game Object` pairs are set up in the Inspector:

![fig1](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking/blob/main/Materials/fig1.jpg)

---

## Reference Repositories

This implementation is based on the following sample repositories:

- [Unity-PassthroughCameraApiSamples](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples)
- [QuestCameraKit](https://github.com/xrdevrob/QuestCameraKit)

---

## Contact

If you have any questions, feel free to reach out:

- **X (Twitter)**:  
  - [@Tks_Yoshinaga](https://x.com/Tks_Yoshinaga)  
- **LinkedIn**: [Tks Yoshinaga](https://www.linkedin.com/in/tks-yoshinaga/)  

## Support

If you find this project useful, you can support me via PayPal:  
[![Donate with PayPal](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://paypal.me/TakashiYoshinaga)  
