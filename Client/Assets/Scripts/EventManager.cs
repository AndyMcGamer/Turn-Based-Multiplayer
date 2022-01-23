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
    #endregion

    #region Event Invokers
    public static void InvokeLoadConfirmed()
    {
        if(OnLoadConfirmed != null)
        {
            OnLoadConfirmed();
        }
    }
    #endregion
}
