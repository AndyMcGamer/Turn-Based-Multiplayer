using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] playerNames;

    [SerializeField] private Client client;

    private void OnEnable()
    {
        foreach (var item in playerNames)
        {
            item.text = "";
        }
        EventManager.OnPlayerListUpdate += LoadPlayerList;
    }

    private void LoadPlayerList()
    {
        PlayerManager playerManager = client.GetPlayerManager();
        for (int i = 0; i < playerNames.Length; i++)
        {
            playerNames[i].text = (playerManager[i] != null) ? playerManager[i].Name : "";
        }
        
    }

    private void OnDisable()
    {
        EventManager.OnPlayerListUpdate -= LoadPlayerList;
    }
}
