using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour {

    const int threadGroupSize = 1024;

	[Header("Settings")]
    public BoidSettings settings;
    public ComputeShader compute;
    public bool useGPU = false;

	[Header("Target")]
	public Transform target;

    [Header("Spawn")]
    public Boid prefab;
    public float spawnRadius = 10;
    public int spawnCount = 10;

    [Header("Debug")]
    public bool spawnBoid = false;
    public bool destroyBoid = false;

    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size {
            get {
                return sizeof(float) * 3 * 5 + sizeof(int);
            }
        }
    }

    Boid[] boids;
    Boid[] tmpBoids;

    ComputeBuffer boidBuffer;
    BoidData[] boidData;

    void Start() {

        boids = new Boid[0];

        for(int i=0; i<spawnCount; i++) {
            SpawnBoid();
        }
    }

    void Update () {

		//Debug
		if (spawnBoid) { SpawnBoid(); spawnBoid = false; }
        if (destroyBoid) { RemoveBoid(0); destroyBoid = false; }

        //Update boids
        if (boids != null) {
            if (boids.Length > 0) {

                int numBoids = boids.Length;

                if (boidBuffer == null || boidData == null) {
                    CreateBuffer();
                } else if (boidBuffer.count != numBoids || boidData.Length != numBoids) {
                    CreateBuffer();
                }

                for (int i = 0; i < numBoids; i++) {
                    boidData[i].position = boids[i].position;
                    boidData[i].direction = boids[i].forward;
					boidData[i].numFlockmates = 0;
					boidData[i].flockHeading = Vector3.zero;
					boidData[i].flockCentre = Vector3.zero;
					boidData[i].avoidanceHeading = Vector3.zero;
				}

                if (useGPU) {
                    boidBuffer.SetData(boidData);

                    compute.SetBuffer(0, "boids", boidBuffer);
                    compute.SetInt("numBoids", numBoids);
                    compute.SetVector("radii", new Vector4(settings.perceptionRadius, 0, 0, 0));

                    int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
                    compute.Dispatch(0, threadGroups, 1, 1);

                    boidBuffer.GetData(boidData);

                } else {

                    float sqrPerceptionRadius = settings.perceptionRadius * settings.perceptionRadius;

                    for (int i = 0; i < numBoids; i++) {
                        for(int j = 0; j < numBoids; j++) {
                            if(j != i) {
                                BoidData boidB = boidData[j];
                                Vector3 offset = boidB.position - boidData[i].position;
                                float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

                                if (sqrDst < sqrPerceptionRadius) {
                                    boidData[i].numFlockmates += 1;
                                    boidData[i].flockHeading += boidB.direction;
                                    boidData[i].flockCentre += boidB.position;
                                    boidData[i].avoidanceHeading += -offset.normalized * Mathf.Exp(-sqrDst * settings.avoidanceDamping);
                                }
                            }
						}
                    }
                }

                for (int i = 0; i < numBoids; i++) {
                    boids[i].avgFlockHeading = boidData[i].flockHeading;
                    boids[i].centreOfFlockmates = boidData[i].flockCentre;
                    boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                    boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

                    boids[i].UpdateBoid();
                }
            }
        }
    }

	private void OnDestroy() {

        ReleaseBuffer();
	}

	void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }

    void CreateBuffer() {
        ReleaseBuffer();

        boidData = new BoidData[boids.Length];
        boidBuffer = new ComputeBuffer(boids.Length, BoidData.Size);
    }

    void ReleaseBuffer() {

        if (boidBuffer != null)
            boidBuffer.Release();
	}

    public Boid SpawnBoid() {

        return SpawnBoid(transform.position + Random.insideUnitSphere * spawnRadius);
    }

    public Boid SpawnBoid(Vector3 position)
    {
	    return SpawnBoid(position, Random.insideUnitSphere);
    }

    public Boid SpawnBoid(Vector3 position, Vector3 direction)
    {

	    if (boids == null)
		    return null;

	    tmpBoids = boids;
	    boids = new Boid[tmpBoids.Length + 1];

	    for (int i = 0; i < tmpBoids.Length; i++) {
		    boids[i] = tmpBoids[i];
		    boids[i].id = i;
	    }

	    Boid boid = Instantiate(prefab, transform);
	    boid.settings = settings;
	    boid.target = target;
	    boid.id = tmpBoids.Length;
	    boid.Initialize(position, direction.normalized);
	    boids[tmpBoids.Length] = boid;

	    return boid;
    }

    public void RemoveBoid(int id) {

        if (id >= boids.Length)
            return;

        Destroy(boids[id].gameObject);

        tmpBoids = boids;
        boids = new Boid[tmpBoids.Length - 1];

        bool skipped = false;

        for (int i = 0; i < tmpBoids.Length; i++) {
            if (i == id) {
                skipped = true;
            } else {
                int j = skipped ? i - 1 : i;
                boids[j] = tmpBoids[i];
                boids[j].id = j;
            }
        }
    }
}