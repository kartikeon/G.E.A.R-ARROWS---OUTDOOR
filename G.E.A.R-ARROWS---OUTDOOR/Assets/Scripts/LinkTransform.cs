using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinkTransform : MonoBehaviour
{

 public int A,Z;


    void Awake()
 {  
     
     GameObject[] objs = GameObject.FindGameObjectsWithTag("LinkTransform");
     
     if (objs.Length > 1)
     {
       Destroy(this.gameObject);
         
     }
     DontDestroyOnLoad(this);  

 }



 
}


 /* GameObject[] objs = GameObject.FindGameObjectsWithTag("LinkTransform");
     
     if (objs.Length > 1)
     {
       Destroy(this.gameObject);
         
     }
     DontDestroyOnLoad(this); */ 
