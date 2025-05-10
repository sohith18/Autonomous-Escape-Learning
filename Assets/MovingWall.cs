using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingWall : MonoBehaviour
{
    public Vector3 moveDirection = Vector3.left; // direction to move
    public float moveDistance = 3f;              // how far it moves
    public float moveSpeed = 2f;                 // how fast it moves

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.PingPong(Time.time * moveSpeed, moveDistance);
        transform.position = startPos + moveDirection.normalized * offset;
    }
}
