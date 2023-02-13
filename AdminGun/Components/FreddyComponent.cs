namespace AdminGun.Components
{
    using UnityEngine;

    public class FreddyComponent : MonoBehaviour
    {
        private int count = 250;
        private Rigidbody rb;
        private void FixedUpdate()
        {
            count--;
            if (count <= 0)
            {
                Destroy(this);
            }
        }

        private void Start()
        {
        }

        public void AddForce(Vector3 force)
        {
            rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
            }
            rb.AddForce(force * (Plugin.Instance == null ? 1000 : Plugin.Instance.Config.Force));
        }
    }
}