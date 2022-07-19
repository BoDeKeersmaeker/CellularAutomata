using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float Speed = 10f;

    private Rigidbody Rigidbody;
    Vector3 velocity;

    // Start is called before the first frame update
    private void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update()
    {
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * Speed;
    }

    private void FixedUpdate()
    {
        Rigidbody.MovePosition(Rigidbody.position + velocity * Time.fixedDeltaTime);
    }
}
