using Photon.Pun;
using UnityEngine;

public class PlayerName : MonoBehaviourPun
{
    private PhotonView _view;

    public void SetText(string text, string color)
    {
        var textToUse = text;
        
        if (textToUse.Length > 10)
            textToUse = text.Substring(0, 10);
        
        _view.RPC("SetMyText", RpcTarget.AllBuffered, textToUse, color);
    }

    [PunRPC]
    private void SetMyText(string text, string color)
    {
        GetComponent<TextMesh>().text = text;
        
        if (ColorUtility.TryParseHtmlString(color, out var playerColor))
            GetComponent<TextMesh>().color = playerColor;
    }
    
    public void SetView()
    {
        _view = GetComponent<PhotonView>();
    }
}
