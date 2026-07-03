using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyShip : Ship
{
    [Header("--- 円運動の設定 ---")]
    private Vector3 centerPoint;
    private float radius;
    private float radius_random_factor = 0.05f;  
    [SerializeField] private float movementSpeed = 0.2f;
    private float currentAngle = 0f;

    [SerializeField] private float modelRotationOffset = 90f; //船の向き修正

    [Header("--- 光の暗号設定 ---")]
    [SerializeField] private Light signalLight;

    protected override void Start()
    {
        base.Start();

        centerPoint = this.transform.position;
        
        radius = DataManager.GetEnemyShipRotateRadius() + Random.Range(radius * radius_random_factor * (-1), radius * radius_random_factor);

        radius = DataManager.GetEnemyShipRotateRadius();
        radius += Random.Range(radius * radius_random_factor * (-1), radius * radius_random_factor);
        
        this.transform.position = centerPoint + new Vector3(radius, 0f, 0f);

        if (signalLight == null) signalLight = GetComponentInChildren<Light>();

        if (signalLight != null) StartCoroutine(FlashSignalRoutine());

               GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
    }

    protected override void Update()
    {
        base.Update();
        
        System.Object centerPoint = this.centerPoint; 

        if (centerPoint == null) return;

        CircleMove(movementSpeed);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        GetComponent<Rigidbody>().linearVelocity = transform.forward * movementSpeed;
    }

    private void CircleMove(float speed)
    {
        currentAngle += speed * Time.deltaTime;

        float x = Mathf.Cos(currentAngle) * radius;
        float z = Mathf.Sin(currentAngle) * radius;
        Vector3 nextPosition = centerPoint + new Vector3(x, 0f, z);

        Vector3 moveDirection = nextPosition - transform.position;
        moveDirection.y = 0f; // 上下の傾き（お辞儀）をカットして水平にする

        if (moveDirection != Vector3.zero)
        {
            // 1. まずはプログラム上の「正しい進む方向」を計算します
            Quaternion correctRotation = Quaternion.LookRotation(moveDirection.normalized);
            
            // 2. 正しい向きに対して、アセットのズレ分だけ回転を加える！
            transform.rotation = correctRotation * Quaternion.Euler(0f, modelRotationOffset, 0f);
        }

        transform.position = nextPosition;
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