using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    public bool LoopShouldEnd;

    //Start is called before the first frame update
    void Start()
    {
        
    }

    IEnumerator GameLoop()
    {
        while(LoopShouldEnd == false)
        {
            //spawn enemies

            //spawn towers

            //Move enemies

            //tick towers

            //Apply effects

            //Damage enemies

            //Remove enemies

            //Remove towers

            yield return null;
        }
    }
}
