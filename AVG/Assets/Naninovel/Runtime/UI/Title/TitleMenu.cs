// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TitleMenu : CustomUI, ITitleUI
    {
        private IScriptPlayer scriptPlayer;
        private string titleScriptName;

        protected override void Awake ()
        {
            base.Awake();

            scriptPlayer = Engine.GetService<IScriptPlayer>();
            titleScriptName = Engine.GetConfiguration<ScriptsConfiguration>().TitleScript;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float? duration = null, CancellationToken cancellationToken = default)
        {
            if (visible && !string.IsNullOrEmpty(titleScriptName))
            {
                await scriptPlayer.PreloadAndPlayAsync(titleScriptName);
                if (cancellationToken.CancelASAP) return;
                await UniTask.WaitWhile(() => scriptPlayer.Playing);
                if (cancellationToken.CancelASAP) return;
            }

            await base.ChangeVisibilityAsync(visible, duration, cancellationToken);
        }
    }
}
