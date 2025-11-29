using UnityEngine;

public class SpiderLegGroup : MonoBehaviour
{
    public SpiderLeg[] legs;

    public bool IsAnyStepping
    {
        get
        {
            foreach (var leg in legs)
            {
                if (leg != null && leg.isStepping)
                    return true;
            }
            return false;
        }
    }
}
