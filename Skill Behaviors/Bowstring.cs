using UnityEngine;
using ThunderRoad;

namespace ShadowArmory
{
    /// <summary>
    /// Helper class to identify bow weapons
    /// </summary>
    public class BowString : MonoBehaviour
    {
        public Transform nockPoint;
        public float maxDrawDistance = 0.5f;
        public float drawForce = 50f;

        private Transform stringRestPoint;
        private bool isDrawn;
        private float currentDrawValue;

        private void Start()
        {
            stringRestPoint = new GameObject("StringRestPoint").transform;
            stringRestPoint.parent = transform;
            stringRestPoint.localPosition = Vector3.zero;

            if (nockPoint == null)
            {
                nockPoint = new GameObject("NockPoint").transform;
                nockPoint.parent = transform;
                nockPoint.localPosition = Vector3.forward * 0.1f;
            }
        }

        public float GetDrawPercentage()
        {
            return currentDrawValue / maxDrawDistance;
        }

        public bool IsFullyDrawn()
        {
            return currentDrawValue >= maxDrawDistance * 0.9f;
        }
    }
}