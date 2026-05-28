using UnityEngine;

public class Ship : MonoBehaviour
{
    protected Rigidbody rigidbody;

    protected float speed;        // 速度
    protected float max_speed;    // 最大速度

    protected float rotateSpeed = 0;



    protected virtual void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void Update()
    {
        
    }

    protected virtual void FixedUpdate()
    {
        Turn(rotateSpeed);
    }

    protected void Turn(float rotateSpeed)
    {
        if (rotateSpeed != 0)
        {
            Quaternion deltaRotation = Quaternion.Euler(0f, rotateSpeed * Time.fixedDeltaTime, 0f);
            rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
        }
        else
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public bool SetRotateSpeed(float rotateSpeed)
    {
        if (rotateSpeed == null) return false;

        this.rotateSpeed = rotateSpeed;
        return true;
    }
}
