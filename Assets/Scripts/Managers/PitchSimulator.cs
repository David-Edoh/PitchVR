using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Adds a pitch rehearsal loop to the existing audience scene. The two hand-held
/// props, slide deck, timing feedback and recap are generated at runtime so the
/// scene stays usable in both the editor and on a Quest headset.
/// </summary>
public sealed class PitchSimulator : MonoBehaviour
{
    [SerializeField] private SessionManager session;
    [SerializeField] private LevelManager audience;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private AudioClip applause;

    private readonly string[] slideTitles = { "YOUR IDEA", "THE PROBLEM", "YOUR SOLUTION", "THE ASK" };
    private readonly string[] slidePrompts = {
        "Introduce yourself and give a one-sentence hook.",
        "Who needs this, and what frustrates them today?",
        "Show how your idea works and why it is better.",
        "Make a clear request and close with confidence."
    };

    private GameObject board;
    private GameObject microphone;
    private GameObject remote;
    private GameObject pdfSurface;
    private Renderer pdfRenderer;
    private Texture2D currentPage;
    private readonly PitchPdfSource pdf = new PitchPdfSource();
    private AudioSource leftSpeaker;
    private AudioSource rightSpeaker;
    private AudioClip liveMicrophone;
    private bool microphonePending;
    private bool microphonePermissionRequested;
    private bool speakersPlaying;
    private Rigidbody microphoneBody;
    private Rigidbody remoteBody;
    private XRGrabInteractable microphoneGrab;
    private XRGrabInteractable remoteGrab;
    private Vector3 microphoneHome;
    private Vector3 remoteHome;
    private TMP_Text boardText;
    private TMP_Text hintText;
    private Transform head;
    private bool holdingMicrophone;
    private bool holdingRemote;
    private bool previousActive;
    private bool previousPaused;
    private int slideIndex;
    private int slidesVisited;
    private float slideStartedAt;
    private float shortestSlide;
    private float longestSlide;
    private float interactionCooldown;
    private float pausedSlideElapsed;
    private string coachingNote;

    public bool IsRemoteHeld => holdingRemote;
    private int SlideCount => pdf.PageCount > 0 ? pdf.PageCount : slideTitles.Length;

    private void Awake()
    {
        if (session == null) session = GetComponent<SessionManager>();
        if (audience == null) audience = GetComponent<LevelManager>();
        Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (camera != null) head = camera.transform;
        if (rightHand == null)
        {
            VRUIPointer pointer = FindObjectOfType<VRUIPointer>();
            if (pointer != null) rightHand = pointer.controllerTransform;
        }
        if (leftHand == null)
        {
            foreach (Transform candidate in FindObjectsOfType<Transform>(true))
            {
                if (candidate.name == "LeftHand Controller") { leftHand = candidate; break; }
            }
        }
    }

