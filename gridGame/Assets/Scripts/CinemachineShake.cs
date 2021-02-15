using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake instance { get; private set; }

    private CinemachineVirtualCamera cvc;
    private float shakeTimer;

    private void Awake()
    {
        instance = this;
        cvc = GetComponent<CinemachineVirtualCamera>();
    }

    public void ShakeCamera(float intensity, float time)
    {
        CinemachineBasicMultiChannelPerlin noise = cvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = intensity;

        shakeTimer = time;
    }

    public void Update()
    {
        if(shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if(shakeTimer <= 0)
            {
                CinemachineBasicMultiChannelPerlin noise = cvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                noise.m_AmplitudeGain = 0.0f;
            }
        }
    }
}
