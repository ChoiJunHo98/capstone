using System;
using Unity.Mathematics;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float throttle;
    public float steer;
    public float downForce;

    public Transform[] wheels;
    public WheelCollider[] wheelColliders;
    
    private float maxAccel;
    private float maxRot;
    private Rigidbody _rBody;

    private void Start()
    {
        _rBody = GetComponent<Rigidbody>();
        _rBody.centerOfMass += Vector3.down * 0.3f;
        maxAccel = 500f;
        maxRot = 45f;
        throttle = 0f;
        steer = 0f;
        downForce = 0.5f;
    }

    void FixedUpdate()
    {
        Move();
        WheelRotate();
        DownForce();
    }
    
    void Move()
    {
        for(int i = 0; i < 4; i++)
            wheelColliders[i].motorTorque = throttle * maxAccel;

        for (int i = 0; i < 2; i++)
            wheelColliders[i].steerAngle = steer * maxRot;
    }

    void WheelRotate()
    {
        for (int i = 0; i < 4; i++)
        {
            wheelColliders[i].GetWorldPose(out var pos, out var quat);
            wheels[i].transform.position = pos;
            wheels[i].transform.rotation = quat;
        }
    }

    void DownForce()
    {
        _rBody.AddForce(-transform.up * downForce * _rBody.velocity.magnitude * 100f);
    }
    
    public void Reset()
    {
        foreach (var wheel in wheelColliders)
        {
            wheel.motorTorque = 0f;
            wheel.steerAngle = 0f;
            wheel.brakeTorque = 0f;
        }
    }
}