    private IEnumerator Start()
    {
        // XR pose arrives after ordinary Start. Wait for tracking before
        // anchoring the screen and props in world space.
        yield return null;
        yield return new WaitForSeconds(0.25f);
        if (head == null)
        {
            Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (camera != null) head = camera.transform;
        }
        if (head == null) { Debug.LogWarning("PitchSimulator: no VR camera found."); yield break; }

        // Anchor the board once in the room; it never moves with the user's head.
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 origin = head.position;
        // An editor XR simulator may leave the camera at the rig origin
        // until tracking begins. Keep the screen at standing eye height.
        origin.y = Mathf.Max(origin.y, head.root.position.y + 1.45f);
        Quaternion facingUser = Quaternion.LookRotation(-forward, Vector3.up);

        board = MakeCube("Pitch presentation", origin + forward * 2.7f - right * 0.95f + Vector3.up * 0.05f,
            facingUser, new Vector3(1.45f, 0.9f, 0.06f), new Color(0.025f, 0.075f, 0.13f));
        Destroy(board.GetComponent<Collider>());
        boardText = MakeText("Slide text", board.transform, new Vector3(0, 0.05f, 0.52f),
            Quaternion.identity, new Vector2(0.86f, 0.72f), 0.095f);
        hintText = MakeText("Rehearsal hint", board.transform, new Vector3(0, -0.55f, 0.52f),
            Quaternion.identity, new Vector2(0.94f, 0.2f), 0.042f);
        pdfSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pdfSurface.name = "PDF slide surface";
        pdfSurface.transform.SetParent(board.transform, false);
        pdfSurface.transform.localPosition = new Vector3(0, 0, 0.54f);
        pdfSurface.transform.localScale = new Vector3(0.87f, 0.78f, 1f);
        Destroy(pdfSurface.GetComponent<Collider>());
        pdfRenderer = pdfSurface.GetComponent<Renderer>();
        pdfRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        pdfSurface.SetActive(false);

        microphone = MakeCube("Practice microphone", origin + forward * 0.48f - right * 0.25f - Vector3.up * 0.43f,
            Quaternion.identity, new Vector3(0.045f, 0.2f, 0.045f), new Color(0.08f, 0.1f, 0.13f));
        remote = MakeCube("Presentation remote", origin + forward * 0.48f + right * 0.25f - Vector3.up * 0.43f,
            Quaternion.identity, new Vector3(0.045f, 0.11f, 0.08f), new Color(0.14f, 0.38f, 0.52f));
        microphone.layer = 2; // Ignore Raycast: props must not block teleport aiming.
        remote.layer = 2;
        microphoneBody = microphone.AddComponent<Rigidbody>();
        remoteBody = remote.AddComponent<Rigidbody>();
        microphoneBody.isKinematic = true;
        remoteBody.isKinematic = true;
        microphoneBody.mass = 0.25f;
        remoteBody.mass = 0.15f;
        microphoneGrab = microphone.AddComponent<XRGrabInteractable>();
        remoteGrab = remote.AddComponent<XRGrabInteractable>();
        microphoneGrab.selectEntered.AddListener(_ => OnMicrophoneGrabbed());
        microphoneGrab.selectExited.AddListener(_ => OnMicrophoneReleased());
        remoteGrab.selectEntered.AddListener(_ => OnRemoteGrabbed());
        remoteGrab.selectExited.AddListener(_ => OnRemoteReleased());
        microphoneGrab.enabled = false;
        remoteGrab.enabled = false;
        microphoneHome = microphone.transform.position;
        remoteHome = remote.transform.position;
        leftSpeaker = MakeSpeaker("Left hall speaker", board.transform.position - right * 1.05f);
        rightSpeaker = MakeSpeaker("Right hall speaker", board.transform.position + right * 1.05f);
        PitchTeleport teleport = gameObject.AddComponent<PitchTeleport>();
        teleport.Configure(leftHand, head);
        ShowWelcome();
        CreatePdfButton();
    }

    private void Update()
    {
        if (session == null || board == null) return;
        bool active = session.IsSessionActive;
        bool paused = session.IsSessionPaused;
        if (!active && (Input.GetKeyDown(KeyCode.U) || XRControllerInput.GetDown(XRControllerInput.Hand.Left, XRControllerInput.Button.Primary))) LoadPdf();
        if (active && !previousActive) BeginPitch();
        if (!active && previousActive) EndPitch();
        if (active && paused != previousPaused)
        {
            if (paused) pausedSlideElapsed = Time.time - slideStartedAt;
            else slideStartedAt = Time.time - pausedSlideElapsed;
        }
        previousActive = active;
        previousPaused = paused;
        if (!active || paused) return;

        UpdateMicrophone();

        interactionCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMicrophone();
        if (Input.GetKeyDown(KeyCode.R))
            ToggleRemote();
        if (holdingRemote && interactionCooldown <= 0f &&
            (Input.GetKeyDown(KeyCode.N) || XRControllerInput.GetDown(XRControllerInput.Hand.Right, XRControllerInput.Button.Trigger)))
            NextSlide();
        if (holdingRemote && Input.GetKeyDown(KeyCode.Comma)) PreviousSlide();
        if (holdingMicrophone && !microphoneGrab.isSelected && leftHand != null) Follow(microphone, leftHand);
        if (holdingRemote && !remoteGrab.isSelected && rightHand != null) Follow(remote, rightHand);
    }

