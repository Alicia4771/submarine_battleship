using UnityEngine;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    [SerializeField, Tooltip("スコアを表示するTMPのテキスト")]
    private TMP_Text scoreText;
    
    private float time_count = 0;
    
    void Start()
    {
        time_count = 0;

        if (scoreText != null)
        {
            int score = DataManager.GetScore();
            scoreText.text = score.ToString();
        }
    }

    void Update()
    {
        time_count += Time.deltaTime;
    }
}
