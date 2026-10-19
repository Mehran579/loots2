using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float lifetime = 0.6f;
    public float riseSpeed = 2f;
    public float spread = 0.4f;          // random offset so quick hits don't stack
    public float startScale = 1.4f;      // pops big, settles to 1

    TextMeshPro tmp;
    Vector3 vel;
    float age;

    public static void Spawn(DamagePopup prefab, Vector3 pos, float amount)
    {
        DamagePopup p = Instantiate(prefab, pos, Quaternion.identity);
        p.Setup(amount);
    }

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        tmp.sortingOrder = 50;
    }

    void Setup(float amount)
    {
        tmp.text = "-" + amount.ToString("0.#");
        transform.position += new Vector3(Random.Range(-spread, spread), Random.Range(0f, spread), 0f);
        vel = new Vector3(Random.Range(-0.5f, 0.5f), riseSpeed, 0f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / lifetime;

        transform.position += vel * Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, Mathf.Clamp01(k * 4f));

        Color c = tmp.color;
        c.a = 1f - k * k;                // stays visible, then fades quickly at the end
        tmp.color = c;

        if (age >= lifetime) Destroy(gameObject);
    }
}