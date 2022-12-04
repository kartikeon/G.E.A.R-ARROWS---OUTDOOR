using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class cudio : MonoBehaviour
{
    AudioSource audiok;
         public AudioClip engineStartClip;
         public AudioClip engineLoopClip;
         void Start()
         {
             GetComponent<AudioSource> ().loop = true;
             StartCoroutine(playEngineSound());
         }
 
         IEnumerator playEngineSound()
         {
             audiok.clip = engineStartClip;
             audiok.clip = Resources.Load<AudioClip>("AudioClips/1");
             audiok.Play();
             yield return new WaitForSeconds(audiok.clip.length);
             audiok.clip = engineLoopClip;
             audiok.clip = Resources.Load<AudioClip>("AudioClips/to the LEFT");
             audiok.Play();
         }
    
}
