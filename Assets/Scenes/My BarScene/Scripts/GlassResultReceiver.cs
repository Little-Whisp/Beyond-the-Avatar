using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GlassResultReceiver : MonoBehaviour
{
    [Header("Links")]
    public GlassFill glass;
    public Transform spawnPoint;
    public GameObject poofVfx;
    public AudioSource sfx;

    [Header("Book")]
    public CocktailBook cocktailBook;

    [Header("Spawn Decorator")]
    public SpawnDecorator decorator;

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
        if (glass != null && glass.IsFull)
            Serve();
    }

    void Serve()
    {
        GameObject prefab = cocktailBook ? cocktailBook.GetRandom() : null;
        if (!prefab) return;

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        GameObject drink = Instantiate(prefab, pos, rot);
        drink.tag = "Cocktail";

        // ✅ 1) Decorate FIRST (it may add/replace colliders/grab/etc)
        if (decorator) decorator.Decorate(drink);

        // ✅ 2) Then ensure grabbable (on the actual grab object)
        // EnsureGrabbable(drink);
        FindObjectOfType<ShakerContainer>()?.MakeCocktailGrabbable(drink);

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

    private void EnsureGrabbable(GameObject root)
    {
        // Find (or create) the XRGrabInteractable that the hands will actually use
        var grab = root.GetComponentInChildren<XRGrabInteractable>(true);
        GameObject target = grab ? grab.gameObject : root;

        if (!grab)
            grab = target.AddComponent<XRGrabInteractable>();

        // Rigidbody MUST be on the same object as the grab collider setup
        var rb = target.GetComponent<Rigidbody>();
        if (!rb) rb = target.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // Ensure at least 1 solid collider exists on/under the grab object
        var solidCols = target.GetComponentsInChildren<Collider>(true);
        bool hasSolid = false;
        foreach (var c in solidCols)
        {
            if (c && !c.isTrigger) { hasSolid = true; break; }
        }

        if (!hasSolid)
        {
            var box = target.GetComponent<BoxCollider>();
            if (!box) box = target.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }

        // Rebuild grab.colliders to ONLY non-trigger colliders under target
        grab.colliders.Clear();
        foreach (var c in target.GetComponentsInChildren<Collider>(true))
        {
            if (c && !c.isTrigger)
                grab.colliders.Add(c);
        }

        // GlassPickup must be on the same object as XRGrabInteractable
        var pickup = target.GetComponent<GlassPickup>();
        if (!pickup) pickup = target.AddComponent<GlassPickup>();

        pickup.triggerZones = FindObjectsOfType<TriggerZone>();
        // Rebuild caches and listeners to ensure Awake-time caching doesn't leave
        // stale/null references after runtime decoration.
        pickup.Reinitialize();
    }
}
