using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCharacterControllerSelf : SampleCharacterControllerBase
{
    protected Vector3 previousPosition;
    protected Vector3 previousRotation;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (previousPosition == null || previousRotation == null)
        {
            previousPosition = gameObject.transform.position;
            previousRotation = gameObject.transform.rotation.eulerAngles;
            return;
        }
        if (previousPosition == gameObject.transform.position && previousRotation == gameObject.transform.rotation.eulerAngles)
        {
            return;
        }
        CharacterMovement movement = new CharacterMovement();
        movement.deltaPosition = gameObject.transform.position - previousPosition;
        movement.deltaRotation = gameObject.transform.rotation.eulerAngles - previousRotation;
        Debug.Log($"Send move: deltaPosition={movement.deltaPosition}, deltaRotation={movement.deltaRotation}");
        orchestrator.Send<CharacterMovement>(CharacterMovementCommand, movement);
        previousPosition = gameObject.transform.position;
        previousRotation = gameObject.transform.rotation.eulerAngles;
    }
}
