using UnityEngine;

namespace Other
{
    public class AmmoBox : MonoBehaviour
    {
        public Vector3 rotationSpeed = new Vector3(0, 50, 0);

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}