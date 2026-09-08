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
        // transform.forward doit correspondre a l'axe (la longueur) du rondin.
        // Si le rondin n'est pas oriente le long de son axe local Z, ajuste
        // ici avec transform.right ou une autre reference selon l'orientation.
        if (cam != null) cam.EnterLog(transform.forward);
    }

    void OnTriggerExit(Collider other)
    {
        var cam = other.GetComponentInParent<CameraPlayer>();
        if (cam != null) cam.ExitLog();
    }
}