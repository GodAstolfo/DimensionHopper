using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    [Range(1, 10)]
    [SerializeField] private float speed = 1f;

    [Range(1, 10)]
    [SerializeField] private float lifeTime = 3f;
    
    private Rigidbody2D rb;

    private void Start () {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate() {
        rb.linearVelocity = transform.up * speed;
    }
}
