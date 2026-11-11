# VR Pitch

VR Pitch is a public speaking rehearsal for Meta Quest. Choose an empty room or an audience of 5, 11, or 16 people, then present a built-in four-part pitch or your own PDF. It builds on the project's existing audience simulation and adds teleportation, a presentation screen, hand-held microphone and remote, slide pacing prompts, and a recap.

The pitch deck has live pacing coaching. XR Interaction Toolkit handles microphone and remote grabs; `PitchSimulator.cs` handles slides and the recap, and `PitchTeleport.cs` previews teleport landings. The audience levels, session timer, menus, and VR pointer come from the earlier classroom project and were adapted for pitch practice.

## Run

1. Open the project in Unity **6000.6.6f1**.
2. Open `Assets/Scenes/MainScene.unity` and press Play, or build for Android using the enabled MainScene build entry. The project uses XR Interaction Toolkit 3.6.1 and the Oculus XR Plug-in 4.5.2; XR Plug-in Management starts the Oculus loader for Android and desktop.
3. On Quest, choose an audience size with the right-hand pointer and trigger. The **Upload PDF** menu button opens Android's document picker; **X** also opens it from the menu. Hold the left thumbstick forward to aim the curved teleport arc and release it to land. Hold the left grip near the microphone and the right grip near the remote to pick them up. Release either grip to drop that prop. Press the right trigger while holding the remote to advance the presentation. Press **B** to end and view the recap.

For editor testing, press **U** to choose a PDF, hold **T** to aim teleport and release it to land, press **1–4** to start a session, **M** for the microphone, **R** for the remote, **N** for the next slide, **,** for the previous slide, **P** to pause, and **Escape** to end. The on-screen audience buttons also work in VR. Editor PDF preview needs Poppler's `pdfinfo` and `pdftoppm` on PATH.

## Scope and project notes

- The pitch board uses a built-in four-part deck or a user-selected PDF of up to 100 pages. On Quest, Android's document picker and `PdfRenderer` render each page; the native plugin source and build script are in `Native/Android/`.
- The microphone monitors the headset microphone through two spatial speakers while held. Android requests microphone permission at first use. Keep headset speaker volume modest to reduce acoustic feedback. Audio is not saved or transmitted. Speech transcription remains future work; the recap evaluates slide coverage and timing, not speech quality.
- The environment retains the existing classroom audience assets. The menu and session language now frame it as a pitch rehearsal room; a dedicated hall art pass remains future work.
- Quest frame rate, comfort, Android document picker, and live microphone routing still require an on-device pass. C# and Java compile checks passed in this workspace, but no headset was connected for runtime validation.

## Credits

The earlier classroom VR project and its manager scripts credit Dulakshini Dharmarathne. This adaptation adds the VR Pitch interaction loop, presentation visuals, pacing feedback, menu text, and this README. Existing third-party assets retain their licenses and attributions in `Assets/`.
