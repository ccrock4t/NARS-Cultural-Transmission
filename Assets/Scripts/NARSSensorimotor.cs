using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NARSSensorimotor : MonoBehaviour
{
    private NARSHost _host;

    CharacterController bodyController;

    float TIMER_DURATION = 0.5f; // how often to queue sensory inputs
    float timer = 0;

    public float rotatingAngle = 0f;
    public Vector3 moveVector = Vector3.zero;
    int layerMask;

    string opToExecute = "";

    List<Dome> domes;

    [SerializeField]
    public Teacher teacher;

    //raycast directions
    int[] azimuth_degrees;
    int[] altitude_degrees;
    float[] azimuth_radians;
    float[] altitude_radians;
    // Start is called before the first frame update
    void Start()
    {
        timer = TIMER_DURATION;

        bodyController = GetComponent<CharacterController>();

        //set up raycast directions
        this.azimuth_degrees = new int[] { 0, 90, 180, 270 };
        this.altitude_degrees = new int[] { -90, -45, 0, 45, 90 };
        this.azimuth_radians = azimuth_degrees.Select(d => d * Mathf.Deg2Rad).ToArray();
        this.altitude_radians = altitude_degrees.Select(d => d * Mathf.Deg2Rad).ToArray();

        // Set up raycast layermask
        // ---------
        // Bit shift the index of the layer (layer 8, the player layer) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        this.layerMask = ~layerMask;

        // Cache objects we might raycast (since GetComponent<> is computationally expensive)
        GameObject domeGO = GameObject.Find("Domes");
        if (domeGO == null) Debug.LogError("Domes object not found.");
        domes = new List<Dome>();
        foreach (Dome dome in domeGO.GetComponentsInChildren<Dome>())
        {
            domes.Add(dome);
        }
    }

    public void SetNARSHost(NARSHost host)
    {
        _host = host;
    }

    public NARSHost GetNARSHost()
    {
        return _host;
    }


    // Update is called once per frame
    void Update()
    {
        //Narsese events
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {

            Lidar();
            RemindGoal();

            timer = TIMER_DURATION;
        }

        if (GetNARSHost().type == NARSHost.NARSType.ONA)
        {
            GetNARSHost().AddInferenceCycles(1);
        }

        if (GetNARSHost().type == NARSHost.NARSType.NARS)
        {
            GetNARSHost().AddInferenceCycles(2);
        }

        //execute queued op if it exists
        if (opToExecute.Length > 0)
        {
            char s = opToExecute[1];
            opToExecute = char.ToUpper(s) + opToExecute.Substring(2);
            Invoke(opToExecute, 0f);
            opToExecute = "";
        }

        //update body
        MoveBody();

    }

    void QueueInput(string input)
    {
        //Debug.Log("queueing " + input);
        GetNARSHost().QueueInput(input);
    }

    void MoveBody()
    {
        bodyController.SimpleMove(moveVector);
        bodyController.transform.Rotate(0, rotatingAngle, 0);
    }

    // Perception

    void Lidar()
    {
        int events = 0;
        Vector3Int lidarIDIndex = Vector3Int.zero;
        foreach (float azimuthX in azimuth_radians)
        {
            foreach (float altitude in altitude_radians)
            {
                foreach (float azimuthZ in azimuth_radians)
                {
                    // Does the ray intersect any objects excluding the player layer
                    Vector3 direction = new Vector3(Mathf.Cos(azimuthX), Mathf.Sin(altitude), Mathf.Cos(azimuthZ));
                    int length = 10;

                    RaycastHit hit;
                    // Does the ray intersect any objects excluding the player layer
                    if (Physics.Raycast(transform.position, transform.TransformDirection(direction), out hit, Mathf.Infinity, this.layerMask))
                    {
                        
                        Vector3 lidarID = new Vector3(this.azimuth_degrees[lidarIDIndex.x % this.azimuth_degrees.Length], 
                            this.altitude_degrees[lidarIDIndex.y % this.altitude_degrees.Length], 
                            this.azimuth_degrees[lidarIDIndex.z % this.azimuth_degrees.Length]);
                        string rayString = GetLidarHitString(lidarID, hit);
                        if(rayString.Length > 0)
                        {
                            QueueInput("<" + rayString + " --> hit>. :|:");
                            Debug.DrawRay(transform.position, transform.TransformDirection(direction) * length, Color.red, TIMER_DURATION);
                            events++;
                        }
                        else
                        {
                            Debug.DrawRay(transform.position, transform.TransformDirection(direction) * length, Color.green, TIMER_DURATION);
                        }
                        //subject += rayString;
                       
                        //Debug.Log("Did Hit");
                    }
                    else
                    {
                        Debug.DrawRay(transform.position, transform.TransformDirection(direction) * length, Color.green, TIMER_DURATION);
                        //Debug.Log("Did not Hit");
                    }
                    
                    lidarIDIndex.z++;
                }
                lidarIDIndex.y++;
            }
            lidarIDIndex.x++;
        }

        //QueueInput("<" + subject + " --> hit>. :|:");
    }

    public string GetLidarHitString(Vector3 lidarID, RaycastHit hit)
    {
        string lidarString = "Lidar" + lidarID.x + "x" + lidarID.y + "x" + lidarID.z;
        lidarString = lidarString.Replace("-", "m");
        foreach (Dome dome in this.domes)
        {
            if(hit.transform == dome.transform)
            {
                string domeColorString = dome.color.ToString();
                return lidarString + domeColorString;
            }
        }

        if (hit.transform == teacher.transform)
        {
            return lidarString + "teacher";
        }

        return "";
    }


    public void TeachInitialKnowledge()
    {
        //GetNARSHost().AddInput("<(&/,<(*,{SELF},{$sth}) --> seesLeft>,^lookLeft) =/> <(*,{SELF},{$sth}) --> seesCenter>>.");
        //GetNARSHost().AddInput("<(&/,<(*,{SELF},{$sth}) --> seesRight>,^lookRight) =/> <(*,{SELF},{$sth}) --> seesCenter>>.");
        //GetNARSHost().AddInput("<(&/,<(*,{SELF},{$sth}) --> seesCenter>,^moveForward) =/> <(*,{SELF},{$sth}) --> touching>>.");
        //GetNARSHost().AddInput("<(&/,(--,<(*,{SELF},{$sth}) --> sees>),^lookRight) =/> <(*,{SELF},{$sth}) --> sees>>.");
    }


    // ============== Motor Functions (OPERATIONS) ============== 
    // NOTE: Function names must match NARS operation names
    public void SetOp(string op)
    {
        opToExecute = op;
    }

    float moveSpeed = 1.5f, rotateSpeed = 0.66f;

    public void LookLeft()
    {
        rotatingAngle = -rotateSpeed;
        moveVector = Vector3.zero;
    }

    public void LookRight()
    {
        rotatingAngle = rotateSpeed;
        moveVector = Vector3.zero;
    }

    public void MoveForward()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        moveVector = forward * moveSpeed;
        rotatingAngle = 0;
    }

    // Rewards

    public void RemindGoal()
    {
        //TODO
        string goal = "<{SELF} --> [good]>! :|:";
        QueueInput(goal);
    }

    public void Punish()
    {
        Debug.Log("BAD NARS");
        //NARSHost.GetInstance().AddInput("(--,<{SELF} --> [good]>). :|:");
        //NARSHost.GetInstance().AddInput("<{SELF} --> [bad]>. :|:");
    }

    public void Praise()
    {
        Debug.Log("GOOD " + GetNARSHost().type.ToString());
        QueueInput("<{SELF} --> [good]>. :|:");
    }
}
