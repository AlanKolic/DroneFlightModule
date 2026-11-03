using UnityEngine;

namespace DroneFlightModule.Unity
{
    /// <summary>
    /// Keeps the camera behind and above its target. Runs in LateUpdate, after the drone view has
    /// moved for the frame, so the two never fight.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -6f);
        [SerializeField] private float lookHeight = 0.5f;
        [SerializeField] private float sharpness = 5f;

        private ChaseCameraRig rig;

        public void SetTarget(Transform target)
        {
            this.target = target;
            Snap();
        }

        private void Awake()
        {
            rig = new ChaseCameraRig(offset, lookHeight, sharpness);
        }

        private void Start()
        {
            Snap();
        }

        private void LateUpdate()
        {
            if (target == null || rig == null)
            {
                return;
            }
            var current = new Pose(transform.position, transform.rotation);
            var pose = rig.Follow(current, new Pose(target.position, target.rotation), Time.deltaTime);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void Snap()
        {
            if (target == null || rig == null)
            {
                return;
            }
            var pose = rig.Snap(new Pose(target.position, target.rotation));
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
