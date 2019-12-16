using UnityEngine;
using UnityEngine.UI;

public class ListingPrefab : MonoBehaviour
{
    public Text playerNameText;
    public Image playerStatus;
    public Button removeButton;

    public void SetPlayerStatus(bool b)
    {
        playerStatus.color = b ? Color.green : Color.red;
    }
}
