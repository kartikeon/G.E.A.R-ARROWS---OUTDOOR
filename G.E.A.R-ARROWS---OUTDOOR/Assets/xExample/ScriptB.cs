using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScriptB : MonoBehaviour
{
    public TextMeshProUGUI txtad, txtsb;
    public int a , b, c, d, x, y, f, h;

    public ScriptA dig;
    public ScriptC gid;

    public void Add()
    {
        x = a + b;
    }

    public void sub()
    {
        y = a - b;
 
    }

     public void addval()
    {
        a=5;
        b=5;

    }

    public void RunBTN()
    {
      //  dig = new ScriptA();
       dig = FindObjectOfType<ScriptA>();       
       dig.addval();
       //addval();
       

        x = dig.a + dig.b;
        txtad.text = "a+b = " + x;
         print("add a+b :" + x);

      //gid = new ScriptC();
       gid = FindObjectOfType<ScriptC>();
        gid.subval();
       y = gid.c;
       h = gid.d;
        f = y - h;
       txtsb.text ="a-b = " + f;
       print("sub a-b :" + f);
    }


       public  void Start()
      {
        //RunBTN();

      }

}
