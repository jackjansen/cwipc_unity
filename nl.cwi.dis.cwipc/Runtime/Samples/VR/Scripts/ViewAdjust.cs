using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class ViewAdjust : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Turn data from the left hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_LeftHandSnapTurnAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Turn data sent from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty leftHandSnapTurnAction
    {
        get => m_LeftHandSnapTurnAction;
        set => SetInputActionProperty(ref m_LeftHandSnapTurnAction, value);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