    private void BeginPitch()
    {
        slideIndex = 0;
        slidesVisited = 1;
        shortestSlide = float.MaxValue;
        longestSlide = 0f;
        coachingNote = string.Empty;
        slideStartedAt = Time.time;
        pausedSlideElapsed = 0f;
        holdingMicrophone = false;
        holdingRemote = false;
        microphoneGrab.enabled = true;
        remoteGrab.enabled = true;
        ResetProp(microphone, microphoneBody, microphoneHome);
        ResetProp(remote, remoteBody, remoteHome);
        ShowSlide();
    }

    private void EndPitch()
    {
        RecordSlideTime();
        StopMicrophone();
        holdingMicrophone = false;
        holdingRemote = false;
        microphoneGrab.enabled = false;
        remoteGrab.enabled = false;
        ResetProp(microphone, microphoneBody, microphoneHome);
        ResetProp(remote, remoteBody, remoteHome);
        pdfSurface.SetActive(false);
        boardText.gameObject.SetActive(true);
        boardText.text = "PITCH RECAP\n\n" + slidesVisited + "/" + SlideCount + " sections visited\n" +
            "Time: " + session.GetFormattedElapsedTime() + "\n" +
            "Shortest section: " + Mathf.RoundToInt(shortestSlide == float.MaxValue ? 0f : shortestSlide) + "s\n" +
            (slidesVisited < SlideCount ? "Try covering every section next time." :
             shortestSlide < 10f ? "Try spending more time on each point." :
             longestSlide > 90f ? "Try a tighter transition between points." :
             "Steady pacing. Practice your closing again.");
        hintText.text = "Choose an audience size to rehearse again.";
        if (slidesVisited == SlideCount && applause != null) AudioSource.PlayClipAtPoint(applause, board.transform.position, 0.5f);
    }

    private void ShowWelcome()
    {
        boardText.text = "VR PITCH\n\nChoose an audience size and begin.\n\nRehearse a four-part pitch at your own pace.";
        hintText.text = "X: load PDF   Left thumbstick: teleport   Grip: props";
    }

    private void ShowSlide()
    {
        if (pdf.PageCount > 0)
        {
            try
            {
                if (currentPage != null) Destroy(currentPage);
                currentPage = pdf.RenderPage(slideIndex);
                pdfRenderer.material.mainTexture = currentPage;
                float aspect = (float)currentPage.width / currentPage.height;
                pdfSurface.transform.localScale = new Vector3(0.78f * aspect *
                    board.transform.localScale.y / board.transform.localScale.x, 0.78f, 1f);
                pdfSurface.SetActive(true);
                boardText.gameObject.SetActive(false);
            }
            catch (System.Exception error)
            {
                Debug.LogError("PitchSimulator: PDF render failed: " + error.Message);
                pdfSurface.SetActive(false);
                boardText.gameObject.SetActive(true);
                boardText.text = "Could not display this PDF page.";
            }
        }
        else
        {
            pdfSurface.SetActive(false);
            boardText.gameObject.SetActive(true);
            boardText.text = "0" + (slideIndex + 1) + " / 04\n\n" + slideTitles[slideIndex] + "\n\n" + slidePrompts[slideIndex];
        }
        hintText.text = string.IsNullOrEmpty(coachingNote) ?
            "Grip remote + trigger: next slide   B: end pitch" : coachingNote;
    }

    private void NextSlide()
    {
        if (slideIndex >= SlideCount - 1)
        {
            hintText.text = "Closing reached. Press B to view your recap.";
            Pulse(XRControllerInput.Hand.Right);
            interactionCooldown = 0.25f;
            return;
        }
        float duration = Time.time - slideStartedAt;
        RecordSlideTime();
        coachingNote = duration < 10f ? "Take your time on the next point." :
            duration > 90f ? "Keep the next point concise." : "Good pace. Keep going.";
        slideIndex++;
        slidesVisited = Mathf.Max(slidesVisited, slideIndex + 1);
        slideStartedAt = Time.time;
        interactionCooldown = 0.25f;
        ShowSlide();
        Pulse(XRControllerInput.Hand.Right);
    }

