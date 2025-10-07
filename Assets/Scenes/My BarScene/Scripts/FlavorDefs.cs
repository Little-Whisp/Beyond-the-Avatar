using UnityEngine;

public enum Flavor { OldTwitter, LifeJacket, Imposter }

[CreateAssetMenu(menuName="Bar/Flavor Palette")]
public class FlavorPalette : ScriptableObject
{
public Color oldTwitter = new Color(0.45f, 0.90f, 0.92f); // #73E5EB
public Color lifeJacket = new Color(1.00f, 0.55f, 0.28f); // #FF8C47
public Color imposter   = new Color(0.93f, 0.27f, 0.42f); // #ED456B

}
