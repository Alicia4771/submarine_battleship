using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int time_limit;
    private int default_time_limit = 60;
    private int time_count = 0;
    
    void Awake()
    {
        if (time_limit <= 0) time_limit = default_time_limit;
    }
    
    void Start()
    {
        time_count = 0;
    }

    void Update()
    {
        
    }
}
