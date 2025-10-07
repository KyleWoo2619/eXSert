using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MirzaBeig.DistortionShockwavesVFX
{
    public class MouseFollow : MonoBehaviour
    {
        public float distance = 2.0f;

        void Start()
        {

        }

        void Update()
        {
            Camera camera = Camera.main;

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = distance;

            Vector3 mouseWorldPosition = camera.ScreenToWorldPoint(mousePosition);

            transform.position = mouseWorldPosition;
        }
    }
}
