using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler
{
    private AudioSource _audioSource;
    private AudioClip _hoverClip;
    private AudioClip _clickClip;
    
    private void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _hoverClip = Resources.Load<AudioClip>(Constants.HOVER_SOUND);
        _clickClip = Resources.Load<AudioClip>(Constants.CLICK_SOUND);
        GetComponent<Button>().onClick.AddListener(ClickSound);
    }

    private void ClickSound()
    {
        _audioSource.PlayOneShot(_clickClip);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _audioSource.PlayOneShot(_hoverClip);
    }
}
