using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyShip : Ship
{
    [Header("--- 円運動の設定 ---")]
    private Vector3 centerPoint;
    private float radius;
    private float radius_random_factor = 0.05f;  // 半径のランダム変動の割合
    [SerializeField] private float movementSpeed = 0.5f;
    private float currentAngle = 0f;

    [Header("--- 光の暗号設定 ---")]
    [SerializeField] private Light signalLight;

    protected override void Start()
    {
        base.Start();

        centerPoint = this.transform.position;
        radius = DataManager.GetEnemyShipRotateRadius() + Random.Range(radius * radius_random_factor * (-1), radius * radius_random_factor);

        this.transform.position = centerPoint + new Vector3(radius, 0f, 0f);

        if (signalLight == null) signalLight = GetComponentInChildren<Light>();

        if (signalLight != null) StartCoroutine(FlashSignalRoutine());
    }

    protected override void Update()
    {
        base.Update();
        if (centerPoint == null) return;

        CircleMove(movementSpeed);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    private void CircleMove(float speed)
    {
        currentAngle += speed * Time.deltaTime;

        float x = Mathf.Cos(currentAngle) * radius;
        float z = Mathf.Sin(currentAngle) * radius;

        transform.position = centerPoint + new Vector3(x, 0f, z);
    }

    private IEnumerator FlashSignalRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(PlayFlash(0.2f)); // トン
            yield return StartCoroutine(PlayFlash(0.2f)); // トン
            yield return StartCoroutine(PlayFlash(0.8f)); // ツー
            yield return StartCoroutine(PlayFlash(0.2f)); // トン

            yield return new WaitForSeconds(3.0f);
        }
    }

    private IEnumerator PlayFlash(float duration)
    {
        signalLight.enabled = true;
        yield return new WaitForSeconds(duration);
        signalLight.enabled = false;
        yield return new WaitForSeconds(0.3f);
    }
}
