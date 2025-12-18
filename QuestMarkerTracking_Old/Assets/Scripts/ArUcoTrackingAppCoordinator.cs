// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using PassthroughCameraSamples;
using UnityEngine.UI;

namespace TryAR.MarkerTracking
{
    /// <summary>
    /// Coordinates the AR marker tracking application, handling camera initialization,
    /// marker detection, and visualization management.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MarkerTracking")]
    public class ArUcoTrackingAppCoordinator : MonoBehaviour
    {
        /// <summary>
        /// Serializable class for mapping marker IDs to GameObjects in the Inspector.
        /// </summary>
        [Serializable]
        public class MarkerGameObjectPair
        {
            /// <summary>
            /// The unique ID of the AR marker to track.
            /// </summary>
            public int markerId;
            
            /// <summary>
            /// The GameObject to associate with this marker.
            /// </summary>
            public GameObject gameObject;
        }

        [Header("Camera Texture View")]
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;
        private Vector2Int CameraResolution => m_webCamTextureManager.RequestedResolution;
        [SerializeField] private Transform m_cameraAnchor;
   
        [SerializeField] private Canvas m_cameraCanvas;
        [SerializeField] private RawImage m_resultRawImage;
        [SerializeField] private float m_canvasDistance = 1f;

        [Header("Marker Tracking")]
        [SerializeField] private ArUcoMarkerTracking m_arucoMarkerTracking;
        [SerializeField, Tooltip("List of marker IDs mapped to their corresponding GameObjects")]
        private List<MarkerGameObjectPair> m_markerGameObjectPairs = new List<MarkerGameObjectPair>();
        private Dictionary<int, GameObject> m_markerGameObjectDictionary = new Dictionary<int, GameObject>();
        private bool m_showCameraCanvas = true;

        private Texture2D m_resultTexture;

        /// <summary>
        /// Initializes the camera, permissions, and marker tracking system.
        /// </summary>
        private IEnumerator Start()
        {
            // Validate required components
            if (m_webCamTextureManager == null)
            {
                Debug.LogError($"PCA: {nameof(m_webCamTextureManager)} field is required " +
                            $"for the component {nameof(ArUcoTrackingAppCoordinator)} to operate properly");
                enabled = false;
                yield break;
            }

            // Wait for camera permissions
            Assert.IsFalse(m_webCamTextureManager.enabled);
            yield return WaitForCameraPermission();

            // Initialize camera
            yield return InitializeCamera();

            // Configure UI and tracking components
            ScaleCameraCanvas();
            
            //======================================================================================
            // CORE SETUP: Initialize the marker tracking system with camera parameters
            // This configures the ArUco detection with proper camera calibration values
            // and prepares the marker-to-GameObject mapping dictionary
            //======================================================================================
            InitializeMarkerTracking();
            
            // Set initial visibility states
            m_cameraCanvas.gameObject.SetActive(m_showCameraCanvas);
            SetMarkerObjectsVisibility(!m_showCameraCanvas);
        }

        /// <summary>
        /// Waits until camera permission is granted.
        /// </summary>
        private IEnumerator WaitForCameraPermission()
        {
            while (PassthroughCameraPermissions.HasCameraPermission != true)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Initializes the camera with appropriate resolution and waits until ready.
        /// </summary>
        private IEnumerator InitializeCamera()
        {
            // Set the resolution and enable the camera manager
            m_webCamTextureManager.RequestedResolution = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;
            m_webCamTextureManager.enabled = true;

            // Wait until the camera texture is available
            while(m_webCamTextureManager.WebCamTexture == null)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Updates camera poses, detects markers, and handles input for toggling visualization mode.
        /// </summary>
        private void Update()
        {
            // Skip if camera or tracking system isn't ready
            if (m_webCamTextureManager.WebCamTexture == null || !m_arucoMarkerTracking.IsReady)
                return;

            // Toggle between camera view and AR visualization on button press
            HandleVisualizationToggle();
            
            // Update tracking and visualization
            UpdateCameraPoses();
            
            //======================================================================================
            // CORE FUNCTIONALITY: Process marker detection and positioning of 3D objects
            // This is where ArUco markers are detected in the camera frame and 3D objects
            // are positioned in the scene according to marker positions
            //======================================================================================
            ProcessMarkerTracking();
        }

        /// <summary>
        /// Handles button input to toggle between camera view and AR visualization.
        /// </summary>
        private void HandleVisualizationToggle()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_showCameraCanvas = !m_showCameraCanvas;
                m_cameraCanvas.gameObject.SetActive(m_showCameraCanvas);
                SetMarkerObjectsVisibility(!m_showCameraCanvas);
            }
        }

