using UnityEngine;

public class MainCamera : MonoBehaviour
{
    private float rotateSpeed = 0f;
    
    void Start()
    {
        if (transform.parent != null)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        Turn(rotateSpeed);
    }

    private void Turn(float rotateSpeed)
    {
        if (rotateSpeed != 0f)
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }
    }

    public bool SetRotateSpeed(float rotateSpeed)
    {
        this.rotateSpeed = rotateSpeed;
        return true;
    }
}