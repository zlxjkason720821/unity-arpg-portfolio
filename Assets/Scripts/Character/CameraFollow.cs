using UnityEngine;

namespace ARPG.Character
{
    /// <summary>
    /// 第三人称俯视角跟随：平滑追玩家，演示录屏更稳。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 11f, -9f);
        [SerializeField] float followSpeed = 8f;
        [SerializeField] bool lookAtTarget = true;

        public void SetTarget(Transform t) => target = t;

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            if (lookAtTarget)
            {
                Vector3 look = target.position + Vector3.up * 1.2f;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(look - transform.position),
                    1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            }
        }
    }
}
