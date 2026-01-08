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
        mlOld += p.o * ml;
        mlLife += p.l * ml;
        mlImp += p.i * ml;
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

        // 1) Decorate FIRST
        if (decorator) decorator.Decorate(drink);

        // 2) Normalize collider & rigidbody for XR safety
        NormalizeGrabCollider(drink);

        // 3) Make grabbable ONCE
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

    // -------- XR SAFETY NORMALIZATION --------
    private void NormalizeGrabCollider(GameObject root)
    {
        var grab = root.GetComponentInChildren<XRGrabInteractable>(true);
        if (!grab) return;

        var grabRoot = grab.gameObject;

        // Ensure Rigidbody is on grab root
        var rb = grabRoot.GetComponent<Rigidbody>();
        if (!rb) rb = grabRoot.AddComponent<Rigidbody>();

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Remove solid colliders from children (visual-only hierarchy)
        foreach (var c in grabRoot.GetComponentsInChildren<Collider>(true))
        {
            if (c.gameObject != grabRoot)
                Destroy(c);
        }

        // Ensure exactly ONE solid collider
        if (!grabRoot.GetComponent<Collider>())
        {
            var box = grabRoot.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }
    }
}
