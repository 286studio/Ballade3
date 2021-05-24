// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.


namespace Naninovel.UI
{
    public class LoadingPanel : CustomUI, ILoadingUI
    {
        private IStateManager stateManager;

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<IStateManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.OnGameLoadStarted += HandleLoadStarted;
            stateManager.OnGameLoadFinished += HandleLoadFinished;
            stateManager.OnResetStarted += Show;
            stateManager.OnResetFinished += Hide;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (stateManager != null)
            {
                stateManager.OnGameLoadStarted -= HandleLoadStarted;
                stateManager.OnGameLoadFinished -= HandleLoadFinished;
                stateManager.OnResetStarted -= Show;
                stateManager.OnResetFinished -= Hide;
            }
        }

        private void HandleLoadStarted (GameSaveLoadArgs args) => Show();
        private void HandleLoadFinished (GameSaveLoadArgs args) => Hide();
    }
}
