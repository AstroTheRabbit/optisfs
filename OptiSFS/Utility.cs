using System.Globalization;
using SFS.Cameras;
using UnityEngine;

namespace OptiSFS
{
    public static class Utility
    {
        public static int CompareToCultureInvariant(this string a, string b)
        {
            return b == null ? 1 : CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None);
        }

        public static Rect GetCameraBounds(float z = 0f)
        {
            Camera cam = ActiveCamera.Camera.camera;

            if (cam == null)
            {
                Debug.LogError("ActiveCamera.Camera.camera is null!");
                return Rect.zero;
            }

            float height = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * Mathf.Abs(z - cam.transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float width = height * cam.aspect;

            Vector3 center = new Vector3(cam.transform.position.x, cam.transform.position.y, z);
            
            Vector3 half = new Vector3(width / 2f, height / 2f, 0f);
            Vector3 min = center - half;
            Vector3 max = center + half;

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}