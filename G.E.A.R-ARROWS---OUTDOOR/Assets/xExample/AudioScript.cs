using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioScript : MonoBehaviour
{
   
AudioSource myAudi;

public  AudioClip a1,a2 ;

   void Awake()
   {
       
   }
   
void Start()
    {
       
     d1();

      d2();
     
      d3();
    
    }


    public void d1()
    {
         myAudi = GetComponent<AudioSource>();
         myAudi.clip = Resources.Load<AudioClip>("AudioClips/1");
         myAudi.PlayDelayed(1.0f);
        print("1");
         
    }

    public void d2()
    {
         myAudi = GetComponent<AudioSource>();
         myAudi.clip = Resources.Load<AudioClip>("AudioClips/to the LEFT");
         myAudi.PlayDelayed(1.5f);
         print("to the LEFT");
        // myAudi.Stop(); 
    }

     public void d3()
    {
        
         myAudi = GetComponent<AudioSource>();
         myAudi.clip = Resources.Load<AudioClip>("AudioClips/AHEAD");
         myAudi.Play();
         print("AHEAD");
        
         
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
