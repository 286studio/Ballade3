// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until the specified wait condition.
    /// </summary>
    public class Wait : Command
    {
        /// <summary>
        /// Literal used to indicate "wait-for-input" mode.
        /// </summary>
        public const string InputLiteral = "i";

        /// <summary>
        /// Wait conditions:<br/>
        ///  - `i` user press continue or skip input key;<br/>
        ///  - `0.0` timer (seconds);<br/>
        ///  - `i0.0` timer, that is skip-able by continue or skip input keys.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter WaitMode;
        /// <summary>
        /// Script commands to execute when the wait is over.
        /// Escape commas inside list values to prevent them being treated as delimiters.
        /// </summary>
        [ParameterAlias("do")]
        public StringListParameter OnFinished;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            // Don't just return here if skip is enabled; state snapshot is marked as allowed for player rollback when setting waiting for input.

            // Always wait for at least a frame; otherwise skip-able timer (eg, @wait i3) may not behave correctly
            // when used before/after a generic text line: https://forum.naninovel.com/viewtopic.php?p=156#p156
            await AsyncUtils.WaitEndOfFrame;

            if (!Assigned(WaitMode))
            {
                LogWarningWithPosition($"`{nameof(WaitMode)}` parameter is not specified, the wait command will do nothing.");
                return;
            }

            var waitMode = WaitMode.Value;
            if (waitMode.EqualsFastIgnoreCase(InputLiteral))
                await WaitForInputAsync(cancellationToken);
            else if (waitMode.StartsWithFast(InputLiteral) && ParseUtils.TryInvariantFloat(waitMode.GetAfterFirst(InputLiteral), out var waitTime))
                await WaitForTimerAsync(waitTime, cancellationToken);
            else if (ParseUtils.TryInvariantFloat(waitMode, out waitTime))
                await WaitForTimerAsync(waitTime, cancellationToken.ASAPToken);
            else LogWarningWithPosition($"Failed to resolve value of the `{nameof(WaitMode)}` parameter for the wait command. Check the API reference for list of supported values.");

            if (cancellationToken.CancelASAP) return;
            
            if (Assigned(OnFinished))
                await ExecuteOnFinishedAsync(OnFinished, cancellationToken);
        }

        private static async UniTask WaitForInputAsync (CancellationToken cancellationToken)
        {
            var player = Engine.GetService<IScriptPlayer>();
            player.SetWaitingForInputEnabled(true);
            while (Application.isPlaying && !cancellationToken.CancellationRequested)
            {
                await AsyncUtils.WaitEndOfFrame;
                if (!player.WaitingForInput || player.AutoPlayActive) break;
            }
        }

        private static async UniTask WaitForTimerAsync (float waitTime, CancellationToken cancellationToken)
        {
            var player = Engine.GetService<IScriptPlayer>();
            if (player.SkipActive) return;

            var startTime = Time.time;
            while (Application.isPlaying)
            {
                await AsyncUtils.WaitEndOfFrame;
                var waitedEnough = Time.time - startTime >= waitTime;
                if (cancellationToken.CancellationRequested || waitedEnough) break;
            }
        }

        private static async UniTask ExecuteOnFinishedAsync (string[] scriptLines, CancellationToken cancellationToken)
        {
            var scriptText = string.Join(Environment.NewLine, scriptLines);
            var script = Script.FromScriptText("On wait finished script", scriptText);
            var playlist = new ScriptPlaylist(script);
            await playlist.ExecuteAsync(cancellationToken);
        }
    } 
}
