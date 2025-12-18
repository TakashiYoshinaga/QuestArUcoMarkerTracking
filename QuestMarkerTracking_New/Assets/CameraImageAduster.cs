using Meta.XR;
using UnityEngine;

public class CameraImageAduster : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess _passthroughCameraAccess;
    [SerializeField] private float _distanceFromCamera = 1.0f;
   
    void Awake()
    {
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var pose = _passthroughCameraAccess.GetCameraPose();
        var intrinsics = _passthroughCameraAccess.Intrinsics;
        var image = _passthroughCameraAccess.CurrentResolution;
        var principalPoint = intrinsics.PrincipalPoint;
        var focalLength = intrinsics.FocalLength;
        var resolution = intrinsics.SensorResolution;
        transform.SetPositionAndRotation(pose.position + pose.rotation * Vector3.forward * _distanceFromCamera, pose.rotation);

        var scaleX = 2.0f * _distanceFromCamera * focalLength.x / resolution.x;
        var scaleY = scaleX * (image.y / (float)image.x);

        transform.localScale = new Vector3(scaleX, scaleY, 1.0f);

    }
}
