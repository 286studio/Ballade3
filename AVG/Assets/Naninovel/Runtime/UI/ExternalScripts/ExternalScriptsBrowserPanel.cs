// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.UI
{
    public class ExternalScriptsBrowserPanel : ScriptNavigatorPanel, IExternalScriptsUI
    {
        public override async UniTask LocateScriptsAsync ()
        {
            var scripts = await ScriptManager.LocateExternalScriptsAsync();
            GenerateScriptButtons(scripts);
        }
    }
}
