using UnityEngine;

public class SendManager : MonoBehaviour
{
    private static SendManager instance;

    private bool canSendClient = true;
    private bool canSendServer = true;

    public static SendManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SendManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(SendManager).Name;
                    instance = obj.AddComponent<SendManager>();
                }
            }
            return instance;
        }
    }

    public bool CanSendClient
    {
        get { return canSendClient; }
        set { canSendClient = value; }
    }

    public bool CanSendServer
    {
        get { return canSendServer; }
        set { canSendServer = value; }
    }
}
