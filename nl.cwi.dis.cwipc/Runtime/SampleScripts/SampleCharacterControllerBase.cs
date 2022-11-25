using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCharacterControllerBase : MonoBehaviour
{
    [Tooltip("Orchestration object.")]
    [SerializeField] protected SampleOrchestration orchestrator;

    /// <summary>
    /// Describes movement of a character, relative to its own coordinate system.
    /// Movement is applied first (so using the old rotation), then rotation.
    /// </summary>
    [Serializable]
    protected class CharacterMovement
    {
        public Vector3 deltaPosition;
        public Vector3 deltaRotation;
    }

    protected const string CharacterMovementCommand = "Move";

    // Start is called before the first frame update
    virtual protected void Start()
    {
           // Initialize orchestrator?
    }

}
