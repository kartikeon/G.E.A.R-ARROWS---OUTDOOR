using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;



public class MainmenuScript : MonoBehaviour
{
   // public AudioClip a1,a2;

	public TextMeshProUGUI txt;

    public LinkTransform link; 


    public int y;
   

	public TMP_Dropdown DropdownBox1 , DropdownBox2 ,DropdownBox3; 
    public int i,j,k,x;
   

   

    void Start()
    {

   // StartCoroutine(Audiog());
     
    }

   /* public IEnumerator Audiog()
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = Resources.Load<AudioClip>("AudioClips/hi there, welcome to GEAR Arrows indoor");
        audio.Play();
        yield return new WaitForSeconds(audio.clip.length);
        audio.clip = a1;
        audio.clip = Resources.Load<AudioClip>("AudioClips/please choose your initial point and destination");
        audio.Play();
        yield return new WaitForSeconds(audio.clip.length);
         audio.clip = a2;
        audio.clip = Resources.Load<AudioClip>("AudioClips/please choose your arrows");
        audio.Play();
    }*/

    public void BackBtn()
    {
      SceneManager.LoadScene(0);
    }

    public void Dropdown1()
    {
        i = DropdownBox1.value;
    }

    public void Dropdown2()
    {
        j = DropdownBox2.value;
    }

    public void Dropdown3()
    {
        k = DropdownBox3.value;
    }

    public void MatrixValue()
    {
        if(i == 0 || j == 0)
        {
            txt.text = "\n  Please Select the Route";

        }
        
     else
     {
       if (i==j)
        {
            txt.text = "\n INVALID ROUTE ";
        }

        else 
        {
             x = (i*10) + j; 
             txt.text = "\n CORRECT ROUTE \n Value: " + x;
             NavigateBtn();
        }

    
     }

    }

   

    public void NavigateBtn()
    {
       
        print("playing....Done");

      
         SceneManager.LoadScene(1);
          
         y = x;
        link.A = y;
        link.Z = k;
    }
    
	void Update () 
    {

    }
   


}
