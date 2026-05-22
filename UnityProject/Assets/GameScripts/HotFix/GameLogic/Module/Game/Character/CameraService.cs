using UnityEngine;

namespace GameLogic.Game
{
    public class CameraService
    {
        private Camera m_camera;
        private Transform m_target;
        private Vector3 m_offset = new Vector3(0, 10, -10);
        private float m_smoothSpeed = 5f;

        public Camera Camera
        {
            get { return m_camera; }
            set { m_camera = value; }
        }

        public Transform Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        public void Init(Camera camera)
        {
            m_camera = camera;
        }

        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        public void Update()
        {
            if (m_target != null && m_camera != null)
            {
                Vector3 desiredPosition = m_target.position + m_offset;
                Vector3 smoothedPosition = Vector3.Lerp(m_camera.transform.position, desiredPosition, m_smoothSpeed * Time.deltaTime);
                m_camera.transform.position = smoothedPosition;
                m_camera.transform.LookAt(m_target);
            }
        }

        public void Dispose()
        {
            m_camera = null;
            m_target = null;
        }
    }
}
