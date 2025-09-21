using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Wobble : MonoBehaviour
{
    private Renderer m_renderer;
    private Vector3 m_lastPos;
    private Vector3 m_velocity;
    private Vector3 m_lastRot;
    private Vector3 m_angularVelocity;
    public float m_maxWobble = 0.03f;
    public float m_wobbleSpeed = 1f;
    public float m_recovery = 1f;
    private float m_wobbleAmountX = 0;
    private float m_wobbleAmountZ = 0;
    private float m_wobbleAmountToAddX = 0;
    private float m_wobbleAmountToAddZ = 0;
    private float m_pulse = 0;
    private float m_time = 0.5f;

    void Start()
    {
        m_renderer = GetComponent<Renderer>();
    }
    private void Update()
    {
        m_time += Time.deltaTime;

        m_wobbleAmountToAddX = Mathf.Lerp(m_wobbleAmountToAddX, 0, Time.deltaTime * (m_recovery));
        m_wobbleAmountToAddZ = Mathf.Lerp(m_wobbleAmountToAddZ, 0, Time.deltaTime * (m_recovery));


        m_pulse = 2 * Mathf.PI * m_wobbleSpeed;
        m_wobbleAmountX = m_wobbleAmountToAddX * Mathf.Sin(m_pulse * m_time);
        m_wobbleAmountZ = m_wobbleAmountToAddZ * Mathf.Sin(m_pulse * m_time);

 
        m_renderer.material.SetFloat("_WobbleX", m_wobbleAmountX);
        m_renderer.material.SetFloat("_WobbleZ", m_wobbleAmountZ);


        m_velocity = (m_lastPos - transform.position) / Time.deltaTime;
        m_angularVelocity = transform.rotation.eulerAngles - m_lastRot;


        m_wobbleAmountToAddX += Mathf.Clamp((m_velocity.x + (m_angularVelocity.z * 0.2f)) * m_maxWobble, -m_maxWobble, m_maxWobble);
        m_wobbleAmountToAddZ += Mathf.Clamp((m_velocity.z + (m_angularVelocity.x * 0.2f)) * m_maxWobble, -m_maxWobble, m_maxWobble);

        m_lastPos = transform.position;
        m_lastRot = transform.rotation.eulerAngles;
    }

}