        /// <summary>
        /// Performs marker detection and pose estimation.
        /// This is the core functionality that processes camera frames to detect markers
        /// and position virtual objects in 3D space.
        /// </summary>
        private void ProcessMarkerTracking()
        {
            // Step 1: Detect ArUco markers in the current camera frame
            m_arucoMarkerTracking.DetectMarker(m_webCamTextureManager.WebCamTexture, m_resultTexture);
            
            // Step 2: Estimate the pose of markers and position 3D objects accordingly
            // This maps the 2D marker positions to 3D space using the camera parameters
            m_arucoMarkerTracking.EstimatePoseCanonicalMarker(m_markerGameObjectDictionary, m_cameraAnchor);
        }

        /// <summary>
        /// Toggles the visibility of all marker-associated GameObjects in the dictionary.
        /// </summary>
        /// <param name="isVisible">Whether the marker objects should be visible or not.</param>
        private void SetMarkerObjectsVisibility(bool isVisible)
        {
            // Toggle visibility for all GameObjects in the marker dictionary
            foreach (var markerObject in m_markerGameObjectDictionary.Values)
            {
                if (markerObject != null)
                {
                    var rendererList = markerObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var meshRenderer in rendererList)
                    {
                        meshRenderer.enabled = isVisible;
                    }
                }
            }
        }
    
        /// <summary>
        /// Initializes the marker tracking system with camera parameters and builds the marker dictionary.
        /// This method configures the ArUco marker detection system with the correct camera parameters
        /// for accurate pose estimation.
        /// </summary>
        private void InitializeMarkerTracking()
        {
            // Step 1: Set up camera parameters for tracking
            // These intrinsic parameters are essential for accurate marker pose estimation
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye);
            var cx = intrinsics.PrincipalPoint.x;  // Principal point X (optical center)
            var cy = intrinsics.PrincipalPoint.y;  // Principal point Y (optical center)
            var fx = intrinsics.FocalLength.x;     // Focal length X
            var fy = intrinsics.FocalLength.y;     // Focal length Y
            var width = intrinsics.Resolution.x;   // Image width
            var height = intrinsics.Resolution.y;  // Image height
            
            // Initialize the ArUco tracking with camera parameters
            m_arucoMarkerTracking.Initialize(width, height, cx, cy, fx, fy);
            
            // Step 2: Build marker dictionary from serialized list
            // This maps marker IDs to the GameObjects that should be positioned at each marker
            BuildMarkerDictionary();
            
            // Step 3: Set up texture for visualization
            ConfigureResultTexture(width, height);
        }

        /// <summary>
        /// Builds the dictionary mapping marker IDs to GameObjects.
        /// </summary>
        private void BuildMarkerDictionary()
        {
            m_markerGameObjectDictionary.Clear();
            foreach (var pair in m_markerGameObjectPairs)
            {
                if (pair.gameObject != null)
                {
                    m_markerGameObjectDictionary[pair.markerId] = pair.gameObject;
                }
            }
        }

        /// <summary>
        /// Configures the texture for displaying camera and tracking results.
        /// </summary>
        /// <param name="width">Width of the camera resolution</param>
        /// <param name="height">Height of the camera resolution</param>
        private void ConfigureResultTexture(int width, int height)
        {
            int divideNumber = m_arucoMarkerTracking.DivideNumber;
            m_resultTexture = new Texture2D(width/divideNumber, height/divideNumber, TextureFormat.RGB24, false);
            m_resultRawImage.texture = m_resultTexture;
        }

        /// <summary>
        /// Calculates the dimensions of the canvas based on the distance from the camera origin and the camera resolution.
        /// </summary>
        private void ScaleCameraCanvas()
        {
            var cameraCanvasRectTransform = m_cameraCanvas.GetComponentInChildren<RectTransform>();
            
            // Calculate field of view based on camera parameters
            var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(0, CameraResolution.y / 2));
            var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(CameraResolution.x, CameraResolution.y / 2));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;
            
            // Calculate canvas size to match camera view
            var newCanvasWidthInMeters = 2 * m_canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        /// <summary>
        /// Updates the positions and rotations of camera-related transforms based on head and camera poses.
        /// </summary>
        private void UpdateCameraPoses()
        {
            // Get current head pose
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();
            
            // Update camera anchor position and rotation
            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CameraEye);
            m_cameraAnchor.position = cameraPose.position;
            m_cameraAnchor.rotation = cameraPose.rotation;

            // Position the canvas in front of the camera
            m_cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_canvasDistance;
            m_cameraCanvas.transform.rotation = cameraPose.rotation;
        }
    }
}
