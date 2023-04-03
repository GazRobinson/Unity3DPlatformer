using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GaRo.Debug
{
    public class VectorTester : MonoBehaviour
    {
        public Vector3 Direction = Vector3.down;
        public bool CustomNormal = false;
        public Vector3 SurfaceNormal = Vector3.up;
        private Vector3 OutDirection = Vector3.zero;

        private bool HasRayHit = false;
        private RaycastHit HitInfo;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            HasRayHit = false;
            if (!CustomNormal)
            {
                if (Physics.Raycast(transform.position, Direction, out HitInfo))
                {
                    HasRayHit = true;
                    OutDirection = Vector3.ProjectOnPlane(Direction, HitInfo.normal);
                }
			}
			else
            {
                HasRayHit = true;
                HitInfo.point = transform.position + Direction * 2.0f;
                HitInfo.normal = SurfaceNormal;
                OutDirection = Vector3.ProjectOnPlane(Direction, HitInfo.normal);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = HasRayHit ? Color.green : Color.red;

            Gizmos.DrawLine(transform.position, transform.position + (Direction * (HasRayHit ? HitInfo.distance : 10.0f)));

			if (HasRayHit)
            {
                Gizmos.color =  Color.red;
                Gizmos.DrawLine(HitInfo.point, HitInfo.point + (OutDirection * 10.0f));

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(HitInfo.point, HitInfo.point + (HitInfo.normal));
            }
        }
    }    
}
