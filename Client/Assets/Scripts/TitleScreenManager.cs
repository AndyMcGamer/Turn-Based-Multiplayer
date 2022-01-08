using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject[] menuScreens;
    private void Awake()
    {
        menuScreens = GameObject.FindGameObjectsWithTag("Screen");
    }
    public void ChangeScreen(string screenName)
    {
        foreach (GameObject screen in menuScreens)
        {
            if(screen.name == screenName)
            {
                screen.SetActive(true);
            }
            else
            {
                screen.SetActive(false);
            }
        }
    }

}
