using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] Transform model;
    [HideInInspector] public RhythmObject o;
    float spinTime = 0.5f;
    float spinCount = 0;
    private void Start()
    {
        o.OnFallingFracUpdated += OnFallingFracUpdated;
        o.OnScored += OnScored;
    }
    private void Update()
    {
        if (spinCount >= spinTime)
        {
            spinCount = 0;
        }
        else
        {
            spinCount += Time.deltaTime;
        }
        model.localRotation = Quaternion.Euler(0, spinCount / spinTime * 360, 0);
        if (!o) Destroy(gameObject);
    }
    void OnFallingFracUpdated(float f)
    {
        if (!o) return;
        float deg = Road.coinStartAngleDeg + (Road.coinEndAngleDeg - Road.coinStartAngleDeg) * f;
        float rad = deg * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(Road.radius * Mathf.Sin(rad), Road.radius * Mathf.Cos(rad), Road.leftLane + Road.laneLength * o.exit);
        transform.localRotation = Quaternion.Euler(deg, -270, 0);
    }
    void OnScored(int s)
    {
        if (s == 2)
        {
            var star = Instantiate(Resources.Load<GameObject>("PickupStar"), Road.ins.rollAxis);
            star.transform.position = transform.position;
        }
    }
}
