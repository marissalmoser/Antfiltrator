/**********************************************************************************

// File Name :         PlayerBehavior.cs
// Author :            Marissa Moser
// Creation Date :     September 19, 2023
//
// Brief Description : 

**********************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    public GameManager GameManager;
    private GameManager gm;

    void Start()
    {
        gm = GameManager.GetComponent<GameManager>();
    }

    void Update()
    {
        
    }
}
