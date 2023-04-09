using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices;
using UnityEngine;

public class Tester : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EOSSDKComponent.Instance.LoginToEos(ExternalCredentialType.DeviceidAccessToken, onLoginSuccess: info => {
            Debug.Log(info.ResultCode + " " + info.LocalUserId);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
