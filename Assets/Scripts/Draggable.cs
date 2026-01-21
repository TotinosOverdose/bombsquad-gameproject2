using UnityEngine;

public class Draggable : MonoBehaviour
{

    bool onPlanters = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Planter")
        {
            onPlanters = true;
            print("on planter!");
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.tag == "Planter")
        {
            onPlanters = false;
            print("not on planter");
        }
    }

    public void CheckIfPlanted()
    {
        //aaaa
    }
}
