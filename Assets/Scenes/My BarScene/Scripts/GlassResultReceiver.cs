using UnityEngine;

public class GlassResultReceiver : MonoBehaviour
{
    [Header("Links")]
    public GlassFill glass;
    public Transform spawnPoint;
    public GameObject poofVfx;
    public AudioSource sfx;

    [Header("Book & Tuning")]
    public CocktailBook cocktailBook;
    [Range(1f, 6f)] public float sharpness = 3f;

    [Header("Spawn Decorator")]
    public SpawnDecorator decorator; // assign in Inspector

    float mlOld, mlLife, mlImp;

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
        if (glass != null && glass.IsFull) Serve();
    }

    void Serve()
    {
        float total = Mathf.Max(0.0001f, mlOld + mlLife + mlImp);
        float co = mlOld / total, cl = mlLife / total, ci = mlImp / total;

        Serve(co, cl, ci);
    }

    void Serve(float co, float cl, float ci)
    {
        GameObject prefab = cocktailBook ? cocktailBook.GetWeighted(co, cl, ci, sharpness) : null;
        if (prefab)
        {
            Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

            GameObject drink = Instantiate(prefab, pos, rot);
            Debug.Log("[GlassResultReceiver] Spawned drink: " + drink.name);

            // Tag as "Cocktail" so TriggerZone can detect it
            drink.tag = "Cocktail";

            // Find XRGrabInteractable anywhere in the spawned prefab
            var grab = drink.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null)
            {
                if (grab.gameObject.GetComponent<GlassPickup>() == null)
                {
                    grab.gameObject.AddComponent<GlassPickup>();
                    Debug.Log("[GlassResultReceiver] Added GlassPickup to " + grab.gameObject.name);
                }
            }
            else
            {
                Debug.LogWarning("[GlassResultReceiver] Spawned drink has no XRGrabInteractable: " + drink.name);
            }

            if (decorator) decorator.Decorate(drink); // <- single source of truth
        }

        if (poofVfx)
        {
            Vector3 vfxPos = spawnPoint ? spawnPoint.position : transform.position;
            Instantiate(poofVfx, vfxPos, Quaternion.identity);
        }
        if (sfx) sfx.Play();

        if (glass)
        {
            glass.currentMl = 0f;
            glass.RemoveLiquid(9999f);
        }

        mlOld = mlLife = mlImp = 0f;
    }
}