    private void PreviousSlide()
    {
        if (slideIndex <= 0) return;
        RecordSlideTime();
        slideIndex--;
        slideStartedAt = Time.time;
        ShowSlide();
    }

    private void RecordSlideTime()
    {
        float duration = Mathf.Max(0f, previousPaused ? pausedSlideElapsed : Time.time - slideStartedAt);
        shortestSlide = Mathf.Min(shortestSlide, duration);
        longestSlide = Mathf.Max(longestSlide, duration);
    }

    private void ToggleMicrophone()
    {
        if (!holdingMicrophone && !Input.GetKeyDown(KeyCode.M) && leftHand != null && Vector3.Distance(leftHand.position, microphone.transform.position) > 0.7f) return;
        holdingMicrophone = !holdingMicrophone;
        microphoneBody.isKinematic = holdingMicrophone;
        microphoneBody.useGravity = !holdingMicrophone;
        if (holdingMicrophone) microphonePending = true;
        else StopMicrophone();
        hintText.text = holdingMicrophone ? "Mic on. Voice plays through the hall speakers." : "Mic dropped. Grip near it to pick it up.";
        Pulse(XRControllerInput.Hand.Left);
    }

    private void OnMicrophoneGrabbed()
    {
        holdingMicrophone = true;
        microphonePending = true;
        hintText.text = "Mic on. Voice plays through the hall speakers.";
        Pulse(XRControllerInput.Hand.Left);
    }

    private void OnMicrophoneReleased()
    {
        holdingMicrophone = false;
        StopMicrophone();
        if (hintText != null) hintText.text = "Mic dropped. Grip near it to pick it up.";
    }

    private void OnRemoteGrabbed()
    {
        holdingRemote = true;
        interactionCooldown = 0.2f;
        hintText.text = "Remote ready. Right trigger changes slides.";
        Pulse(XRControllerInput.Hand.Right);
    }

    private void OnRemoteReleased()
    {
        holdingRemote = false;
        if (hintText != null) hintText.text = "Remote dropped. Grip near it to pick it up.";
    }

    private void ToggleRemote()
    {
        if (!holdingRemote && !Input.GetKeyDown(KeyCode.R) && rightHand != null && Vector3.Distance(rightHand.position, remote.transform.position) > 0.7f) return;
        holdingRemote = !holdingRemote;
        remoteBody.isKinematic = holdingRemote;
        remoteBody.useGravity = !holdingRemote;
        interactionCooldown = 0.2f;
        hintText.text = holdingRemote ? "Remote ready. Right trigger changes slides." : "Remote dropped. Grip near it to pick it up.";
        Pulse(XRControllerInput.Hand.Right);
    }

    /// <summary>Open the headset document picker, or the editor file picker.</summary>
    public void LoadPdf()
    {
        if (session != null && session.IsSessionActive) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaClass("com.pitchvr.PdfBridge"))
                bridge.CallStatic("openPicker", activity, gameObject.name);
        }
        catch (System.Exception error) { ShowPdfError(error); }
#elif UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Choose a presentation PDF", "", "pdf");
        if (!string.IsNullOrEmpty(path)) OpenPdf(path, false);
#else
        hintText.text = "PDF picker is available in the Unity editor and Android build.";
