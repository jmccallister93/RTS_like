using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WarbandManager : MonoBehaviour
{
    [Header("UI References")]
    public Button warbandMember1Button;
    public Button warbandMember2Button;
    public Button warbandMember3Button;
    public Button warbandMember4Button;
    public Button warbandMember5Button;
    public Button warbandMember6Button;
    public Button warbandMember7Button;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    public Camera cam;

    private Mouse mouse;
   
    void Start()
    {
        cam = GetComponent<Camera>();
        mouse = Mouse.current;
        warbandMember1Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember1Button));
        warbandMember2Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember2Button));
        warbandMember3Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember3Button));
        warbandMember4Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember4Button));
        warbandMember5Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember5Button));
        warbandMember6Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember6Button));
        warbandMember7Button.onClick.AddListener(() => OnWarbandMemberButtonClicked(warbandMember7Button));
    }

    void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
    }

    private void OnWarbandMemberButtonClicked(Button warbandMember1Button)
    {
     
    }

    private void DisplayWarbandMember()
    {
    }

    bool IsMemeberAlive()
    {
        return false;
    }

    private void MoveCameraToMemeber()
    {

    }

}
