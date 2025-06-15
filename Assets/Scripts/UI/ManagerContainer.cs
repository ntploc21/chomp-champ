using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ManagerContainer : MonoBehaviour
{
    void Awake()
    {
        if (FindObjectsOfType<ManagerContainer>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}