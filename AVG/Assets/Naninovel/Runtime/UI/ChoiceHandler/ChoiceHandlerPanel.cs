// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a view for choosing between a set of choices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChoiceHandlerPanel : CustomUI, IManagedUI
    {
        [Serializable]
        public new class GameState
        {
            public bool RemoveAllButtonsPending;
            // Saving buttons separately from handler actor choices, as they're destroyed dependently.
            public List<ChoiceState> Buttons = new List<ChoiceState>();
        }

        /// <summary>
        /// Invoked when one of active choices are chosen.
        /// </summary>
        public event Action<ChoiceState> OnChoice;

        protected virtual RectTransform ButtonsContainer => buttonsContainer;
        protected virtual ChoiceHandlerButton DefaultButtonPrefab => defaultButtonPrefab;
        protected virtual bool FocusChoiceButtons => focusChoiceButtons;

        [Tooltip("Container that will hold spawned choice buttons.")]
        [SerializeField] private RectTransform buttonsContainer = null;
        [Tooltip("Button prototype to use by default.")]
        [SerializeField] private ChoiceHandlerButton defaultButtonPrefab = null;
        [Tooltip("Whether to focus added choice buttons for keyboard and gamepad control.")]
        [SerializeField] private bool focusChoiceButtons = true;

        private readonly List<ChoiceHandlerButton> choiceButtons = new List<ChoiceHandlerButton>();
        private IBacklogUI backlogUI;
        private bool removeAllButtonsPending;

        UniTask IManagedUI.ChangeVisibilityAsync (bool visible, float? duration, CancellationToken cancellationToken)
        {
            Debug.LogError("@showUI and @hideUI commands can't be used with choice handlers; use @show/hide commands instead");
            return UniTask.CompletedTask;
        }

        public virtual void AddChoiceButton (ChoiceState choice)
        {
            if (removeAllButtonsPending)
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }

            if (choiceButtons.Any(b => b.ChoiceState.Id == choice.Id)) return; // Could happen on rollback.

            var choicePrefab = string.IsNullOrWhiteSpace(choice.ButtonPath) ? defaultButtonPrefab : Resources.Load<ChoiceHandlerButton>(choice.ButtonPath);
            if (!choicePrefab) throw new Exception($"Failed to add `{choice.ButtonPath}` choice button. Make sure the button prefab is stored in a `Resources` folder of the project.");
            var choiceButton = Instantiate(choicePrefab, buttonsContainer, false);
            choiceButton.Initialize(choice);
            choiceButton.OnButtonClicked += () => OnChoice?.Invoke(choice);

            if (backlogUI != null)
            {
                choiceButton.OnButtonClicked += () => backlogUI.AddChoice(choiceButtons
                    .Select(b => new Tuple<string, bool>(b.ChoiceState.Summary, b.ChoiceState.Id == choice.Id)).ToList());
            }

            if (choice.OverwriteButtonPosition)
                choiceButton.transform.localPosition = choice.ButtonPosition;

            choiceButtons.Add(choiceButton);

            if (FocusChoiceButtons)
            {
                switch (FocusModeType)
                {
                    case FocusMode.Visibility:
                        if (EventSystem.current)
                            EventSystem.current.SetSelectedGameObject(choiceButton.gameObject);
                        break;
                    case FocusMode.Navigation:
                        FocusOnNavigation = choiceButton.gameObject;
                        break;
                }
            }
        }

        public virtual void RemoveChoiceButton (string id)
        {
            var buttons = choiceButtons.FindAll(c => c.ChoiceState.Id == id);
            if (buttons.Count == 0) return;

            foreach (var button in buttons)
            {
                if (button) Destroy(button.gameObject);
                choiceButtons.Remove(button);
            }
        }

        /// <summary>
        /// Will remove the buttons before the next <see cref="AddChoiceButton(ChoiceState)"/> call.
        /// </summary>
        public virtual void RemoveAllChoiceButtonsDelayed ()
        {
            removeAllButtonsPending = true;
        }

        public virtual void RemoveAllChoiceButtons ()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
                Destroy(choiceButtons[i].gameObject);
            choiceButtons.Clear();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(defaultButtonPrefab, buttonsContainer);

            backlogUI = Engine.GetService<IUIManager>().GetUI<IBacklogUI>();
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                RemoveAllButtonsPending = removeAllButtonsPending,
                Buttons = choiceButtons.Select(b => b.ChoiceState).ToList()
            };
            stateMap.SetState(state, name);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>(name);
            if (state is null) return;

            var existingButtonIds = choiceButtons.Select(b => b.ChoiceState.Id).ToList();
            foreach (var buttonId in existingButtonIds)
                if (state.Buttons.All(s => s.Id != buttonId))
                    RemoveChoiceButton(buttonId);

            foreach (var buttonState in state.Buttons)
                if (choiceButtons.All(b => b.ChoiceState != buttonState))
                    AddChoiceButton(buttonState);

            removeAllButtonsPending = state.RemoveAllButtonsPending;
        }
    }
}
