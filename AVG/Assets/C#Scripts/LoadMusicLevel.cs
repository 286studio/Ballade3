using UnityEngine;
using Naninovel;
using Cysharp.Threading.Tasks;

[CommandAlias("LoadMusicLevel")]
public class LoadMusicLevel : Command
{
    public StringParameter SongName;
    public StringParameter ScriptName_IfClearLevel;
    public StringParameter Label_IfClearLevel;
    public StringParameter ScriptName_IfFailLevel;
    public StringParameter Label_IfFailLevel;
    public IntegerParameter ScorePercentageConsideredFail;
    public override async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        MusicGameEngine.scriptName_ClearLevel = Assigned(ScriptName_IfClearLevel) ? ScriptName_IfClearLevel : null;
        MusicGameEngine.label_ClearLevel = Assigned(Label_IfClearLevel) ? Label_IfClearLevel : null;
        MusicGameEngine.scriptName_FailLevel = Assigned(ScriptName_IfFailLevel) ? ScriptName_IfFailLevel : null;
        MusicGameEngine.label_FailLevel = Assigned(Label_IfFailLevel) ? Label_IfFailLevel : null;
        MusicGameEngine.scorePercentageConsideredFail = ScorePercentageConsideredFail.Value;
        MusicGameEngine.LoadLevel(SongName);

        // https://naninovel.com/guide/integration-options.html#switching-modes
        // 1. Disable Naninovel input.
        var inputManager = Engine.GetService<IInputManager>();
        inputManager.ProcessInput = false;

        // 2. Stop script player.
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. Reset state.
        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();

        // 4. Switch cameras.
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }
}
