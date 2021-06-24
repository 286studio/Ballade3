using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] GameObject[] frontWheels;
    [SerializeField] GameObject body;
    [SerializeField] Transform paiqikou;
    [HideInInspector] public int dir = 0; // 1=left, -1=right, 0=middle
    int bodyStep = 0;
    int wheelStep = 0;
    float lastLocalX;
    private void Start()
    {
        InvokeRepeating("Pppp", 0.2f, 0.2f);
    }
    void Pppp()
    {
        var p = Instantiate(Resources.Load<GameObject>("PUFF"), Road.ins.rollAxis);
        p.transform.position = paiqikou.position;
    }

    private void Update()
    {
        var diff = transform.localPosition.z - lastLocalX;
        if (diff > 0) dir = -1;
        else if (diff < 0) dir = 1;
        else dir = 0;
        if (dir == 1)
        {
            bodyStep = Mathf.Max(bodyStep - 10, -30);
            wheelStep = Mathf.Min(wheelStep + 10, 30);
        }
        else if (dir == -1)
        {
            bodyStep = Mathf.Min(bodyStep + 10, 30);
            wheelStep = Mathf.Max(wheelStep - 10, -30);
        }
        else
        {
            if (bodyStep < 0) ++bodyStep; else if (bodyStep > 0) --bodyStep;
            if (wheelStep < 0) ++wheelStep; else if (wheelStep > 0) --wheelStep;
        }
        body.transform.localRotation = Quaternion.Euler(0, bodyStep, 0);
        frontWheels[0].transform.localRotation = frontWheels[1].transform.localRotation = Quaternion.Euler(0, wheelStep, 0);

        lastLocalX = transform.localPosition.z;
    }
}