#endif
    }

    private void CreatePdfButton()
    {
        GameObject template = null;
        foreach (Button candidate in FindObjectsOfType<Button>(true))
        {
            if (candidate.name == "Level4Button") { template = candidate.gameObject; break; }
        }
        if (template == null) return;
        RectTransform templateRect = template.GetComponent<RectTransform>();
        GameObject obj = new GameObject("LoadPdfButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(templateRect.parent, false);
        rect.anchorMin = templateRect.anchorMin;
        rect.anchorMax = templateRect.anchorMax;
        rect.sizeDelta = new Vector2(templateRect.sizeDelta.x, 35f);
        rect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, -125f);
        Image image = obj.GetComponent<Image>();
        Image templateImage = template.GetComponent<Image>();
        image.sprite = templateImage.sprite;
        image.type = templateImage.type;
        image.color = new Color(0.8f, 0.91f, 0.98f, 1f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(LoadPdf);
        TMP_Text sourceLabel = template.GetComponentInChildren<TMP_Text>();
        if (sourceLabel != null)
        {
            TMP_Text label = Instantiate(sourceLabel, rect);
            label.text = "Upload PDF";
            label.raycastTarget = false;
        }
        VRUIPointer pointer = FindObjectOfType<VRUIPointer>();
        if (pointer != null) pointer.RefreshButtonList();
    }

    // Called by the Android document picker after the user chooses a file.
    public void OnPdfChosen(string uri)
    {
        if (!string.IsNullOrEmpty(uri)) OpenPdf(uri, true);
    }

    private void OpenPdf(string location, bool androidUri)
    {
        try
        {
            pdf.Open(location, androidUri);
            slideIndex = 0;
            ShowSlide();
            hintText.text = "PDF loaded: " + pdf.PageCount + " pages. Choose an audience to begin.";
        }
        catch (System.Exception error) { ShowPdfError(error); }
    }

    private void ShowPdfError(System.Exception error)
    {
        Debug.LogError("PitchSimulator: PDF import failed: " + error);
        if (hintText != null) hintText.text = "PDF import failed. Check the Console for details.";
    }

    private AudioSource MakeSpeaker(string name, Vector3 position)
    {
        GameObject speaker = MakeCube(name, position, Quaternion.identity,
            new Vector3(0.18f, 0.34f, 0.16f), new Color(0.09f, 0.1f, 0.12f));
        Destroy(speaker.GetComponent<Collider>());
        AudioSource source = speaker.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;
        source.minDistance = 1.2f;
        source.maxDistance = 8f;
        source.volume = 0.28f;
        return source;
    }

    private void UpdateMicrophone()
    {
        if (!holdingMicrophone) return;
        if (microphonePending)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string recordPermission = "android.permission.RECORD_AUDIO";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(recordPermission))
            {
                if (!microphonePermissionRequested)
                {
                    microphonePermissionRequested = true;
                    UnityEngine.Android.Permission.RequestUserPermission(recordPermission);
                }
                return;
            }
#endif
            liveMicrophone = Microphone.Start(null, true, 1, 22050);
            microphonePending = false;
            if (liveMicrophone == null)
            {
                hintText.text = "Microphone unavailable. Check headset permission.";
                return;
            }
            leftSpeaker.clip = liveMicrophone;
            rightSpeaker.clip = liveMicrophone;
        }
        if (liveMicrophone != null && !speakersPlaying && Microphone.GetPosition(null) > 2205)
        {
            leftSpeaker.Play();
            rightSpeaker.Play();
            speakersPlaying = true;
        }
    }

    private void StopMicrophone()
    {
        microphonePending = false;
        speakersPlaying = false;
        if (leftSpeaker != null) leftSpeaker.Stop();
        if (rightSpeaker != null) rightSpeaker.Stop();
        if (Microphone.IsRecording(null)) Microphone.End(null);
        if (liveMicrophone != null) Destroy(liveMicrophone);
        liveMicrophone = null;
    }

    private static void ResetProp(GameObject prop, Rigidbody body, Vector3 home)
    {
        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        prop.transform.position = home;
        prop.transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        StopMicrophone();
        if (currentPage != null) Destroy(currentPage);
    }

    private static void Follow(GameObject prop, Transform hand)
    {
        prop.transform.position = hand.position + hand.forward * 0.08f;
        prop.transform.rotation = hand.rotation;
    }

    private void Pulse(XRControllerInput.Hand controller)
    {
        XRControllerInput.Pulse(controller, 0.35f, 0.08f);
    }

    private static GameObject MakeCube(string name, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
        result.name = name;
        result.transform.SetPositionAndRotation(position, rotation);
        result.transform.localScale = scale;
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        result.GetComponent<Renderer>().material = material;
        return result;
    }

    private static TMP_Text MakeText(string name, Transform parent, Vector3 localPosition, Quaternion rotation,
        Vector2 size, float fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = rotation;
        TextMeshPro text = obj.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = size;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
}
