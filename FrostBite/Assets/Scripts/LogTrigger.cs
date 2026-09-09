using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LogTrigger : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var cam = other.GetComponentInParent<CameraPlayer>();
        if (cam != null) cam.EnterLog(transform.forward);
    }

    void OnTriggerExit(Collider other)
    {
        var cam = other.GetComponentInParent<CameraPlayer>();
        if (cam != null) cam.ExitLog();
    }
}
