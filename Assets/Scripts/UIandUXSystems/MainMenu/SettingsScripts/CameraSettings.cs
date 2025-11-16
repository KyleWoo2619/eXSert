using Unity.Cinemachine;
using UnityEngine;

public class CameraSettings : MonoBehaviour
{
    private CinemachineInputAxisController axisController;
    void Start()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (SettingsManager.Instance.invertY)
        {
            foreach (var c in axisController.Controllers)
            {
                if (c.Name == "Look Orbit X")
                {
                    c.Input.Gain = SettingsManager.Instance.sensitivity;
                }

                if (c.Name == "Look Orbit Y" && SettingsManager.Instance.invertY)
                {
                    c.Input.Gain = 1;

                }
            }
        }
    }
}
