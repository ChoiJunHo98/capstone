using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class NavCar : Agent
{
    public Transform[] targets;
    public Transform[] spawns;
    
    private Rigidbody rBody;
    private CarController ctrl;
    private NavMeshPath path;
    private EnvironmentParameters resetParams;
    private Transform target;
    
    private Vector3 tPos;
    private float pathDist;
    private float preDist;
    private float curDist;
    private float discount = 0.5f;
    private int idx;
    private int setDest;
    public override void Initialize()
    {
        path = new NavMeshPath();
        ctrl = GetComponentInChildren<CarController>();
        rBody = GetComponent<Rigidbody>();
        resetParams = Academy.Instance.EnvironmentParameters;
        setDest = targets.Length / 2;
    }

    public override void OnEpisodeBegin()
    {
        var max = Random.Range(1, setDest + 1) * 2;
        var min = max - 2;

        var sIdx = Random.Range(min, max);
        var tIdx = Random.Range(min, max);
        
        transform.position = spawns[sIdx].position;
        transform.rotation = spawns[sIdx].rotation;
        target = targets[tIdx];
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        ctrl.Reset();
        MakeNav();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 carInfo = new Vector2(ctrl.wheelColliders[0].rpm, ctrl.wheelColliders[1].rpm);
        Vector3 direction = path.corners[idx] - transform.position;
        direction.y = 0f;
        
        Quaternion heading = Quaternion.LookRotation(direction.normalized);
        
        // sensor.AddObservation(transform.position)
        // sensor.AddObservation(path.corners[pathInfo.idx]);
        
        sensor.AddObservation(transform.rotation);
        sensor.AddObservation(rBody.velocity);
        sensor.AddObservation(rBody.angularVelocity);
        sensor.AddObservation(carInfo);
        sensor.AddObservation(heading);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var continuousAction = actions.ContinuousActions;
        ctrl.throttle = Mathf.Clamp(continuousAction[0], -1f, 1f);
        ctrl.steer = Mathf.Clamp(continuousAction[1], -1f, 1f);
        
        CalcReward();
#if UNITY_EDITOR
        DrawPath();
#endif
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionOut = actionsOut.ContinuousActions;
        continuousActionOut[0] = Input.GetAxis("Vertical");
        continuousActionOut[1] = Input.GetAxis("Horizontal");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("wall"))
        {
            AddReward(-10f);
            EndEpisode();   
        }
        else if (other.gameObject.CompareTag("lane"))
            AddReward(-0.07f * discount);
    }
    
    private void CalcReward()
    {
        float c_dist= Vector3.Distance(transform.position, tPos);
        curDist = pathDist + c_dist;
        
        float reward = Mathf.Clamp(preDist - curDist, -1f, 1f);
        
        if (c_dist > 1.5f)
        {
            AddReward(reward);
            AddReward(-0.1f * discount);
            preDist = curDist;
        }
        else if (idx + 1 < path.corners.Length)
        {
            AddReward(1f);
            idx += 1;
            tPos = path.corners[idx];
            
            pathDist = 0f;
            for (int i = idx; i < path.corners.Length - 1; i++)
                pathDist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            preDist = pathDist + Vector3.Distance(transform.position, tPos);
        }
        else
        {
            AddReward(10f);
            EndEpisode();
        }

    }
    
    private void MakeNav()
    {
        int layerMask = 1 << LayerMask.NameToLayer("Default");
        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f, layerMask);
        bool isPath = NavMesh.CalculatePath(hit.point, target.position, NavMesh.AllAreas, path);
        idx = 0;
        
        if (!isPath)
            EpisodeInterrupted();
        else
        {
            tPos = path.corners[idx];
            
            if (path.corners.Length != 1)
                for (int i = 1; i < path.corners.Length - 1; i++)
                    pathDist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            
            preDist = pathDist + Vector3.Distance(transform.position, tPos);
        }
    }

    private void DrawPath()
    {
        for (int i = idx; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 0.1f, true);
        Debug.DrawLine(transform.position, path.corners[idx], color: Color.blue, 0.1f, true);
    }
    
    // private int SetParams()
    // {
    //     int lesson = (int)resetParams.GetWithDefault("Distance", 1);
    //     int p_idx = 0;
    //
    //     // CarEnv2
    //     switch (lesson)
    //     {
    //         case 1:
    //             p_idx = Random.Range(0, 2);
    //             break;
    //         case 2:
    //             p_idx = Random.Range(2, 4);
    //             break;
    //         case 3:
    //             p_idx = Random.Range(4, 6);
    //             break;
    //         case 4:
    //             p_idx = Random.Range(6, 8);
    //             break;
    //     }
    //     return p_idx;
    // }
}
