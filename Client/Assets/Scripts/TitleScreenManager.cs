using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject[] menuScreens;
    private void Awake()
    {
        menuScreens = GameObject.FindGameObjectsWithTag("Screen");
        ChangeScreen("MainScreen");
    }

    private void OnEnable()
    {
        EventManager.OnLoadConfirmed += ConfirmLoad;
        EventManager.ChangeLoad += Load;
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

    private void Load()
    {
        ChangeScreen("LoadingScreen");
    }

    private void ConfirmLoad()
    {
        ChangeScreen("LobbyScreen");
    }

    private void OnDisable()
    {
        EventManager.OnLoadConfirmed -= ConfirmLoad;
        EventManager.ChangeLoad -= Load;
    }
}
