using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player3D : MonoBehaviour
{
    [SerializeField]
    private Camera MainCamera = null;
    [SerializeField]
    private CharacterController Controller = null;
    [SerializeField]
    private Rigidbody RigidBody = null;

    private Vector3 PlayerVelocity;
    private bool IsPlayerGrounded;

    [SerializeField]
    private float MoveSpeed = 10f;
    [SerializeField]
    private float JumpPower = 1f;
    [SerializeField]
    private float Gravity = 0f;

    Vector2 rotation = Vector2.zero;
    [SerializeField]
    private float LookSpeed = 1f;
    [SerializeField]
    private float MaxCamaeraPitch = 80f;

    [SerializeField]
    private float MaxMiningRange = 10f;

    // Start is called before the first frame update
    void Start()
    {
        if(!Controller)
            Controller = gameObject.AddComponent<CharacterController>();

        if (!RigidBody)
            RigidBody = gameObject.AddComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        IsPlayerGrounded = Controller.isGrounded;
        if (IsPlayerGrounded && PlayerVelocity.y < 0)
        {
            PlayerVelocity.y = 0f;
        }

        Vector3 move = Vector3.zero;
        move += Input.GetAxis("Horizontal") * MainCamera.transform.right;
        move += Input.GetAxis("Vertical") * MainCamera.transform.forward;
        Controller.Move(move * Time.deltaTime * MoveSpeed);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }

        // Changes the height position of the player..
        if (Input.GetButtonDown("Jump") && IsPlayerGrounded)
        {
            PlayerVelocity.y += Mathf.Sqrt(JumpPower * -3f * Gravity);
        }

        PlayerVelocity.y += Gravity * Time.deltaTime;
        Controller.Move(PlayerVelocity * Time.deltaTime);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        MoveSpeed += scroll * 100f * Time.deltaTime;

        if(MoveSpeed < 0f)
            MoveSpeed = 0;

        rotation.y += Input.GetAxis("Mouse X");
        rotation.x += -Input.GetAxis("Mouse Y");
        rotation.x = Mathf.Clamp(rotation.x, -MaxCamaeraPitch, MaxCamaeraPitch);
        MainCamera.transform.eulerAngles = (Vector2)rotation * LookSpeed;

        float mouseClick = Input.GetAxis("Fire1");
        if (mouseClick > 0f)
        {
            Ray ray = new Ray(MainCamera.transform.position, MainCamera.transform.forward);
            RaycastHit hitData;
            Physics.Raycast(ray, out hitData);

            if (hitData.transform && hitData.distance < MaxMiningRange)
            {
                GameObject hitObject = hitData.transform.gameObject;

                ChunkScript script = hitObject.GetComponent<ChunkScript>();
                if (script)
                {
                    print("Worldpos: " + hitData.point);
                    script.MineNode(hitData.point);
                }
            }
            else
                print("Raycast failed");
        }
    }
}