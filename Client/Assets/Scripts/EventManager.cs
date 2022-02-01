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
    #endregion

    #region Event Invokers
    public static void InvokeLoadConfirmed()
    {
        if(OnLoadConfirmed != null)
        {
            OnLoadConfirmed();
        }
    }

    public static void InvokeChangeLoad()
    {
        if(ChangeLoad != null)
        {
            ChangeLoad();
        }
    }
    #endregion
}
