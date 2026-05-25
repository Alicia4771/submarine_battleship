using UnityEngine;

public class MainCamera : MonoBehaviour
{
    void Start()
    {
        if (transform.parent != null)
        {
            this.transform.position = transform.parent.position;
            this.transform.rotation = transform.parent.rotation;
        }
    }
}
