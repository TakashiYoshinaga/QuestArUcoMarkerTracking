using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TryAR.MarkerTracking
{
    /// <summary>
    /// ChArUco marker detection and tracking component.
    /// Handles detection of ChArUco boards in camera frames and provides pose estimation.
    /// </summary>
    public class ChArUcoMarkerTracking : MonoBehaviour
    {
        /// <summary>
        /// The ArUco dictionary to use for marker detection.
        /// </summary>
        [SerializeField] private ArUcoDictionary _dictionaryId = ArUcoDictionary.DICT_4X4_50;

        [Space(10)]

        /// <summary>
        /// The length of the markers' side in meters.
        /// </summary>
        [SerializeField] private float _markerLength = 0.03f;

        /// <summary>
        /// The length of a chessboard square side in meters.
        /// </summary>
        [SerializeField] private float _squareLength = 0.05f;

        /// <summary>
        /// Number of squares in X direction for ChArUco board.
        /// </summary>
        [SerializeField] private int _squaresX = 5;

        /// <summary>
        /// Number of squares in Y direction for ChArUco board.
        /// </summary>
        [SerializeField] private int _squaresY = 4;

        /// <summary>
        /// Minimum number of ChArUco markers needed for detection.
        /// </summary>
        [SerializeField] private int _charucoMinMarkers = 2;

        /// <summary>
        /// Coefficient for low-pass filter (0-1). Higher values mean more smoothing.
        /// </summary>
        [Range(0, 1)]
        [SerializeField] private float _poseFilterCoefficient = 0.5f;

        /// <summary>
        /// Division factor for input image resolution. Higher values improve performance but reduce detection accuracy.
        /// </summary>
        [SerializeField] private int _divideNumber = 2;
        
        /// <summary>
        /// Read-only access to the divide number value
        /// </summary>
        public int DivideNumber => _divideNumber;

        // OpenCV matrices for image processing
        /// <summary>
        /// RGB format mat for marker detection and result display.
        /// </summary>
        private Mat _processingRgbMat;

        /// <summary>
        /// Full-size RGBA mat from original webcam image.
        /// </summary>
        private Mat _originalWebcamMat;
        
        /// <summary>
        /// Resized mat for intermediate processing.
        /// </summary>
        private Mat _halfSizeMat;

        /// <summary>
        /// The camera intrinsic parameters matrix.
        /// </summary>
        private Mat _cameraIntrinsicMatrix;

        /// <summary>
        /// The camera distortion coefficients.
        /// </summary>
        private MatOfDouble _cameraDistortionCoeffs;

        // ArUco detection related mats and variables
        private Mat _detectedMarkerIds;
        private List<Mat> _detectedMarkerCorners;
        private List<Mat> _rejectedMarkerCandidates;
        private Dictionary _markerDictionary;
        private ArucoDetector _arucoDetector;

        // ChArUco specific variables
        private Mat _charucoCorners;
        private Mat _charucoIds;
        private CharucoBoard _charucoBoard;
        private CharucoDetector _charucoDetector;

        private bool _isReady = false;
        
        /// <summary>
        /// Read-only access to determine if tracking is ready
        /// </summary>
        public bool IsReady => _isReady;

        /// <summary>
        /// Dictionary storing previous pose data for each marker ID for smoothing
        /// </summary>
        private Dictionary<int, PoseData> _prevPoseDataDictionary = new Dictionary<int, PoseData>();

        /// <summary>
        /// Previous pose data for the board for smoothing
        /// </summary>
        private PoseData _prevPoseData = new PoseData();

        /// <summary>
        /// Initialize the marker tracking system with camera parameters
        /// </summary>
        /// <param name="imageWidth">Camera image width in pixels</param>
        /// <param name="imageHeight">Camera image height in pixels</param>
        /// <param name="cx">Principal point X coordinate</param>
        /// <param name="cy">Principal point Y coordinate</param>
        /// <param name="fx">Focal length X</param>
        /// <param name="fy">Focal length Y</param>
        public void Initialize(int imageWidth, int imageHeight, float cx, float cy, float fx, float fy)
        {
            InitializeMatrices(imageWidth, imageHeight, cx, cy, fx, fy);        
        }

        /// <summary>
        /// Initialize all OpenCV matrices and detector parameters
        /// </summary>
        private void InitializeMatrices(int originalWidth, int originalHeight, float cX, float cY, float fX, float fY)
        {            
            // Processing dimensions (scaled by divide number)
            int processingWidth = originalWidth / _divideNumber;
            int processingHeight = originalHeight / _divideNumber;
            fX = fX / _divideNumber;
            fY = fY / _divideNumber;
            cX = cX / _divideNumber;
            cY = cY / _divideNumber;

            // Create camera intrinsic matrix
            _cameraIntrinsicMatrix = new Mat(3, 3, CvType.CV_64FC1);
            _cameraIntrinsicMatrix.put(0, 0, fX);
            _cameraIntrinsicMatrix.put(0, 1, 0);
            _cameraIntrinsicMatrix.put(0, 2, cX);
            _cameraIntrinsicMatrix.put(1, 0, 0);
            _cameraIntrinsicMatrix.put(1, 1, fY);
            _cameraIntrinsicMatrix.put(1, 2, cY);
            _cameraIntrinsicMatrix.put(2, 0, 0);
            _cameraIntrinsicMatrix.put(2, 1, 0);
            _cameraIntrinsicMatrix.put(2, 2, 1.0f);

            // No distortion coefficients for Quest cameras
            _cameraDistortionCoeffs = new MatOfDouble(0, 0, 0, 0);

            // Initialize all processing mats
            _originalWebcamMat = new Mat(originalHeight, originalWidth, CvType.CV_8UC4);
            _halfSizeMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC4);
            _processingRgbMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC3);

            // Create ArUco detection mats
            _detectedMarkerIds = new Mat();
            _detectedMarkerCorners = new List<Mat>();
            _rejectedMarkerCandidates = new List<Mat>();
            _markerDictionary = Objdetect.getPredefinedDictionary((int)_dictionaryId);
            
            // Configure detector parameters for optimal performance
            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_minDistanceToBorder(3);
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            detectorParams.set_minSideLengthCanonicalImg(16);
            detectorParams.set_errorCorrectionRate(0.8);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);

            // Create the ArUco detector
            _arucoDetector = new ArucoDetector(_markerDictionary, detectorParams, refineParameters);

            // Initialize ChArUco specific objects
            _charucoCorners = new Mat();
            _charucoIds = new Mat();
            _charucoBoard = new CharucoBoard(new Size(_squaresX, _squaresY), _squareLength, _markerLength, _markerDictionary);
            
            // Configure ChArUco detector parameters
            CharucoParameters charucoParameters = new CharucoParameters();
            charucoParameters.set_cameraMatrix(_cameraIntrinsicMatrix);
            charucoParameters.set_distCoeffs(_cameraDistortionCoeffs);
            charucoParameters.set_minMarkers(_charucoMinMarkers);
            
            // Create the ChArUco detector
            _charucoDetector = new CharucoDetector(_charucoBoard, charucoParameters, detectorParams, refineParameters);

            _isReady = true;
        }

        /// <summary>
        /// Release all OpenCV resources
        /// </summary>
        private void ReleaseResources()
        {
            Debug.Log("Releasing ChArUco tracking resources");

            if (_processingRgbMat != null)
                _processingRgbMat.Dispose();

            if (_originalWebcamMat != null)
                _originalWebcamMat.Dispose();
                
            if (_halfSizeMat != null)
                _halfSizeMat.Dispose();
  
            if (_arucoDetector != null)
                _arucoDetector.Dispose();

            if (_detectedMarkerIds != null)
                _detectedMarkerIds.Dispose();
            
            foreach (var corner in _detectedMarkerCorners)
            {
                corner.Dispose();
            }
            _detectedMarkerCorners.Clear();
            
            foreach (var rejectedCorner in _rejectedMarkerCandidates)
            {
                rejectedCorner.Dispose();
            }
            _rejectedMarkerCandidates.Clear();
                
            // Release ChArUco resources
            if (_charucoCorners != null)
                _charucoCorners.Dispose();
                
            if (_charucoIds != null)
                _charucoIds.Dispose();
                
            if (_charucoBoard != null)
                _charucoBoard.Dispose();
                
            if (_charucoDetector != null)
                _charucoDetector.Dispose();
        }

        /// <summary>
        /// Handle errors that occur during tracking operations
        /// </summary>
        /// <param name="errorCode">Error code</param>
        /// <param name="message">Error message</param>
        public void HandleError(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("ChArUco tracking error: " + errorCode + ":" + message);
        }

        /// <summary>
        /// Detect ChArUco markers in the provided webcam texture
        /// </summary>
        /// <param name="webCamTexture">Input webcam texture</param>
        /// <param name="resultTexture">Optional output texture for visualization</param>
        public void DetectMarker(WebCamTexture webCamTexture, Texture2D resultTexture = null)
        {
            if (_isReady)
            {
                if (webCamTexture == null)
                {
                    return;
                }
                
                // Get image from webcam at full size
                Utils.webCamTextureToMat(webCamTexture, _originalWebcamMat);
                
                // Resize for processing
                Imgproc.resize(_originalWebcamMat, _halfSizeMat, _halfSizeMat.size());
                
                // Convert to RGB for ArUco processing
                Imgproc.cvtColor(_halfSizeMat, _processingRgbMat, Imgproc.COLOR_RGBA2RGB);

                // Reset detection containers
                _detectedMarkerIds.create(0, 1, CvType.CV_32S);
                _detectedMarkerCorners.Clear();
                _rejectedMarkerCandidates.Clear();
                
                // First detect ArUco markers
                _arucoDetector.detectMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds, _rejectedMarkerCandidates);
                
                // Refine marker detection for better accuracy with ChArUco
                _arucoDetector.refineDetectedMarkers(_processingRgbMat, _charucoBoard, _detectedMarkerCorners, _detectedMarkerIds, _rejectedMarkerCandidates);
                
                // Draw detected markers for visualization
                if (_detectedMarkerCorners.Count == _detectedMarkerIds.total() || _detectedMarkerIds.total() == 0){
                    Objdetect.drawDetectedMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds, new Scalar(0, 255, 0));
                }
                
                // If at least one marker detected, process ChArUco board
                if (_detectedMarkerIds.total() > 0)
                {
                    // Detect ChArUco board corners
                    _charucoDetector.detectBoard(_processingRgbMat, _charucoCorners, _charucoIds, _detectedMarkerCorners, _detectedMarkerIds);
                    
                    // Draw ChArUco corners
                    if (_charucoCorners.total() == _charucoIds.total() || _charucoIds.total() == 0)
                        Objdetect.drawDetectedCornersCharuco(_processingRgbMat, _charucoCorners, _charucoIds, new Scalar(0, 0, 255));
                }

                // Update result texture for visualization
                if (resultTexture != null)
                {
                    Utils.matToTexture2D(_processingRgbMat, resultTexture);
                }
            }
        }

        /// <summary>
        /// Estimate pose for ChArUco board and update corresponding game object
        /// </summary>
        /// <param name="targetObject">GameObject to apply the pose to</param>
        /// <param name="camTransform">Camera transform for world-space positioning</param>
        public void EstimatePose(GameObject targetObject, Transform camTransform)
        {
            // Skip if not ready
            if (!_isReady || targetObject == null)
            {
                return;
            }
            
            // Skip if no ChArUco corners detected
            if (_charucoCorners == null || _charucoIds == null || 
                _charucoCorners.total() == 0 || _charucoIds.total() == 0 ||
                _charucoCorners.total() != _charucoIds.total() || _charucoIds.total() < 4)
                return;
                
            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat objectPoints = new Mat())
            using (Mat imagePoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                List<Mat> charucoCorners_list = new List<Mat>();
                for (int i = 0; i < _charucoCorners.rows(); i++)
                {
                    charucoCorners_list.Add(_charucoCorners.row(i));
                }
                _charucoBoard.matchImagePoints(charucoCorners_list, _charucoIds, objectPoints, imagePoints);
                
                // Find pose
                MatOfPoint3f objectPoints_p3f = new MatOfPoint3f(objectPoints);
                MatOfPoint2f imagePoints_p3f = new MatOfPoint2f(imagePoints);
                
                try
                {
                    Calib3d.solvePnP(objectPoints_p3f, imagePoints_p3f, _cameraIntrinsicMatrix, 
                        _cameraDistortionCoeffs, rvec, tvec);
                        
                    // Convert to Unity coordinate system
                    double[] rvecArr = new double[3];
                    rvec.get(0, 0, rvecArr);
                    double[] tvecArr = new double[3];
                    tvec.get(0, 0, tvecArr);
                    PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);
                    
                    // z軸の向きをArUcoと一致させるためにx軸周りに180度回転
                    poseData.rot = poseData.rot * Quaternion.Euler(180, 0, 0);
                    
                    // Apply low-pass filter if we have previous pose data
                    if (_prevPoseData.pos != Vector3.zero)
                    {
                        float t = _poseFilterCoefficient;
                        
                        // Filter position with linear interpolation
                        poseData.pos = Vector3.Lerp(poseData.pos, _prevPoseData.pos, t);
                        
                        // Filter rotation with spherical interpolation
                        poseData.rot = Quaternion.Slerp(poseData.rot, _prevPoseData.rot, t);
                    }
                    
                    // Store current pose for next frame
                    _prevPoseData = poseData;
                    
                    // Convert pose to matrix and apply to game object
                    var arMatrix = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
                    arMatrix = camTransform.localToWorldMatrix * arMatrix;
                    ARUtils.SetTransformFromMatrix(targetObject.transform, ref arMatrix);
                }
                catch (CvException e)
                {
                    Debug.LogWarning("EstimatePose error: " + e);
                }
            }
        }

        /// <summary>
        /// Explicitly release resources when the object is disposed
        /// </summary>
        public void Dispose()
        {
            ReleaseResources();
        }

        /// <summary>
        /// Clean up when object is destroyed
        /// </summary>
        void OnDestroy()
        {
            ReleaseResources();
        }

        /// <summary>
        /// Available ArUco dictionaries for marker detection
        /// </summary>
        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Objdetect.DICT_4X4_50,
            DICT_4X4_100 = Objdetect.DICT_4X4_100,
            DICT_4X4_250 = Objdetect.DICT_4X4_250,
            DICT_4X4_1000 = Objdetect.DICT_4X4_1000,
            DICT_5X5_50 = Objdetect.DICT_5X5_50,
            DICT_5X5_100 = Objdetect.DICT_5X5_100,
            DICT_5X5_250 = Objdetect.DICT_5X5_250,
            DICT_5X5_1000 = Objdetect.DICT_5X5_1000,
            DICT_6X6_50 = Objdetect.DICT_6X6_50,
            DICT_6X6_100 = Objdetect.DICT_6X6_100,
            DICT_6X6_250 = Objdetect.DICT_6X6_250,
            DICT_6X6_1000 = Objdetect.DICT_6X6_1000,
            DICT_7X7_50 = Objdetect.DICT_7X7_50,
            DICT_7X7_100 = Objdetect.DICT_7X7_100,
            DICT_7X7_250 = Objdetect.DICT_7X7_250,
            DICT_7X7_1000 = Objdetect.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Objdetect.DICT_ARUCO_ORIGINAL,
        }
    }
}