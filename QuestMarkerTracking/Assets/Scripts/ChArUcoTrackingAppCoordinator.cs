// Copyright (c) Takashi Yoshinaga. All rights reserved.
using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using Meta.XR;

namespace TryAR.MarkerTracking
{
    /// <summary>
    /// Coordinates the AR marker tracking application, handling camera initialization,
    /// marker detection, and visualization management.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MarkerTracking")]
    public class ChArUcoTrackingAppCoordinator : MonoBehaviour
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

        [Header("Passthrough Camera")]
        [SerializeField]
        private PassthroughCameraAccess m_passthroughCameraAccess;
        
        [Header("Marker Tracking")]
        [SerializeField] private ChArUcoMarkerTracking m_charucoMarkerTracking;
        [SerializeField, Tooltip("List of marker IDs mapped to their corresponding GameObjects")]
        private GameObject _arObject;
        [SerializeField] MeshRenderer m_debugRenderer;

        private bool m_showRecogResult = false;
        private Texture2D m_resultTexture;
        private Transform m_cameraAnchor;
   
        /// <summary>
        /// Initializes the camera anchor, camera, and marker tracking system.
        /// </summary>
        private IEnumerator Start()
        {
            if(m_passthroughCameraAccess==null)
            {
                Debug.LogError("PassthroughCameraAccess reference is missing.");
                yield break;
            }

            // Create camera anchor dynamically
            CreateCameraAnchor();

            // Initialize camera
            yield return InitializeCamera();
            
            //======================================================================================
            // CORE SETUP: Initialize the marker tracking system with camera parameters
            // This configures the ArUco detection with proper camera calibration values
            // and prepares the marker-to-GameObject mapping dictionary
            //======================================================================================
            InitializeMarkerTracking();
            
            // Set initial visibility states
            if(m_debugRenderer!=null){
                m_debugRenderer.gameObject.SetActive(m_showRecogResult);
            }
            SetMarkerObjectsVisibility(!m_showRecogResult);
        }

        /// <summary>
        /// Creates a camera anchor GameObject dynamically at runtime.
        /// </summary>
        private void CreateCameraAnchor()
        {
            GameObject anchorObject = new GameObject("CameraAnchor");
            m_cameraAnchor = anchorObject.transform;
        }

        /// <summary>
        /// Initializes the camera with appropriate resolution and waits until ready.
        /// </summary>
        private IEnumerator InitializeCamera()
        {
            while (!m_passthroughCameraAccess.IsPlaying)
            {
                yield return null;
            }
            yield return null; // Wait one frame to ensure camera is fully initialized
        }

        /// <summary>
        /// Updates camera poses, processes marker tracking, and handles input for toggling visualization mode.
        /// </summary>
        private void Update()
        {
            // Skip if camera or tracking system isn't ready
            if(m_passthroughCameraAccess==null || !m_passthroughCameraAccess.IsPlaying || !m_charucoMarkerTracking.IsReady)
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
        /// Handles button input to toggle between recognition result display and AR marker objects.
        /// </summary>
        private void HandleVisualizationToggle()
        {
            if(m_debugRenderer==null)
                return;

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_showRecogResult = !m_showRecogResult;
                m_debugRenderer.gameObject.SetActive(m_showRecogResult);
                SetMarkerObjectsVisibility(!m_showRecogResult);
            }
        }

        /// <summary>
        /// Performs marker detection and pose estimation.
        /// This is the core functionality that processes camera frames to detect markers
        /// and position virtual objects in 3D space.
        /// </summary>
        private void ProcessMarkerTracking()
        {
            // Step 1: Detect ChArUco markers in the current camera frame
            m_charucoMarkerTracking.DetectMarker(m_passthroughCameraAccess.GetTexture(), m_resultTexture);
            
            // Step 2: Estimate the pose of the ChArUco board and position 3D object accordingly
            // This maps the 2D marker positions to 3D space using the camera parameters
            m_charucoMarkerTracking.EstimatePose(_arObject, m_cameraAnchor);
        }

        /// <summary>
        /// Toggles the visibility of the marker-associated GameObject.
        /// </summary>
        /// <param name="isVisible">Whether the marker object should be visible or not.</param>
        private void SetMarkerObjectsVisibility(bool isVisible)
        {
            // Toggle visibility for the AR object
            if (_arObject != null)
            {
                var rendererList = _arObject.GetComponentsInChildren<Renderer>(true);
                foreach (var meshRenderer in rendererList)
                {
                    meshRenderer.enabled = isVisible;
                }
            }   
        }
    
        /// <summary>
        /// Initializes the ChArUco marker tracking system with camera parameters.
        /// </summary>
        private void InitializeMarkerTracking()
        {
            // Step 1: Get camera intrinsic parameters
            // These intrinsic parameters are essential for accurate marker pose estimation
            var intrinsics = m_passthroughCameraAccess.Intrinsics;
            var cx = intrinsics.PrincipalPoint.x;  // Principal point X (optical center)
            var cy = intrinsics.PrincipalPoint.y;  // Principal point Y (optical center)
            var fx = intrinsics.FocalLength.x;     // Focal length X
            var fy = intrinsics.FocalLength.y;     // Focal length Y
            var width = intrinsics.SensorResolution.x;   // Image width
            var height = intrinsics.SensorResolution.y;  // Image height
            
            // Step 2: Scale parameters to match current camera resolution
            var currentResolution = m_passthroughCameraAccess.CurrentResolution;
            Debug.Log($"Camera Intrinsics - fx: {fx}, fy: {fy}, cx: {cx}, cy: {cy}, width: {width}, height: {height}");
            Debug.Log($"Current Camera Resolution - width: {currentResolution.x}, height: {currentResolution.y}");
            
            if (currentResolution.x != width || currentResolution.y != height)
            {
                float scaleX = (float)currentResolution.x / width;
                float scaleY = (float)currentResolution.y / height;
                fx *= scaleX;
                fy *= scaleY;
                cx *= scaleX;
                cy *= scaleY;
                width = currentResolution.x;
                height = currentResolution.y;
            }
      
            // Step 3: Initialize the ChArUco tracking with camera parameters
            m_charucoMarkerTracking.Initialize(width, height, cx, cy, fx, fy);
            
            // Step 4: Set up texture for visualization
            ConfigureResultTexture(width, height);
        }



        /// <summary>
        /// Configures the texture for displaying camera and tracking results.
        /// </summary>
        /// <param name="width">Width of the camera resolution</param>
        /// <param name="height">Height of the camera resolution</param>
        private void ConfigureResultTexture(int width, int height)
        {
            int divideNumber = m_charucoMarkerTracking.DivideNumber;
            m_resultTexture = new Texture2D(width/divideNumber, height/divideNumber, TextureFormat.RGB24, false);
            if (m_debugRenderer != null)
            {
                m_debugRenderer.material.mainTexture = m_resultTexture;
            }
        }

        /// <summary>
        /// Updates the camera anchor position and rotation based on the camera pose.
        /// </summary>
        private void UpdateCameraPoses()
        {
            // Update camera anchor position and rotation
            var cameraPose = m_passthroughCameraAccess.GetCameraPose();
            m_cameraAnchor.position = cameraPose.position;
            m_cameraAnchor.rotation = cameraPose.rotation;
        }
    }
}
