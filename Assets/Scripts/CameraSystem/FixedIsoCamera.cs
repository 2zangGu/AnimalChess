using UnityEngine;
using UnityEngine.InputSystem;
using AnimalChess.Board;

namespace AnimalChess.CameraSystem
{
    /// <summary>
    /// 롤토체스 스타일의 고정 카메라.
    /// 보드를 항상 같은 각도에서 내려다보며 회전은 불가능하고, 스크롤 줌만 허용한다.
    ///
    /// 이 프로젝트는 Active Input Handling이 새 Input System으로 설정되어 있어
    /// UnityEngine.Input 대신 Mouse.current로 스크롤 값을 읽는다.
    ///
    /// 사용법: Main Camera에 이 컴포넌트를 붙인다. target을 비워두면
    /// BoardManager가 만든 BoardCenter를 자동으로 찾아서 사용한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FixedIsoCamera : MonoBehaviour
    {
        [Header("타겟")]
        [Tooltip("카메라가 바라볼 보드의 중심점. 비워두면 BoardManager.Instance.BoardCenter를 사용한다.")]
        public Transform target;

        [Header("각도 / 거리")]
        [Tooltip("보드를 내려다보는 각도 (0=수평, 90=완전 위에서 아래로)")]
        [Range(20f, 80f)] public float pitchAngle = 50f;
        [Tooltip("타겟으로부터의 기본 거리")]
        public float distance = 14f;
        public float minDistance = 8f;
        public float maxDistance = 20f;

        [Header("줌")]
        public bool allowZoom = true;
        [Tooltip("스크롤 감도. 값이 클수록 한 번에 더 많이 줌인/아웃된다.")]
        public float zoomSpeed = 0.01f;
        public float zoomSmoothing = 8f;

        private float _targetDistance;

        private void Start()
        {
            if (target == null && BoardManager.Instance != null)
            {
                target = BoardManager.Instance.BoardCenter;
            }

            _targetDistance = distance;
            UpdateCameraTransform(true);
        }

        private void LateUpdate()
        {
            if (allowZoom && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
                }
            }

            UpdateCameraTransform(false);
        }

        private void UpdateCameraTransform(bool instant)
        {
            if (target == null) return;

            distance = instant
                ? _targetDistance
                : Mathf.Lerp(distance, _targetDistance, Time.deltaTime * zoomSmoothing);

            // 고정된 피치 각도로, 타겟 뒤쪽(-Z 방향)에서 내려다보는 위치를 계산한다.
            // 요(yaw) 회전은 없으므로 사용자가 임의로 시점을 돌릴 수 없다.
            Quaternion rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            transform.position = target.position + offset;
            transform.rotation = rotation;
        }
    }
}
