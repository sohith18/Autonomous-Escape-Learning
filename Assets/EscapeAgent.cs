using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EscapeAgent : Agent
{
    public float moveSpeed = 3f;
    public float jumpForce = 5f;
    public Transform exitTarget;
    private Rigidbody rb;
    private bool isGrounded = false;

    // 👇 NEW: For tracking how close to walls
    // private float lastWallProximity = Mathf.Infinity;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Random X between -4 and 4
        float randomX = Random.Range(-4f, 4f);

        // Random Z between -4 and 4
        float randomZ = Random.Range(-4f, 4f);

        // Keep Y = 1
        transform.localPosition = new Vector3(randomX, 1f, randomZ);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // lastWallProximity = Mathf.Infinity; // 👈 reset
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        // sensor.AddObservation(exitTarget.localPosition);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(isGrounded ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var continuous = actions.ContinuousActions;

        // Movement input (X, Z plane)
        Vector3 moveDir = new Vector3(continuous[0], 0f, continuous[1]);
        rb.AddForce(moveDir * moveSpeed, ForceMode.Force);

        AddReward(-0.001f); // Time penalty

        // 👇 Check proximity to walls
        // CheckWallProximity();

        // Jump
        if (isGrounded && continuous[2] > 0.5f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            AddReward(-0.01f); // Small penalty for jumping
        }
    }

    // private void CheckWallProximity()
    // {
    //     float proximity = GetDistanceToClosestWall();

    //     if (proximity < 1.5f)
    //     {
    //         // Close to wall: small penalty
    //         AddReward(-0.001f);
    //     }
    //     else if (proximity > lastWallProximity)
    //     {
    //         // Moved away from wall: small reward
    //         AddReward(0.0015f);
    //     }

    //     lastWallProximity = proximity;
    // }

    // private float GetDistanceToClosestWall()
    // {
    //     float minDistance = Mathf.Infinity;
    //     RaycastHit hit;

    //     // Cast rays in multiple directions
    //     Vector3[] directions = {
    //         Vector3.forward, Vector3.back,
    //         Vector3.left, Vector3.right,
    //         (Vector3.forward + Vector3.left).normalized,
    //         (Vector3.forward + Vector3.right).normalized,
    //         (Vector3.back + Vector3.left).normalized,
    //         (Vector3.back + Vector3.right).normalized
    //     };

    //     foreach (var dir in directions)
    //     {
    //         if (Physics.Raycast(transform.position, dir, out hit, 5f))
    //         {
    //             if (hit.collider.CompareTag("Wall"))
    //             {
    //                 if (hit.distance < minDistance)
    //                 {
    //                     minDistance = hit.distance;
    //                 }
    //             }
    //         }
    //     }

    //     return minDistance;
    // }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        continuous[0] = Input.GetAxis("Horizontal");
        continuous[1] = Input.GetAxis("Vertical");
        continuous[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
        {
            Debug.Log("Hit an obstacle!");
            AddReward(-0.05f); // Small penalty for obstacle
        }

        if (collision.collider.CompareTag("Wall"))
        {
            Debug.Log("Hit a wall!");
            AddReward(-0.1f); // Bigger penalty for wall
        }

        if (collision.collider.CompareTag("Ground"))
        {
            Debug.Log("Touched the ground.");
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            Debug.Log("Left the ground.");
            isGrounded = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
        {
            Debug.Log("Reached the exit!");
            AddReward(1.0f);
            EndEpisode();
        }
    }
}

// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;
// using UnityEngine;

// public class EscapeAgent : Agent
// {
//     public float moveSpeed = 3f;
//     public float jumpForce = 5f;
//     public Transform exitTarget;
//     private Rigidbody rb;
//     private bool isGrounded = false;

//     // Initialize the agent
//     public override void Initialize()
//     {
//         rb = GetComponent<Rigidbody>();
//     }

//     // Called at the start of each episode
//     public override void OnEpisodeBegin()
//     {
//         // Random X between -4 and 4
//         float randomX = Random.Range(-4f, 4f);

//         // Random Z between -4 and 4
//         float randomZ = Random.Range(-4f, 4f);

//         // Keep Y = 1
//         transform.localPosition = new Vector3(randomX, 1f, randomZ);
//         rb.velocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;
//     }

//     // Collect observations from the environment
//     public override void CollectObservations(VectorSensor sensor)
//     {
//         sensor.AddObservation(transform.localPosition);  // Agent position
//         sensor.AddObservation(rb.velocity);  // Agent velocity
//         sensor.AddObservation(isGrounded ? 1f : 0f);  // Is grounded or not
//     }

//     // Handle actions received from the model
//     public override void OnActionReceived(ActionBuffers actions)
//     {
//         // Read actions for movement and jumping
//         int actionMove = actions.DiscreteActions[0];  // Movement (5 choices)
//         int actionJump = actions.DiscreteActions[1];  // Jump (2 choices)

//         // Action logic for movement (5 choices)
//         switch (actionMove)
//         {
//             case 0: // No move
//                 break;
//             case 1: // Move left
//                 rb.AddForce(Vector3.left * moveSpeed, ForceMode.Force);
//                 break;
//             case 2: // Move right
//                 rb.AddForce(Vector3.right * moveSpeed, ForceMode.Force);
//                 break;
//             case 3: // Move forward
//                 rb.AddForce(Vector3.forward * moveSpeed, ForceMode.Force);
//                 break;
//             case 4: // Move backward
//                 rb.AddForce(Vector3.back * moveSpeed, ForceMode.Force);
//                 break;
//             default:
//                 break;
//         }

//         // Action logic for jumping (2 choices)
//         if (actionJump == 1 && isGrounded)  // Jump if action is 1
//         {
//             rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
//             isGrounded = false;
//         }

//         // Apply time penalty to encourage faster completion
//         AddReward(-0.001f);  // Time penalty

//         // Reward for reaching the exit
//         if (Vector3.Distance(transform.position, exitTarget.position) < 1f)
//         {
//             AddReward(1.0f);  // Reward for reaching exit
//             EndEpisode();  // End the episode
//         }
//     }

//     // Heuristic method for manual control (testing)
//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var discrete = actionsOut.DiscreteActions;

//         // Movement control
//         if (Input.GetKey(KeyCode.A)) 
//             discrete[0] = 1; // Move left
//         else if (Input.GetKey(KeyCode.D)) 
//             discrete[0] = 2; // Move right
//         else if (Input.GetKey(KeyCode.W)) 
//             discrete[0] = 3; // Move forward
//         else if (Input.GetKey(KeyCode.S)) 
//             discrete[0] = 4; // Move backward
//         else 
//             discrete[0] = 0; // No move

//         // Jump control
//         discrete[1] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Jump if space is pressed
//     }

//     // Handle collisions
//     private void OnCollisionEnter(Collision collision)
//     {
//         if (collision.collider.CompareTag("Obstacle"))
//         {
//             AddReward(-0.05f);  // Penalty for hitting obstacles
//         }

//         if (collision.collider.CompareTag("Wall"))
//         {
//             AddReward(-0.1f);  // Bigger penalty for hitting walls
//         }

//         if (collision.collider.CompareTag("Ground"))
//         {
//             isGrounded = true;
//         }
//     }

//     // Handle collision exit
//     private void OnCollisionExit(Collision collision)
//     {
//         if (collision.collider.CompareTag("Ground"))
//         {
//             isGrounded = false;
//         }
//     }

//     // Handle trigger events (like reaching the exit)
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Exit"))
//         {
//             AddReward(1.0f);  // Reward for reaching the exit
//             EndEpisode();  // End the episode
//         }
//     }
// }

