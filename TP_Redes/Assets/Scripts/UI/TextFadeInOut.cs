using UnityEngine;
using UnityEngine.UI;

public class TextFadeInOut : MonoBehaviour
{
    private Text textToUse;
    private float counter;

    void Start ()
    {
        textToUse = GetComponent<Text>();
    }

    private void Update()
    {
        counter += 0.01f;
        textToUse.color = new Color(0f, 1f, 1f, Mathf.PingPong(counter * 3, 2));
    }
}
