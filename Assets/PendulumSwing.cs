using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PendulumSwing : MonoBehaviour
{
    public float swingSpeed = 50f;
    public float swingAngle = 60f;

    private float startTime;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        float angle = swingAngle * Mathf.Sin((Time.time - startTime) * swingSpeed * Mathf.Deg2Rad);
        transform.localRotation = Quaternion.Euler(angle,0 , 0); // For Z axis swing
    }
}

