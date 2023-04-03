using UnityEngine;

namespace GaRo
{
    public class CameraController : MonoBehaviour
    {
        public Transform FollowTarget = null;
        public Transform LookAtTarget = null;
        public Vector3 Offset = Vector3.zero;
        public float Distance = 3.5f;
        public float FollowSpeed = 5.0f;
        public float RotationSpeed = 90.0f;

        public float Sensitivity = 5.0f;
        public bool InvertHorizontal = false;
        public bool InvertVertical = true;

        public bool UseLakituCam = true;
        public float CameraHeightFromGround = 2.0f;
        private float Yaw = 0.0f;
        private float Pitch = 0.0f;

        private float CameraDegrees = 180.0f;

        private Vector2 CameraInput = Vector2.zero;
        Vector3 lookAt = Vector3.zero;

        Vector3 tester;
        public void SetInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            CameraInput = context.ReadValue<Vector2>();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            
            if (UseLakituCam)
            {
                LakituCam();
			}
			else
			{
                HardFollow();
			}
        }

        void LakituCam()
		{
            Yaw = CameraInput.x * Time.deltaTime * Sensitivity * CameraDegrees * (InvertHorizontal ? -1.0f : 1.0f);

            Vector3 pos = transform.position - LookAtTarget.position;
            float d = pos.magnitude;
            Vector3 orig = pos;
            orig = Quaternion.AngleAxis(Yaw, Vector3.up) * pos;

            tester = LookAtTarget.position + orig;
            pos = orig.normalized * Distance;// Vector3.ClampMagnitude(pos, Distance);
            if(Mathf.Abs(Yaw) > 0)
			{
                transform.position = LookAtTarget.position + orig;
            }
            Vector3 targetPos = LookAtTarget.position + pos;
            PlayerState StateInfo = GameObject.FindGameObjectWithTag("Player").GetComponent<PlatformPlayerController>().GetStateInfo;

            targetPos.y = StateInfo.ShadowPosition.y + CameraHeightFromGround;
            //Instead if followspeed it should increase as the camera gets closer
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Mathf.Max(FollowSpeed, Mathf.Pow(Mathf.Abs(d-Distance),3)) * Time.deltaTime);

            Vector3 DistanceLine = LookAtTarget.position - transform.position;
            Vector3 DistanceLineNoY = LookAtTarget.position - transform.position;
            DistanceLineNoY.y = 0;
            Vector3 look = transform.forward;
            look.y = 0;
            look.Normalize();
            Quaternion TargetYRotation = Quaternion.FromToRotation(look, DistanceLineNoY.normalized);
            Quaternion TargetXRotation = Quaternion.FromToRotation(transform.forward, DistanceLineNoY.normalized);

            lookAt.x = LookAtTarget.position.x;
            lookAt.z = LookAtTarget.position.z;
            lookAt.y = Mathf.MoveTowards(lookAt.y, LookAtTarget.position.y, Mathf.Pow( Mathf.Abs(LookAtTarget.position.y - lookAt.y),2) * Time.deltaTime);
            transform.LookAt(lookAt);
        }

        void HardFollow()
		{
            //Reset
            transform.position = FollowTarget.position + Offset;
            transform.rotation = Quaternion.identity;
            Yaw = Mathf.Repeat(Yaw + CameraInput.x * Time.deltaTime * Sensitivity * CameraDegrees * (InvertHorizontal ? -1.0f : 1.0f), 360.0f);
            Pitch = Mathf.Clamp(Pitch + CameraInput.y * Time.deltaTime * Sensitivity * CameraDegrees * (InvertVertical ? -1.0f : 1.0f), -89.0f, 89.0f);

            transform.rotation = Quaternion.AngleAxis(Yaw, Vector3.up) * Quaternion.AngleAxis(Pitch, transform.right);
            transform.Translate(Vector3.back * Distance, Space.Self);
            transform.LookAt(LookAtTarget);
        }

		private void OnDrawGizmos()
		{
            Gizmos.DrawSphere(tester, 0.25f);
		}
	}
}
 