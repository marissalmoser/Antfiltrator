/**********************************************************************************

// File Name :         LightBehavior.cs
// Author :            Marissa Moser
// Creation Date :     September 25, 2023
//
// Brief Description : This script contains the behavior for the light objects.
It reacts to the light blockde event to put the bee asleep and turn off the light.

**********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBehavior : MonoBehaviour
{
    private LevelManager lm;
    [SerializeField] private GameObject Bee;
    [SerializeField] private GameObject Bee2;
    [SerializeField] private GameObject levelManager;
    [SerializeField] private GameObject lightsBeeVisionObj;
    [SerializeField] private GameObject hiveObject;
    [SerializeField] private bool hiveInPlace;
    [SerializeField] private LayerMask hiveLM;
    [SerializeField] private Vector2 detectorSize;
    [SerializeField] private bool hivePlacedVertically;
    [SerializeField] private bool queensLight;
    [SerializeField] private bool hasTwoBees;
    [SerializeField] public bool isFirstLight;

    private void Start()
    {
        PlayerBehavior.ObjectDropped += LightBlocked;
        StartCoroutine(ConstantDetection());
        lm = levelManager.GetComponent<LevelManager>();
    }

    private void LightBlocked()
    {
        if (hiveInPlace)
        {
            if (!hivePlacedVertically) 
            {
                if(hiveObject.transform.parent.transform.rotation.z == 0 || hiveObject.transform.parent.transform.rotation.z == 1)
                {
                    if (queensLight)
                    {
                        Bee.GetComponent<QueenBeeBehavior>().LightShutOff();
                    }
                    else
                    {
                        Bee.GetComponent<BeeStates>().FSM(BeeStates.States.Sleep);
                        if (hasTwoBees)
                        {
                            Bee2.GetComponent<BeeStates>().FSM(BeeStates.States.Sleep);
                        }
                    }

                    AudioManager.Instance.Play("LightBlocked");
                    lm.BeeVisionObjects.Remove(hiveObject);
                    Destroy(hiveObject.transform.parent.gameObject);
                    lm.BeeVisionObjects.Remove(lightsBeeVisionObj);
                    Destroy(gameObject);
                }
                
            }
            else if (hivePlacedVertically)
            {
                if (hiveObject.transform.parent.transform.rotation.z < 1 && hiveObject.transform.parent.transform.rotation.z > -1 && hiveObject.transform.parent.transform.rotation.z != 0)
                {
                    if (queensLight)
                    {
                        Bee.GetComponent<QueenBeeBehavior>().LightShutOff();
                    }
                    else
                    {
                        Bee.GetComponent<BeeStates>().FSM(BeeStates.States.Sleep);
                        if (hasTwoBees)
                        {
                            Bee2.GetComponent<BeeStates>().FSM(BeeStates.States.Sleep);
                        }
                    }

                    AudioManager.Instance.Play("LightBlocked");
                    lm.BeeVisionObjects.Remove(hiveObject);
                    Destroy(hiveObject.transform.parent.gameObject);
                    lm.BeeVisionObjects.Remove(lightsBeeVisionObj);
                    Destroy(gameObject);
                }
            }
        }
    }

    IEnumerator ConstantDetection()
    {
        while (true)
        {
            Collider2D collider = Physics2D.OverlapBox(transform.position, detectorSize, 0, hiveLM);
            if (collider != null)
            {
                hiveInPlace = true;
                hiveObject = collider.gameObject;
                if(isFirstLight)
                {
                    Invoke("LightBlocked", 0.2f);
                }
            }
            else if(!isFirstLight)
            {
                hiveInPlace = false;
                hiveObject = null;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!queensLight && collision.CompareTag("Player") && Bee.GetComponent<BeeStates>().StartInToPatrol)
        {
            Bee.GetComponent<BeeStates>().FSM(BeeStates.States.ToPatrol);

            if(hasTwoBees)
            {
                Bee2.GetComponent<BeeStates>().FSM(BeeStates.States.ToPatrol);
            }
        }
        if (isFirstLight && collision.CompareTag("Player"))
        {
            if(hiveObject.GetComponent<Rigidbody2D>() != null)
            {
                hiveObject.GetComponent<Rigidbody2D>().gravityScale = 0.7f;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position, detectorSize);
    }

    private void OnDestroy()
    {
        PlayerBehavior.ObjectDropped -= LightBlocked;
    }
}
