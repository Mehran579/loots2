using Unity.VisualScripting;
using UnityEngine;


public class HitEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private float flashesPerSecond = 12f;

    [Header("Glow Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [Tooltip("Must be above your Bloom threshold to glow. Keep it as low as looks good.")]
    [SerializeField] private float flashIntensity = 2.5f;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private SpriteRenderer[] renderers;
    private MaterialPropertyBlock block;
    private float flashTimeLeft;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        block = new MaterialPropertyBlock();
        Apply(0f);
    }

    public void TakeHit()
    {
        flashTimeLeft = flashDuration;
    }

    private void Update()
    {
        if (flashTimeLeft <= 0f) return;

        flashTimeLeft -= Time.deltaTime;

        if (flashTimeLeft <= 0f)
        {
            Apply(0f);
            return;
        }

        bool on = Mathf.Repeat(Time.time * flashesPerSecond, 1f) < 0.5f;
        Apply(on ? 1f : 0f);
    }

    private void Apply(float amount)
    {
        Color hdr = flashColor * flashIntensity; // values above 1 = glows with bloom
        hdr.a = 1f;

        foreach (var r in renderers)
        {
            if (r == null) return;
            r.GetPropertyBlock(block);
            block.SetColor(FlashColorId, hdr);
            block.SetFloat(FlashAmountId, amount);
            r.SetPropertyBlock(block);
        }
    }
}