using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NARSSensorimotor : MonoBehaviour
{
    private NARSHost _host;

    CharacterController bodyController;

    float TIMER_DURATION = 0.33f; // how often to send inputs to NARS
    float timer = 0;

    public float rotatingAngle = 0f;
    public Vector3 moveVector = Vector3.zero;

    string opToExecute = "";

    // Start is called before the first frame update
    void Start()
    {
        timer = TIMER_DURATION;

        bodyController = GetComponent<CharacterController>();
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

            //DrawRays(true);
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
        GetNARSHost().AddInput(input);
        //inputQueue.Enqueue(input);
    }

    void MoveBody()
    {
        bodyController.SimpleMove(moveVector);
        bodyController.transform.Rotate(0, rotatingAngle, 0);
    }

    // Perception

    private void FixedUpdate()
    {
        DrawRays(false);
    }

    void DrawRays(bool LIDARSense)
    {
        int[] azimuth_degrees = new int[] { 0, 45, 90, 135, 180, 225, 270, 315 };
        int[] altitude_degrees = new int[] { -90, -45, 0, 45, 90 };
        float[] azimuth_radians = azimuth_degrees.Select(d => d * Mathf.Deg2Rad).ToArray();
        float[]  altitude_radians = altitude_degrees.Select(d => d * Mathf.Deg2Rad).ToArray();


        foreach (float azimuthX in azimuth_radians)
        {
            foreach (float altitude in altitude_radians)
            {
                foreach (float azimuthZ in azimuth_radians)
                {
                    // Does the ray intersect any objects excluding the player layer
                    Vector3 rotation = new Vector3(Mathf.Cos(azimuthX), Mathf.Cos(altitude), Mathf.Cos(azimuthZ));
                    int length = 10;
                   // Debug.Log(rotation);
                    Debug.DrawRay(transform.position, transform.TransformDirection(rotation)  * length, Color.green);

                    if (LIDARSense)
                    {
                        RaycastHit hit;
                        // Does the ray intersect any objects excluding the player layer
                        if (Physics.Raycast(transform.position, transform.TransformDirection(rotation), out hit, Mathf.Infinity))
                        {
                            Debug.DrawRay(transform.position, transform.TransformDirection(rotation) * hit.distance, Color.red);
                            //Debug.Log("Did Hit");
                        }
                        else
                        {
                            //Debug.Log("Did not Hit");
                        }
                    }
                }
            }
        }

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
