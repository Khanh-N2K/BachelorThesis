using UnityEngine;

public class FacingCamWorldCanvas : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - GameManager.Instance.TopdownCam.Cam.transform.position);
    }
}
