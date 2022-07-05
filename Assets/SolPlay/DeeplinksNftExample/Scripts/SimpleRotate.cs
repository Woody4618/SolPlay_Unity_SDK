using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Solplay.Deeplinks
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