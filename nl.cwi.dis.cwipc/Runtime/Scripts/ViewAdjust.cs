using Cwipc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class ViewAdjust : LocomotionProvider
{

	[Tooltip("The object of which the height is adjusted, and that resetting origin will modify")]
	[SerializeField] GameObject cameraOffset;

	[Tooltip("Toplevel object of this player, usually the XROrigin, for resetting origin")]
	[SerializeField] GameObject player;

	[Tooltip("Point cloud pipeline GameObject")]
	[SerializeField] GameObject pointCloudGO;

	[Tooltip("Camera used for determining zero position and orientation, for resetting origin")]
	[SerializeField] Camera playerCamera;

	[Tooltip("Multiplication factor for height adjustment")]
	[SerializeField] float heightFactor = 1;

	[Tooltip("Reset viewpoint height when resetting position (otherwise height is untouched)")]
	[SerializeField] bool resetHeightWithPosition = false;
	[Tooltip("Callback done after view has been adjusted")]
	public UnityEvent viewAdjusted;

	[Tooltip("The Input System Action that will be used to change view height. Must be a Value Vector2 Control of which y is used.")]
	[SerializeField] InputActionProperty m_ViewHeightAction;

	[Tooltip("Use Reset Origin action. Unset if ResetOrigin() is called from a script.")]
	[SerializeField] bool useResetOriginAction = true;

	[Tooltip("The Input System Action that will be used to reset view origin.")]
	[SerializeField] InputActionProperty m_resetOriginAction;

	[Tooltip("Position indicator, visible while adjusting position")]
	[SerializeField] GameObject positionIndicator;

    [Tooltip("Best forward direction indicator, visible while adjusting position")]
    [SerializeField] GameObject forwardIndicator;
    
	[Tooltip("Forward indicator countdown")]
    [SerializeField] UnityEngine.UI.Text forwardIndicatorCountdown;

    [Tooltip("How many seconds is the position indicator visible?")]
	[SerializeField] float positionIndicatorDuration = 5f;

	float positionIndicatorInvisibleAfter = 0;

	[Tooltip("Debug output")]
	[SerializeField] bool debug = false;

	// Start is called before the first frame update
	void Start()
	{
		optionalHideIndicators();
	}

	private void optionalHideIndicators()
	{
		if (positionIndicator != null && positionIndicator.activeSelf && Time.time > positionIndicatorInvisibleAfter) positionIndicator.SetActive(false);
		if (forwardIndicator != null && forwardIndicator.activeSelf && Time.time > positionIndicatorInvisibleAfter) forwardIndicator.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		optionalHideIndicators();
		Vector2 heightInput = m_ViewHeightAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
		float deltaHeight = heightInput.y * heightFactor;
		if (deltaHeight != 0 && BeginLocomotion())
		{
			ShowPositionIndicator();
			cameraOffset.transform.position += new Vector3(0, deltaHeight, 0);
			// Note: we don't save height changes. But if you reset view position
			// afterwards we do also save height changes.
			EndLocomotion();
		}
		if (useResetOriginAction && m_resetOriginAction != null)
		{
			bool doResetOrigin = m_resetOriginAction.action.ReadValue<float>() >= 0.5;
			if (doResetOrigin)
			{
				ResetOrigin();
			}
		}
	}

	private void ShowPositionIndicator(string stage=null)
	{
		if (positionIndicator != null)
		{
			positionIndicator.SetActive(true);
		}
		if (forwardIndicator != null && stage != null)
		{
			// We need to determine the direction of the point cloud capture Z axis and rotate
			// the forward indicator so that is in that direction.
			// xxxjack this is wrong, currently.
			float angle = forwardIndicator.transform.rotation.y;
			
			forwardIndicator.transform.Rotate(0, angle, 0);
			forwardIndicator.SetActive(true);
			forwardIndicatorCountdown.text = stage;
		}
		positionIndicatorInvisibleAfter = Time.time + positionIndicatorDuration;
	}

	/// <summary>
	/// The user wants the current head position, (X,Z) only, to be the (0, Y, 0), right above the XROrigin.
	/// </summary>
	public void ResetOrigin()
	{
		StartCoroutine(_ResetOrigin());
	}

	private IEnumerator _ResetOrigin() {
        IPointCloudPositionProvider pointCloudPipeline = null;
        if (pointCloudGO != null) pointCloudPipeline = pointCloudGO.GetComponentInChildren<IPointCloudPositionProvider>();
        yield return null;
		if (pointCloudPipeline == null)
		{
			// Show the position indicator and reset the view point immedeately. 
			ShowPositionIndicator();
        }
        else
		{
            // Show the countdown
            ShowPositionIndicator(stage: "3");
            yield return new WaitForSeconds(1);
            ShowPositionIndicator(stage: "2");
            yield return new WaitForSeconds(1);
            ShowPositionIndicator(stage: "1");
            yield return new WaitForSeconds(1);
            ShowPositionIndicator(stage: "Click!");
        }

        if (BeginLocomotion())
		{
			Debug.Log("ViewAdjust: ResetOrigin");
			// Rotation of camera relative to the player
			float cameraToPlayerRotationY = playerCamera.transform.rotation.eulerAngles.y - player.transform.rotation.eulerAngles.y;
			if (debug) Debug.Log($"ViewAdjust: camera rotation={cameraToPlayerRotationY}");
			// Apply the inverse rotation to cameraOffset to make the camera point in the same direction as the player
			cameraOffset.transform.Rotate(0, -cameraToPlayerRotationY, 0);
			if (pointCloudPipeline != null)
			{
				// Now the camera is pointing forward from the users point of view.
				// Rotate the point cloud so it is in the same direction.
				float cameraToPointcloudRotationY = playerCamera.transform.rotation.eulerAngles.y - pointCloudGO.transform.rotation.eulerAngles.y;
                if (debug) Debug.Log($"ViewAdjust: pointcloud rotation={cameraToPointcloudRotationY}");
                pointCloudGO.transform.Rotate(0, cameraToPointcloudRotationY, 0);
			}
			// Next set correct position on the camera
			Vector3 moveXZ = playerCamera.transform.position - player.transform.position;
			if (resetHeightWithPosition)
			{
				moveXZ.y = cameraOffset.transform.position.y;
			}
			else
			{
				moveXZ.y = 0;
			}
			if (debug) Debug.Log($"ResetOrigin: move cameraOffset by {moveXZ} to worldpos={playerCamera.transform.position}");
			cameraOffset.transform.position -= moveXZ;
			// Finally adjust the pointcloud position
			if (pointCloudPipeline != null)
			{
				Vector3 pcOriginLocal = pointCloudPipeline.GetPosition();
				if (debug) Debug.Log($"ViewAdjust: adjust pointcloud to {pcOriginLocal}");
				pointCloudGO.transform.localPosition = -pcOriginLocal;
			   
			}
			viewAdjusted.Invoke();
			EndLocomotion();
		}
	}

	public void HigherView(float deltaHeight=0.02f)
	{
		ShowPositionIndicator();
		if (deltaHeight != 0 && BeginLocomotion())
		{
			ShowPositionIndicator();
			cameraOffset.transform.position += new Vector3(0, deltaHeight, 0);
			if (debug) Debug.Log($"ViewAdjust: new height={cameraOffset.transform.position.y}");
			viewAdjusted.Invoke();
			EndLocomotion();
		}
	}

	public void LowerView()
	{
		HigherView(-0.02f);
	}

	protected void OnEnable()
	{
		m_ViewHeightAction.EnableDirectAction();
		if (useResetOriginAction) m_resetOriginAction.EnableDirectAction();
	}

	protected void OnDisable()
	{
		m_ViewHeightAction.DisableDirectAction();
		if (useResetOriginAction) m_resetOriginAction.DisableDirectAction();
	}
}
