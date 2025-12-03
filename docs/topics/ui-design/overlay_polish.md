### Goal
Prevent any UI behind the confirmation overlay from receiving mouse or keyboard input while the overlay is visible.

### Diagnosis
- Current `ConfirmOverlay.Draw(...)` renders a dim backdrop and dialog but doesn’t actively block input.
- Some renderers disable their own buttons when the overlay is open, but text inputs and other interactive components still process events (see “Invite Religion by Name”).
- Because our widgets are custom draw-list based (not true ImGui widgets), `io.WantCaptureMouse/Keyboard` won’t help unless we gate our components.

### Plan
1. Central modal input gate (blocking flag)
   - Add a central, reusable modal gate that any overlay can toggle.
   - Options (pick one — both are simple):
     - A) New static utility `UIInputGate` with `PushModal()`/`PopModal()` and `IsBlocked` (internally keeps a counter for nested overlays).
     - B) Extend existing `OverlayCoordinator` (preferred if already present in session) with `BeginModal()`/`EndModal()` and `IsModalOpen`.
   - Acceptance: When any modal is shown, `IsModalOpen == true` globally.

2. Overlay activates the gate and captures clicks
   - Update `ConfirmOverlay.Draw(...)` to:
     - Call `BeginModal()` at start and `EndModal()` at the end of the same frame (or the caller wraps it — see Step 5).
     - Draw a full-window invisible capture layer above the dim backdrop that consumes clicks:
       - Compute window rect, test `IsMouseDown/Clicked` inside; set a local `consumedAnyClick` flag so lower layers never act this frame.
     - Return a `bool inputCaptured` (new out value) so callers know all background input must be ignored even if they forgot to guard.
   - Acceptance: Clicking anywhere outside the dialog no longer toggles/activates behind-the-overlay controls.

3. Harden interactive components to respect the gate
   - ButtonRenderer: Before hover/click logic, if `IsModalOpen` is true → render (dimmed) but always `enabled = false` semantics; keep cursor default.
   - TextInput component: Add optional `enabled` parameter (default true). If `!enabled` or `IsModalOpen` → draw field visuals only, no focus, ignore key presses and caret.
   - Dropdown component: If `IsModalOpen` → never open/close menu; ignore selection.
   - ScrollableList: If `IsModalOpen` → don’t react to wheel or drag; just render.
   - These are small guarded checks at the top of each component, no layout changes.
   - Acceptance: With gate on, none of the shared components change state.

4. Renderer-level guardrails (belt-and-suspenders)
   - In `CivilizationManageRenderer.Draw(...)`, compute `overlayOpen = state.ShowDisbandConfirm || state.KickConfirmReligionId != null` (already present) and:
     - Call TextInput with `enabled: !overlayOpen`.
     - Pass `enabled: !overlayOpen` for “Send Invite”, “Leave”, “Disband”, and list Kick buttons (already partly done, but align fully).
   - Do the same pattern anywhere else we show modals in future.
   - Acceptance: Even without the global gate, the Manage tab won’t accept input when an overlay is up.

5. Clear and simple overlay API usage
   - Preferred calling pattern:
     ```csharp
     using (OverlayScope.Modal()) // RAII helper that BeginModal() in ctor, EndModal() in Dispose()
     {
         ConfirmOverlay.Draw(..., out var confirmed, out var canceled, out var inputCaptured);
     }
     ```
   - If RAII is too heavy, explicitly call `BeginModal()` before drawing the overlay and `EndModal()` after.
   - Acceptance: Code using overlays is straightforward and robust against early returns.

6. Visual behavior preservation
   - Keep backdrop + dialog visuals exactly as-is.
   - Optional: Make clicking backdrop act like “Cancel” (toggleable via parameter). If enabled, respect that in `ConfirmOverlay.Draw` and set `canceled = true` on backdrop click.

7. Testing checklist
   - Mouse:
     - Click anywhere behind the dialog (buttons, dropdown, list, text input): no state changes, no hover states.
     - Scroll wheel over background lists does not scroll.
   - Keyboard:
     - Typing does not alter any text inputs behind the modal.
     - Enter/Escape only affect the modal (Confirm/Cancel) if we wire those keybindings; otherwise they do nothing.
   - Nested modals (future): Counter-based gate works correctly; only when the last modal closes does input unblock.

8. Rollout steps
   - Implement gate (1 file) and integrate into `ConfirmOverlay`.
   - Update ButtonRenderer, TextInput, Dropdown, ScrollableList to respect `IsModalOpen` (tiny edits each).
   - Apply renderer-level `enabled: !overlayOpen` for any remaining interactive calls in `CivilizationManageRenderer`.
   - Manual verify; no unit tests needed (UI), run solution tests for regressions.

### Estimated effort
- Gate + overlay capture: 0.5 h
- Harden 4 shared components: 0.5–0.75 h
- Renderer callsite adjustments + QA: 0.5 h
- Total: ~1.5–2.0 hours

### Risks and mitigations
- Risk: Forgetting a component to check the gate. Mitigation: centralize in shared components; renderer belt-and-suspenders.
- Risk: Blocking lingers if not cleared. Mitigation: RAII/`OverlayScope` or try/finally guard.

### Acceptance criteria
- With confirmation overlay visible, it’s impossible to type into or click/scroll any background widget. Only the overlay receives input.
- On closing the overlay, all widgets behave normally again.