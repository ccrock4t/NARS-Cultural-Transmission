using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dome : MonoBehaviour
{
    [SerializeField]
    public DomeColor color;

    public enum DomeColor
    {
        Red, Orange, Yellow, Green, Blue, Purple
    }

}
