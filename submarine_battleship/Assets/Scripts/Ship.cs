using UnityEngine;

public class Ship : MonoBehaviour
{
    private Rigidbody rigidbody;

    private float speed;        // 速度
    private float max_speed;    // 最大速度

    protected virtual void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void Update()
    {
        
    }

    public void Turn(float rotation_x)
    {
        if (rotation_x != 0)
        {
            Quaternion delta = Quaternion.AngleAxis(rotation_x, Vector3.up);
            rigidbody.MoveRotation(rigidbody.rotation * delta);
        }
        else
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
