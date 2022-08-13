using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class SimpleRotate : MonoBehaviour
    {
        public float speed = 5;

        void Update()
        {
            transform.Rotate(Vector3.forward, speed);
        }
    }
}