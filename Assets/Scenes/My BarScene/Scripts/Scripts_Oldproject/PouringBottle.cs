// using UnityEngine;

// public class PouringBottle : MonoBehaviour
// {
//     public string ingredientName; // E.g. "Vodka", "Juice"
//     public ParticleSystem bubblesEffect; // Drag your particle prefab here
//     public float pourAngleThreshold = 100f;

//   private void Update()
// {
//     // 1. Check if the bottle is upside down (pouring)
//     float angle = Vector3.Angle(transform.up, Vector3.down);
//     bool isPouring = angle < pourAngleThreshold;

//     // 2. Play/stop bubbles
//     if (isPouring)
//     {
//         if (!bubblesEffect.isPlaying)
//             bubblesEffect?.Play();

//         // 3. Cast a ray down to find the glass underneath
//         RaycastHit hit;
//         if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.5f))
//         {
//             Glass glass = hit.collider.GetComponent<Glass>();
//             if (glass != null)
//             {
//                 glass.AddIngredient(ingredientName); // Pour the ingredient in!
//             }
//         }
//     }
//     else
//     {
//         if (bubblesEffect.isPlaying)
//             bubblesEffect?.Stop();
//     }
// }

// }
