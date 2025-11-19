using UnityEngine;

public class CustomerDrinkReceiver : MonoBehaviour
{
    public string customerName;  // e.g. "P2 (Customer)"

    private void OnTriggerEnter(Collider other)
    {
        // Final spawned cocktail from GlassResultReceiver
        if (!other.CompareTag("Cocktail"))
            return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnCocktailServed(customerName, other.gameObject.name);
        }

        Destroy(other.gameObject);
    }
}
