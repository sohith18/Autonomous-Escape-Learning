using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentMovement : Agent
{
    public float moveSpeed = 3f;
    public float rotationSpeed = 100f;
    public float jumpForce = 5f;
    public float avoidWallDistance = 2f; // Detect walls nearby
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent at the center of the room
        transform.position = new Vector3(0, 1, 0);
        rb.velocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the agent's position and velocity as observations
        sensor.AddObservation(transform.position);
        sensor.AddObservation(rb.velocity);

        // Add information about nearby walls (obstacle detection)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, avoidWallDistance))
        {
            sensor.AddObservation(1);  // Wall detected
        }
        else
        {
            sensor.AddObservation(0);  // No wall detected
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get actions from ActionBuffers
        float moveForward = actions.ContinuousActions[0];  // Forward/backward movement
        float rotate = actions.ContinuousActions[1];       // Rotation
        float jump = actions.ContinuousActions[2];         // Jump action

        // Move forward/backward
        transform.Translate(Vector3.forward * moveForward * moveSpeed * Time.deltaTime);

        // Rotate left/right
        transform.Rotate(Vector3.up * rotate * rotationSpeed * Time.deltaTime);

        // Jump action
        if (jump > 0.5f && Mathf.Abs(rb.velocity.y) < 0.1f)  // Check if agent is on the ground
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Add reward for moving forward (escaping the room)
        AddReward(0.01f);

        // Reward for avoiding walls (or moving away from walls)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, avoidWallDistance))
        {
            AddReward(-0.01f); // Penalty for moving toward a wall
        }

        // End the episode if agent escapes the room (based on position or other criteria)
        if (transform.position.y < -1f) // Example condition for escape
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");   // Move forward/backward
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // Rotate left/right
        continuousActionsOut[2] = Input.GetButton("Jump") ? 1f : 0f; // Jump action
    }
}
