using UnityEngine;

public class GlassResultReceiver : MonoBehaviour
{
    [Header("Links")]
    public GlassFill glass;
    public Transform spawnPoint;
    public GameObject poofVfx;
    public AudioSource sfx;

    [Header("Book & Tuning")]
    public CocktailBook cocktailBook;   // assign your asset here
    [Range(1f, 6f)] public float sharpness = 3f;

    // mix that entered THIS glass
    float mlOld, mlLife, mlImp;

    // Called by PourStream when pouring from the shaker
    public void Receive(ShakerContainer shaker, float ml)
    {
        if (!shaker || ml <= 0f) return;
        var p = shaker.Percentages();
        mlOld  += p.o * ml;
        mlLife += p.l * ml;
        mlImp  += p.i * ml;
    }

    void Update()
    {
        if (glass != null && glass.IsFull)
        {
            Serve();
            // keep enabled for next rounds
        }
    }

    void Serve()
    {
        float total = Mathf.Max(0.0001f, mlOld + mlLife + mlImp);
        float co = mlOld  / total;
        float cl = mlLife / total;
        float ci = mlImp  / total;

        GameObject prefab = (cocktailBook != null)
            ? cocktailBook.GetWeighted(co, cl, ci, sharpness)
            : null;

        if (prefab != null)
        {
            Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
            Quaternion rot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;
            Instantiate(prefab, pos, rot);
        }

        if (poofVfx != null)
        {
            Vector3 vfxPos = (spawnPoint != null) ? spawnPoint.position : transform.position;
            Instantiate(poofVfx, vfxPos, Quaternion.identity);
        }

        if (sfx != null) sfx.Play();

        // reset glass for next order
        if (glass != null)
        {
            glass.currentMl = 0f;
            glass.RemoveLiquid(9999f);
        }
        mlOld = mlLife = mlImp = 0f;
    }
}
