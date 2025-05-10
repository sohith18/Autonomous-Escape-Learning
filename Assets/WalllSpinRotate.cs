using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallSpinRotate : MonoBehaviour
{
    public float speed = 50f;

    void Update()
    {
        transform.Rotate(Vector3.right, speed * Time.deltaTime);
    }
}
