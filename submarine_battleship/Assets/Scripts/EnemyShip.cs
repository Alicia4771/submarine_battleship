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

    [SerializeField] private float modelRotationOffset = 90f; 

    private Light signalLight; // 💡 Unity側での割り当てが不要になったため、SerializeFieldを削除
    
    private bool isDetected = false;
    private GameObject shipVisual;

    [Header("--- 潜望鏡の索敵設定 ---")]
    [SerializeField] private float periscopeFOV = 45.0f; 
    [SerializeField] private float maxDetectDistance = 50.0f; 

    protected override void Start()
    {
        base.Start();

        if (transform.childCount > 0)
        {
            shipVisual = transform.GetChild(0).gameObject;
        }

        if (shipVisual != null)
        {
            shipVisual.SetActive(false);
        }

        centerPoint = this.transform.position;
        
        radius = DataManager.GetEnemyShipRotateRadius();
        radius += Random.Range(radius * radius_random_factor * (-1), radius * radius_random_factor);
        
        this.transform.position = centerPoint + new Vector3(radius, 0f, 0f);

        // 💡 【自動化】プログラムが自分でライトのオブジェクトを作成し、設定まで完了させる
        CreateAutomaticLight();

        if (signalLight != null) StartCoroutine(FlashSignalRoutine());

        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
    }

    protected override void Update()
    {
        base.Update();
        
        if (centerPoint == null) return;

        if (!isDetected)
        {
            CheckSubmarineRadar();
            return;
        }

        CircleMove(movementSpeed);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (!isDetected)
        {
            GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            return;
        }
        
        GetComponent<Rigidbody>().linearVelocity = transform.forward * movementSpeed;
    }

    private void CreateAutomaticLight()
    {
        // 1. 新しいゲームオブジェクトをプログラム上で作成
        GameObject lightObj = new GameObject("AutoSignalLight");
        
        // 2. 敵船の子オブジェクトにして位置を固定する
        lightObj.transform.SetParent(this.transform);
        
        // 3. 船の少し上（海の上）に電球の座標を設定する（Position Y = 3）
        lightObj.transform.localPosition = new Vector3(0f, 3f, 0f);
        
        // 4. LightコンポーネントをくっつけてPoint Lightにする
        signalLight = lightObj.AddComponent<Light>();
        signalLight.type = LightType.Point;
        
        // 5. 明るさと範囲を自動で爆上げする
        signalLight.intensity = 500f;
        signalLight.range = 100f;
        
        // 6. 最初は消灯しておく
        signalLight.enabled = false;
    }

    private void CheckSubmarineRadar()
    {
        Vector3 subPos3D = DataManager.GetSubmarinePosition();
        Vector2 subPos = new Vector2(subPos3D.x, subPos3D.z);
        Vector2 enemyPos = new Vector2(transform.position.x, transform.position.z);

        Vector2 targetVector = enemyPos - subPos;
        
        if (targetVector.magnitude > maxDetectDistance) return;
        Vector2 Tn = targetVector.normalized;

        float thetaDeg = DataManager.GetSubmarineRotation();
        float correctedThetaDeg = 90f - thetaDeg;
        float thetaRad = correctedThetaDeg * Mathf.Deg2Rad;

        Vector2 Rn = new Vector2(Mathf.Cos(thetaRad), Mathf.Sin(thetaRad)).normalized;

        Vector2 D = Rn - Tn;
        float allowedThreshold = 2f * Mathf.Sin((periscopeFOV / 2f) * Mathf.Deg2Rad);

        if (D.magnitude <= allowedThreshold)
        {
            OnDetected();
        }
    }

    private void CircleMove(float speed)
    {
        currentAngle += speed * Time.deltaTime;

        float x = Mathf.Cos(currentAngle) * radius;
        float z = Mathf.Sin(currentAngle) * radius;
        Vector3 nextPosition = centerPoint + new Vector3(x, 0f, z);

        Vector3 moveDirection = nextPosition - transform.position;
        moveDirection.y = 0f; 

        if (moveDirection != Vector3.zero)
        {
            Quaternion correctRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = correctRotation * Quaternion.Euler(0f, modelRotationOffset, 0f);
        }

        transform.position = nextPosition;
    }

    public void OnDetected()
    {
        if (isDetected) return;

        isDetected = true;

        if (shipVisual != null)
        {
            shipVisual.SetActive(true);
        }
    }

    private IEnumerator FlashSignalRoutine()
    {
        while (!isDetected)
        {
            yield return null; 
        }

        while (true)
        {
            yield return StartCoroutine(PlayFlash(0.15f, 0.15f)); 
            yield return StartCoroutine(PlayFlash(0.15f, 0.15f)); 
            yield return StartCoroutine(PlayFlash(0.65f, 0.15f)); 
            yield return StartCoroutine(PlayFlash(0.15f, 1.5f)); 

            yield return new WaitForSeconds(1.5f);
        }
    }

    private IEnumerator PlayFlash(float duration, float blankTime)
    {
        if (signalLight == null) yield break;
        
        signalLight.enabled = true;
        yield return new WaitForSeconds(duration);
        signalLight.enabled = false;
        yield return new WaitForSeconds(blankTime);
    }
}