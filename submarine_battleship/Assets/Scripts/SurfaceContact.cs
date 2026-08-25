using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SurfaceContact : MonoBehaviour
{
    // ============================================================
    // 登録中の全接触
    // ============================================================

    private static readonly List<SurfaceContact>
        registeredContacts =
            new List<SurfaceContact>();


    // ============================================================
    // 接触情報
    // ============================================================

    [Header("Contact")]

    [SerializeField, Tooltip(
        "この船の内部的な種類。" +
        "ソナー表示ではこの種類を区別しない")]
    private SurfaceContactType contactType =
        SurfaceContactType.Unknown;


    [SerializeField, Tooltip(
        "この船をソナーに表示するか")]
    private bool sonarDetectable =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "登録・解除をConsoleへ表示する")]
    private bool debugLog =
        false;


    // ============================================================
    // Play開始時のstatic初期化
    // ============================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetRegistry()
    {
        registeredContacts.Clear();
    }


    // ============================================================
    // Enable
    // ============================================================

    private void OnEnable()
    {
        Register();
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        Unregister();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        Unregister();
    }


    // ============================================================
    // 登録
    // ============================================================

    private void Register()
    {
        if (
            registeredContacts.Contains(
                this
            )
        )
        {
            return;
        }


        registeredContacts.Add(
            this
        );


        if (debugLog)
        {
            Debug.Log(
                "SurfaceContact登録: " +
                gameObject.name +
                " / " +
                contactType
            );
        }
    }


    // ============================================================
    // 登録解除
    // ============================================================

    private void Unregister()
    {
        bool removed =
            registeredContacts.Remove(
                this
            );


        if (
            removed &&
            debugLog
        )
        {
            Debug.Log(
                "SurfaceContact解除: " +
                gameObject.name
            );
        }
    }


    // ============================================================
    // 無効な登録を掃除
    // ============================================================

    private static void CleanupInvalidContacts()
    {
        for (
            int index =
                registeredContacts.Count - 1;

            index >= 0;

            index--
        )
        {
            SurfaceContact contact =
                registeredContacts[index];


            if (contact == null)
            {
                registeredContacts
                    .RemoveAt(
                        index
                    );
            }
        }
    }


    // ============================================================
    // 全接触取得
    // ============================================================

    public static IReadOnlyList<SurfaceContact>
        GetRegisteredContacts()
    {
        CleanupInvalidContacts();


        return
            registeredContacts;
    }


    // ============================================================
    // 種類
    // ============================================================

    public SurfaceContactType GetContactType()
    {
        return
            contactType;
    }


    public void SetContactType(
        SurfaceContactType newType
    )
    {
        contactType =
            newType;
    }


    // ============================================================
    // ソナー
    // ============================================================

    public bool GetIsSonarDetectable()
    {
        return
            sonarDetectable;
    }


    public void SetSonarDetectable(
        bool detectable
    )
    {
        sonarDetectable =
            detectable;
    }


    // ============================================================
    // 位置
    // ============================================================

    public Vector3 GetWorldPosition()
    {
        return
            transform.position;
    }
}