using UnityEngine;
using UnityEngine.InputSystem;

public class ControlSwitch : MonoBehaviour {

	public Key toggleKey = Key.C;
	public GameObject manualController;
	public GameObject automaticController;

	private bool useAutomaticControl = false;
	private const string automaticControlDefaultCMDLineArgument = "-automaticControl";

	void Awake()
	{
		Application.targetFrameRate = 60;
		useAutomaticControl = HasCommandLineArgument (automaticControlDefaultCMDLineArgument);
		SetControllerState (useAutomaticControl);
	}

	void Update()
	{
		if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame) {
			Toggle ();
		}
	}

	void Toggle()
	{
		useAutomaticControl = !useAutomaticControl;
		SetControllerState(useAutomaticControl);
	}

	void SetControllerState(bool useAutomaticControl)
	{
		if (manualController == null)
		{
			Debug.LogWarning ($"{nameof(ControlSwitch)} on {name} has no manualController reference; controller switching is disabled.", this);
			return;
		}

		if (useAutomaticControl && automaticController == null)
		{
			Debug.LogWarning ($"{nameof(ControlSwitch)} on {name} has no automaticController reference; using manual control.", this);
			useAutomaticControl = false;
		}

		manualController.SetActive (!useAutomaticControl);
		if (automaticController != null)
			automaticController.SetActive (useAutomaticControl);
	}

	bool HasCommandLineArgument(string argument)
	{
		string[] passedArguments = System.Environment.GetCommandLineArgs ();
		foreach (string passedArgument in passedArguments) {
			if (passedArgument.Equals(argument))
				return true;
		}
		return false;
	}
}
