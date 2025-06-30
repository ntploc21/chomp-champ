// -----------------------------------------------------------------------------------------
// using classes
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// -----------------------------------------------------------------------------------------
// player movement class
public class PlayerMovement : MonoBehaviour
{
    // static public members
    public static PlayerMovement instance;

    public float collisionOffset = 0.00f;
    public ContactFilter2D movementFilter;

    private List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();

    // -----------------------------------------------------------------------------------------
    // public members
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

    // -----------------------------------------------------------------------------------------
    // private members
    private Vector2 movement;

    // -----------------------------------------------------------------------------------------
    // awake method to initialisation
    void Awake()
    {
        instance = this;
    }

    // -----------------------------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // update members
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement.Normalize();
    }
    // -----------------------------------------------------------------------------------------
    // fixed update methode
    // void FixedUpdate()
    // {
    //     rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    // }

    public void FixedUpdate()
    {

        // rb.MovePosition(rb.position + (moveInput * moveSpeed * Time.fixedDeltaTime));


        // Try to move player in input direction, followed by left right and up down input if failed
        bool success = MovePlayer(movement);

        if (!success)
        {
            // Try Left / Right
            success = MovePlayer(new Vector2(movement.x, 0));

            if (!success)
            {
                success = MovePlayer(new Vector2(0, movement.y));
            }
        }

    }

    // Tries to move the player in a direction by casting in that direction by the amount
    // moved plus an offset. If no collisions are found, it moves the players
    // Returns true or false depending on if a move was executed
    public bool MovePlayer(Vector2 direction)
    {
        int numRetry = 5;
        float curMoveSpeed = moveSpeed;
        for (int i = 0; i < numRetry; i++)
        {
            // Check for potential collisions
            int count = rb.Cast(
                direction, // X and Y values between -1 and 1 that represent the direction from the body to look for collisions
                movementFilter, // The settings that determine where a collision can occur on such as layers to collide with
                castCollisions, // List of collisions to store the found collisions into after the Cast is finished
                curMoveSpeed * Time.fixedDeltaTime +
                collisionOffset); // The amount to cast equal to the movement plus an offset

            if (count == 0)
            {
                Vector2 moveVector = direction * curMoveSpeed * Time.fixedDeltaTime;

                // No collisions
                rb.MovePosition(rb.position + moveVector);
                return true;
            }

            curMoveSpeed /= 2;
        }
        
        // Print collisions
        foreach (RaycastHit2D hit in castCollisions)
        {
            print(hit.ToString());
        }

        return false;
    }
}
