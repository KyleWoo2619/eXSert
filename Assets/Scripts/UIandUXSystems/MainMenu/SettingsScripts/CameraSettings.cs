using Unity.Cinemachine;
using UnityEngine;

public class InvertY : MonoBehaviour
{
    private CinemachineInputAxisController axisController;
    void Start()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
    }

    // Update is called once per frame
    void Update()
    {
       foreach(var c in axisController.Controllers)
        {
            if (c.Name == "Look Orbit X")
            {
                c.Input.Gain = CameraSettingsManager.Instance.sensitivity;
            }
            
            if(c.Name == "Look Orbit Y" && CameraSettingsManager.Instance.invertY)
            {
                c.Input.Gain = 1;
                
            }
        }  
    }
}
