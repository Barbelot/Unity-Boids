using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BoidSettings : ScriptableObject {
    // Settings
    [Header("Speed")]
    public float minSpeed = 2;
    public float maxSpeed = 5;
    public float speedNoiseIntensity = 0;
    public float speedNoiseFrequency = 0.1f;

    [Header("Direction")]
    public float directionNoiseIntensity = 0;
    public float directionNoiseFrequency = 0.1f;

    [Header("Perception")]
    public float perceptionRadius = 50f;

    [Header("Avoidance")]
    [Tooltip("Higher value means avoidance happens at lower distance.")]
    public float avoidanceDamping = 2;
    //public Vector2 avoidanceRadius = new Vector2(1, 2);

    [Header("Steering")]
    public float maxSteerForce = 3;

    [Header("Weights")]
    public float alignWeight = 1;
    public float cohesionWeight = 1;
    public float separateWeight = 1;
    public float targetWeight = 1;

    [Header("Target")]
    public float targetCatchupStrength = 0;
    public Vector2 targetCatchupRadiusMinMax = new Vector3(2, 4);

    [Header("Wind")]
    public Vector3 wind = Vector3.zero;

    [Header ("Collisions")]
    public LayerMask obstacleMask;
    public float boundsRadius = .27f;
    public float avoidCollisionWeight = 10;
    public float collisionAvoidDst = 5;

}