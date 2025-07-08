using UnityEngine;

namespace Other
{
    public class Key : MonoBehaviour
    {
        public Vector3 rotationSpeed = new Vector3(0, 50, 0);

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}