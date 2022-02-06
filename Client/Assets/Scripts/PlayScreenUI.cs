using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayScreenUI : MonoBehaviour
{
    [SerializeField] private Client client;
    [SerializeField] private TMP_InputField inputField;

    public void ConnectToHost()
    {
        var ep = inputField.text;
        client.ConnectToHost(ep);
    }

}
