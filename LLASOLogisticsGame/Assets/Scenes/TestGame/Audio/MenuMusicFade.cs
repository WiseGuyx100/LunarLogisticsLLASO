using UnityEngine;

public class MenuMusicFade : MonoBehaviour
{
    public float targetVolume = 0.4f;
    public float fadeSeconds = 3f;

    private AudioSource source;

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (source.volume < targetVolume)
        {
            source.volume = Mathf.MoveTowards(
                source.volume,
                targetVolume,
                (targetVolume / fadeSeconds) * Time.deltaTime);
        }
    }
}