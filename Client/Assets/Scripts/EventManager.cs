using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    #region References
    [SerializeField] private Client client;
    #endregion

    #region Events
    public delegate void EventAction();
    public static event EventAction OnLoadConfirmed;
    public static event EventAction ChangeLoad;
    public static event EventAction GoToMain;
    public static event EventAction OnPlayerListUpdate;
    #endregion

    #region Event Invokers
    public static void InvokeLoadConfirmed()
    {
        OnLoadConfirmed?.Invoke();
    }

    public static void InvokeChangeLoad()
    {
        ChangeLoad?.Invoke();
    }

    public static void InvokeGoToMain()
    {
        GoToMain?.Invoke();
    }

    public static void InvokePlayerListUpdate()
    {
        OnPlayerListUpdate?.Invoke();
    }
    #endregion
}
