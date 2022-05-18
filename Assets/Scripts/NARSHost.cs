using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.UI;

public class NARSHost : MonoBehaviour
{
    public enum NARSType : int
    {
        NARS, ONA
    }

    public NARSType type;
    NARSSensorimotor _sensorimotor;
    Process process = null;
    StreamWriter inputStreamWriter;

    StreamWriter statisticsStreamwriter;

    Queue<string> inputQueue;

    //UI output text
    string lastOperationTextForUI = "";
    bool operationUpdated = false;

    float inputTimer = 0;
    const float INPUT_TIMER_DURATION = 0.03f; 

    //Babbling
    float babbleTimer = 0;
    public int babblesRemaining = 0;
  
    private void Start()
    {
        Application.targetFrameRate = 60;
        
        switch (type)
        {
            case NARSType.NARS:
                LaunchNARS();
                babblesRemaining = 120;
                break;
            case NARSType.ONA:
                LaunchONA();
                break;
            default:
                break;
        }

        _sensorimotor = GetComponent<NARSSensorimotor>();
        _sensorimotor.SetNARSHost(this);

        this.inputQueue = new Queue<string>();

        //_sensorimotor.TeachInitialKnowledge();
    }

    private NARSSensorimotor GetSensorimotor()
    {
        return _sensorimotor;
    }

    private void Update()
    {
        if (operationUpdated)
        {
            string text = "";
            switch (type)
            {
                case NARSType.NARS:
                    text = "NARS Operation:\n";
                    break;
                case NARSType.ONA:
                    text = "ONA Operation:\n";
                    break;
                default:
                    break;
            }
            text += lastOperationTextForUI;
            operationUpdated = false;
        }

        if (type == NARSType.NARS)
        {
            babbleTimer -= Time.deltaTime;
            if (babblesRemaining > 0 && babbleTimer <= 0f)
            {
                Babble();
                babbleTimer = 1.0f;
                babblesRemaining--;
            }
        }

        inputTimer -= Time.deltaTime;

        if(this.inputQueue.Count > 0 && inputTimer < 0)
        {
            if (this.inputQueue.Count > 40)
            {
                UnityEngine.Debug.Log("WARNING: INPUT QUEUE IS NOT EMPTYING FASTER THAN IT IS BEING FILLED, count=" + this.inputQueue.Count);
            }
            this.AddInput(inputQueue.Dequeue());
            inputTimer = INPUT_TIMER_DURATION;
        }
    }


    void Babble()
    {
        int randInt = Random.Range(1, 4);
        string input = "";
        string op = "";
        if (randInt == 1)
        {
            op = @"^moveForward";
        }
        else if (randInt == 2)
        {
            op = @"^lookLeft";
        }
        else if (randInt == 3)
        {
            op = @"^lookRight";
        }

        GetSensorimotor().SetOp(op);

        input = "<(*,{SELF}) --> " + op + ">. :|:";

        if (input != "")
        {
            this.QueueInput(input);
        }
    }

    public void LaunchONA()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(@"cmd.exe");
        startInfo.WorkingDirectory = Application.dataPath + @"\NARS";
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += new DataReceivedEventHandler(ONAOutputReceived);
        process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.StandardInput.WriteLine("NAR shell");
        process.StandardInput.Flush();

        inputStreamWriter = process.StandardInput;
        AddInput("*volume=0");
    }

    public void LaunchNARS()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe");
        startInfo.WorkingDirectory = Application.dataPath + @"\NARS";
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += new DataReceivedEventHandler(NARSOutputReceived);
        process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.StandardInput.WriteLine("java -Xmx1024m -jar opennars.jar");
        process.StandardInput.Flush();

        inputStreamWriter = process.StandardInput;
        AddInput("*volume=0");
    }

    public void AddInferenceCycles(int cycles)
    {
        inputStreamWriter.WriteLine("" + cycles);
    }

    public void QueueInput(string message)
    {
        this.inputQueue.Enqueue(message);
    }

    public void AddInput(string message)
    {
        //UnityEngine.Debug.Log("SENDING INPUT: " + message);

        inputStreamWriter.WriteLine(message);
        inputStreamWriter.Flush();
    }

    void NARSOutputReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        //UnityEngine.Debug.Log(eventArgs.Data);
        if (eventArgs.Data.Contains("EXE:")) //operation executed
        {
            //UnityEngine.Debug.Log("RECEIVED OUTPUT: " + eventArgs.Data);
            int length = eventArgs.Data.IndexOf("(") - eventArgs.Data.IndexOf("^");
            string operation = eventArgs.Data.Substring(eventArgs.Data.IndexOf("^"), length);
            //UnityEngine.Debug.Log("RECEIVED OUTPUT: " + operation);

            GetSensorimotor().SetOp(operation);
        }

/*        if (eventArgs.Data.Contains("EXE:") || eventArgs.Data.Contains("Executed"))
        {
            if (statisticsStreamwriter == null)
            {
                statisticsStreamwriter = File.CreateText("decisionlog.txt");
            }
            statisticsStreamwriter.WriteLine(eventArgs.Data);
        }*/

    }

    void ONAOutputReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        //UnityEngine.Debug.Log(eventArgs.Data);
        if (eventArgs.Data.Contains("executed with args")) //operation executed
        {
            string operation = eventArgs.Data.Split(' ')[0];
            UnityEngine.Debug.Log("RECEIVED OUTPUT: " + operation);

            GetSensorimotor().SetOp(operation);
        }

/*        if (eventArgs.Data.Contains("executed with args") || eventArgs.Data.Contains("decision"))
        {
            if (statisticsStreamwriter == null)
            {
                statisticsStreamwriter = File.CreateText("onadecisionlog.txt");
            }
            statisticsStreamwriter.WriteLine(eventArgs.Data);
        }*/

    }

    void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        UnityEngine.Debug.LogError(eventArgs.Data);
    }

    void OnApplicationQuit()
    {
        if (process != null || !process.HasExited )
        {
            process.CloseMainWindow();
        }
        statisticsStreamwriter.Close();
    }

}



