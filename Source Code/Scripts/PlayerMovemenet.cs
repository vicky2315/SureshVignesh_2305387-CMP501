using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.position = new Vector2 (0, 0);
    }

    void Update()
    {
        if (rb.CompareTag("LocalPlayer"))
        {
            HandleLocalPlayerInput();
        }
    }

    void HandleLocalPlayerInput()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(dirX * 7f, rb.velocity.y);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(rb.velocity.x, 7);
        }
    }
}
