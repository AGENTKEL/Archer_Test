using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    private Rigidbody2D rb;
    private SkeletonAnimation skeleton;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        skeleton = GetComponentInChildren<SkeletonAnimation>();
    }

    void FixedUpdate()
    {
        if (rb.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
        {
            if (skeleton != null)
            {
                skeleton.AnimationState.SetAnimation(0, "attack", false);
            }


            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
                rb.simulated = false;
            }


            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;


            Destroy(gameObject, 2f);
        }
    }
    
}
