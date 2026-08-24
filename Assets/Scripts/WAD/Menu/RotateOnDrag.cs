using UnityEngine;

namespace WAD.Menu
{
    /// <summary> Liegt auf dem Pedestal/Root-Objekt der Waffen-Vorschau im Loadout-Menue. </summary>
    public class RotateOnDrag : MonoBehaviour
    {
        public float rotationSpeed = 5f;
        public bool autoRotateWhenIdle = true;
        public float autoRotateSpeed = 15f;

        private bool isDragging;
        private float lastMouseX;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { isDragging = true; lastMouseX = Input.mousePosition.x; }
            if (Input.GetMouseButtonUp(0)) isDragging = false;

            if (isDragging)
            {
                float deltaX = Input.mousePosition.x - lastMouseX;
                transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime, Space.World);
                lastMouseX = Input.mousePosition.x;
            }
            else if (autoRotateWhenIdle)
            {
                transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
