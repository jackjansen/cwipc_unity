using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class ViewAdjust : LocomotionProvider
{
    [Tooltip("The object of which the height is adjusted")]
    [SerializeField] GameObject cameraOffset;

    [Tooltip("Multiplication factor for height adjustment")]
    [SerializeField] float heightFactor = 1;

    [Tooltip("The Input System Action that will be used to read view height. Must be a Value Vector2 Control of which y is used.")]
    [SerializeField] InputActionProperty m_ViewHeightAction;
    public InputActionProperty viewHeightAction
    {
        get => m_ViewHeightAction;
        set => SetInputActionProperty(ref m_ViewHeightAction, value);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = ReadInput();
        float deltaHeight = input.y * heightFactor;
        if (deltaHeight != 0 && BeginLocomotion())
        {
            cameraOffset.transform.position += new Vector3(0, deltaHeight, 0);
            EndLocomotion();
        }
    }

    protected void OnEnable()
    {
        m_ViewHeightAction.EnableDirectAction();
    }

    protected void OnDisable()
    {
        m_ViewHeightAction.DisableDirectAction();
    }

    protected Vector2 ReadInput()
    {
        return m_ViewHeightAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
     }

    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
    {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }
}
