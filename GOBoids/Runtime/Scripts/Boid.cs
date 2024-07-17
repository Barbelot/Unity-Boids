using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour {

	[HideInInspector] public BoidSettings settings;
	[HideInInspector] public Transform target;

    // Body
    [Tooltip("Transform moved by the boid. If null this script transform is moved instead.")] public Transform boidBody;

    public Vector3 additionalVelocityDirection;
    public float additionalVelocityIntensity;

    // State
    [HideInInspector]
    public int id;
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;

    // To update:
    //Vector3 acceleration;
    [HideInInspector]
    public Vector3 avgFlockHeading;
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    [HideInInspector]
    public Vector3 centreOfFlockmates;
    [HideInInspector]
    public int numPerceivedFlockmates;

    // Cached
    Transform cachedTransform;


    float _random;

    public void Initialize (Vector3 spawnPosition, Vector3 spawnForward) {

        cachedTransform = GetTransform();

        _random = Random.Range(0.0f, 1.0f);
       
        position = cachedTransform.position = spawnPosition;
        forward = cachedTransform.forward = spawnForward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = GetTransform().forward * startSpeed;
    }

    public void UpdateBoid () {

        //Wind
        GetTransform().Translate(settings.wind * Time.deltaTime, Space.World);

        Vector3 acceleration = Vector3.zero;

        if (target != null) {
            Vector3 offsetToTarget = (target.position - position);
            float targetWeight = settings.targetWeight;

            if (settings.targetCatchupStrength > 0)
            {
	            targetWeight += Mathf.Lerp(0, settings.targetCatchupStrength,
		            Mathf.InverseLerp(settings.targetCatchupRadiusMinMax.x, settings.targetCatchupRadiusMinMax.y,
			            offsetToTarget.magnitude));
            }

            acceleration = SteerTowards(offsetToTarget) * targetWeight;
        }

        if (numPerceivedFlockmates != 0) {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

            var alignmentForce = SteerTowards (avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards (offsetToFlockmatesCentre) * settings.cohesionWeight;
            var separationForce = SteerTowards (avgAvoidanceHeading) * settings.separateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += separationForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        if (additionalVelocityIntensity != 0 && additionalVelocityDirection.sqrMagnitude != 0)
        {
	        acceleration += SteerTowards(additionalVelocityDirection) * additionalVelocityIntensity;
        }

        velocity += acceleration * Time.deltaTime;

        velocity += new Vector3((Mathf.PerlinNoise(Time.time * settings.directionNoiseFrequency, _random * 10) - 0.5f) * settings.directionNoiseIntensity,
                                (Mathf.PerlinNoise(Time.time * settings.directionNoiseFrequency, _random * 100) - 0.5f) * settings.directionNoiseIntensity,
                                (Mathf.PerlinNoise(Time.time * settings.directionNoiseFrequency, _random * 1000) - 0.5f) * settings.directionNoiseIntensity);


        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
        speed += (Mathf.PerlinNoise(Time.time * settings.speedNoiseFrequency, _random * 10000) - 0.5f) * settings.speedNoiseIntensity;
        velocity = dir * speed;

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
    }

    bool IsHeadingForCollision () {
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask)) {
            return true;
        } 
        return false;
    }

    Vector3 ObstacleRays () {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = cachedTransform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (position, dir);
            if (!Physics.SphereCast (ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask)) {
                return dir;
            }
        }

        return forward;
    }

    Vector3 SteerTowards (Vector3 vector) {
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }

    Transform GetTransform() {

        return boidBody ? boidBody : transform;
	}
}
