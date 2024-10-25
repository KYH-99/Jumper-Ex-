using UnityEngine;

public class RoomPanelController : MonoBehaviour
{
    private void OnEnable()
    {
        BGMManager.Instance?.StopBGM();
    }
}
