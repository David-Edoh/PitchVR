using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Feeds provider-neutral controller buttons into XR Interaction Toolkit.</summary>
[DefaultExecutionOrder(-90)]
[RequireComponent(typeof(XRBaseInputInteractor))]
public sealed class XRIInteractorInput : MonoBehaviour
{
    public XRControllerInput.Hand hand;
    public XRControllerInput.Button selectButton;
    public XRControllerInput.Button activateButton;

    private XRBaseInputInteractor interactor;

    private void Awake()
    {
        interactor = GetComponent<XRBaseInputInteractor>();
        interactor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
        interactor.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
    }

    private void Update()
    {
        SetButton(interactor.selectInput, selectButton);
        SetButton(interactor.activateInput, activateButton);
    }

    private void SetButton(XRInputButtonReader reader, XRControllerInput.Button button)
    {
        bool pressed = XRControllerInput.Get(hand, button);
        reader.manualPerformed = pressed;
        reader.manualValue = pressed ? 1f : 0f;
        if (XRControllerInput.GetDown(hand, button)) reader.manualFramePerformed = Time.frameCount;
        if (XRControllerInput.GetUp(hand, button)) reader.manualFrameCompleted = Time.frameCount;
    }
}
