// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class EnvironmentRayCastSampleManager : MonoBehaviour
    {
        private const string SPATIALPERMISSION = "com.oculus.permission.USE_SCENE";

        public Transform Camera;
        public EnvironmentRaycastManager RaycastManager;

        public bool HasScenePermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(SPATIALPERMISSION);
#else
            return true;
#endif
        }

        public Transform PlaceGameObject(Vector3 cameraPosition)
        {
            transform.position = Camera.position;
            transform.LookAt(cameraPosition);

            var ray = new Ray(Camera.position, transform.forward);
            if (RaycastManager.Raycast(ray, out var hitInfo))
            {
                transform.SetPositionAndRotation(
                    hitInfo.point,
                    Quaternion.LookRotation(hitInfo.normal, Vector3.up));
            }

            return transform;
        }
    }
}
