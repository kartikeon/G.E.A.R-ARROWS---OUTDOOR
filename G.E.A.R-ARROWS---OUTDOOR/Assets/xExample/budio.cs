using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class budio : MonoBehaviour
{
    public AudioClip a1,a2;

    

    void Start()
    {
         StartCoroutine(Audiog());
    }
    
   

public IEnumerator  Audiog()
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = Resources.Load<AudioClip>("AudioClips/1");
        audio.Play();
        yield return new WaitForSeconds(audio.clip.length);
        audio.clip = a1;
        audio.clip = Resources.Load<AudioClip>("AudioClips/AHEAD");
        audio.Play();
        /*yield return new WaitForSeconds(audio.clip.length);
         audio.clip = a2;
        audio.clip = Resources.Load<AudioClip>("AudioClips/to the LEFT");
        audio.Play();*/
    }
}
