using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVisualHighlight : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float MinimumIntensity =
        0.0f;

    private const float DefaultEmissionIntensity =
        1.5f;

    private const string BaseColorProperty =
        "_BaseColor";

    private const string LegacyColorProperty =
        "_Color";

    private const string EmissionColorProperty =
        "_EmissionColor";

    private const string EmissionKeyword =
        "_EMISSION";


    // ============================================================
    // 対象
    // ============================================================

    [Header("Target")]

    [SerializeField, Tooltip(
        "緑色表示するモデルのRoot。" +
        "未設定の場合はこのGameObject以下のRendererをすべて対象にする")]
    private Transform visualRoot;


    // ============================================================
    // 色
    // ============================================================

    [Header("Highlight")]

    [SerializeField, Tooltip(
        "敵艦に適用する基本色")]
    private Color highlightColor =
        Color.green;


    [SerializeField, Tooltip(
        "Emissionを使用して暗い場所でも緑色を見やすくする")]
    private bool useEmission =
        true;


    [SerializeField, Tooltip(
        "Emissionの強さ")]
    [Min(MinimumIntensity)]
    private float emissionIntensity =
        DefaultEmissionIntensity;


    // ============================================================
    // 除外設定
    // ============================================================

    [Header("Options")]

    [SerializeField, Tooltip(
        "非Activeの子オブジェクトにあるRendererも対象にする")]
    private bool includeInactiveObjects =
        true;


    [SerializeField, Tooltip(
        "SkinnedMeshRendererを含め、取得できるすべてのRendererを対象にする")]
    private bool includeAllRendererTypes =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // 内部状態
    // ============================================================

    private readonly List<Material>
        runtimeMaterials =
            new List<Material>();


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ApplyHighlight();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        DestroyRuntimeMaterials();
    }


    // ============================================================
    // 緑色化
    // ============================================================

    public void ApplyHighlight()
    {
        DestroyRuntimeMaterials();


        Transform targetRoot =
            visualRoot != null
                ? visualRoot
                : transform;


        Renderer[] renderers =
            targetRoot.GetComponentsInChildren<Renderer>(
                includeInactiveObjects
            );


        int changedMaterialCount =
            0;


        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }


            if (
                !includeAllRendererTypes &&
                !(targetRenderer is MeshRenderer)
            )
            {
                continue;
            }


            Material[] sourceMaterials =
                targetRenderer.sharedMaterials;


            Material[] newMaterials =
                new Material[sourceMaterials.Length];


            for (
                int materialIndex = 0;
                materialIndex < sourceMaterials.Length;
                materialIndex++
            )
            {
                Material sourceMaterial =
                    sourceMaterials[materialIndex];


                if (sourceMaterial == null)
                {
                    newMaterials[materialIndex] =
                        null;

                    continue;
                }


                Material runtimeMaterial =
                    new Material(
                        sourceMaterial
                    );


                runtimeMaterial.name =
                    sourceMaterial.name +
                    "_EnemyHighlightRuntime";


                ApplyColorToMaterial(
                    runtimeMaterial
                );


                runtimeMaterials.Add(
                    runtimeMaterial
                );


                newMaterials[materialIndex] =
                    runtimeMaterial;


                changedMaterialCount++;
            }


            targetRenderer.sharedMaterials =
                newMaterials;
        }


        if (debugLog)
        {
            Debug.Log(
                gameObject.name +
                " を緑色表示に変更しました。" +
                " Materials = " +
                changedMaterialCount
            );
        }
    }


    // ============================================================
    // Materialへ色設定
    // ============================================================

    private void ApplyColorToMaterial(
        Material material
    )
    {
        if (material == null)
        {
            return;
        }


        // ========================================================
        // URP Lit等
        // ========================================================

        if (
            material.HasProperty(
                BaseColorProperty
            )
        )
        {
            material.SetColor(
                BaseColorProperty,
                highlightColor
            );
        }
        // ========================================================
        // Standard等
        // ========================================================
        else if (
            material.HasProperty(
                LegacyColorProperty
            )
        )
        {
            material.SetColor(
                LegacyColorProperty,
                highlightColor
            );
        }


        // ========================================================
        // Emission
        // ========================================================

        if (
            useEmission &&
            material.HasProperty(
                EmissionColorProperty
            )
        )
        {
            material.EnableKeyword(
                EmissionKeyword
            );


            Color emissionColor =
                highlightColor *
                emissionIntensity;


            material.SetColor(
                EmissionColorProperty,
                emissionColor
            );
        }
    }


    // ============================================================
    // Runtime Material削除
    // ============================================================

    private void DestroyRuntimeMaterials()
    {
        for (
            int materialIndex = runtimeMaterials.Count - 1;
            materialIndex >= 0;
            materialIndex--
        )
        {
            Material material =
                runtimeMaterials[
                    materialIndex
                ];


            if (material == null)
            {
                continue;
            }


            Destroy(
                material
            );
        }


        runtimeMaterials.Clear();
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        emissionIntensity =
            Mathf.Max(
                MinimumIntensity,
                emissionIntensity
            );
    }
}