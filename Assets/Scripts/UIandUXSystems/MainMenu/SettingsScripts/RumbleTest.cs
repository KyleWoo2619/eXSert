using UnityEngine;

public class RumbleTest : MonoBehaviour
{
    public void onClickRumble()
    {
        RumbleManager.Instance.RumblePulse(.25f, 1, .25f);
    }
}
