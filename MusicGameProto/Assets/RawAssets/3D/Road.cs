using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Road : MonoBehaviour
{
    public static float radius = 2.6f;
    public static float leftLane = -0.525f;
    public static float laneLength = 0.21f;
    public RawImage display;
    [SerializeField] GameObject car;
    [SerializeField] GameObject roll;
    public Transform rollAxis;
    [SerializeField] float oneRoundTime = 60;
    [Range(1, 360)]
    [SerializeField] int degree;
    [SerializeField] Button enableDisplay;
    [SerializeField] GameObject coinPrefab;

    public static Road ins;
    float angle = 2 * Mathf.PI;
    float time;
    public static float coinStartAngleDeg = -20;
    public static float coinEndAngleDeg = 29;


    private void OnValidate2()
    {
        angle = degree * Mathf.Deg2Rad;
        car.transform.localPosition = new Vector3(radius * Mathf.Sin(angle), radius * Mathf.Cos(angle), leftLane);
        car.transform.localRotation = Quaternion.Euler(degree, -270, 0);
    }
    private void Awake()
    {
        ins = this;
        enableDisplay.onClick.AddListener(EnableDisplay);
    }

    public void EnableDisplay()
    {
        bool a = display.IsActive();
        display.gameObject.SetActive(!a);
        enableDisplay.GetComponentInChildren<Text>().text = a ? "3D" : "2D";
    }
    public void EnableDisplay(bool enabled)
    {
        bool a = display.IsActive();
        if (enabled && !a) EnableDisplay();
        if (!enabled && a) EnableDisplay();
    }

    private void Start()
    {
        RhythmGameManager.ins.OnBinguiXFracUpdate += OnCarPositionUpdate;
        Timeline.ins.OnBlockCreated += OnBlockCreated;
    }
    private void Update()
    {
        if (time >= oneRoundTime)
        {
            time = 0;
        }
        else
        {
            time += Time.deltaTime;
        }
        float frac = time / oneRoundTime;
        roll.transform.localRotation = Quaternion.Euler(0, 0, -frac * 360);
        enableDisplay.interactable = GeneralSettings.mode == 1;
        if (GeneralSettings.mode != 1) display.gameObject.SetActive(false);
    }
    void OnCarPositionUpdate(float f)
    {
        var pos = car.transform.localPosition;
        pos.z = leftLane - 0.5f * laneLength + laneLength * 6f * f;
        car.transform.localPosition = pos;
    }
    void OnBlockCreated(RhythmObject o)
    {
        FallingBlock_Tuogui fb = o as FallingBlock_Tuogui;
        if (!fb) return;
        var coin = Instantiate(coinPrefab, rollAxis).GetComponent<Coin>();
        coin.o = fb;
        float rad = coinStartAngleDeg * Mathf.Deg2Rad;
        coin.transform.localPosition = new Vector3(radius * Mathf.Sin(rad), radius * Mathf.Cos(rad), leftLane);
        coin.transform.localRotation = Quaternion.Euler(degree, -270, 0);
    }
}
