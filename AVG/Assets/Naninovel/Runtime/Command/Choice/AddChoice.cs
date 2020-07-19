// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a [choice](/guide/choices.md) option to a choice handler with the specified ID (or default one).
    /// </summary>
    /// <remarks>
    /// When `goto` parameter is not specified, will continue script execution from the next script line.
    /// </remarks>
    /// <example>
    /// ; Print the text, then immediately show choices and stop script execution.
    /// Continue executing this script or load another?[skipInput]
    /// @choice "Continue" goto:.Continue
    /// @choice "Load another from start" goto:AnotherScript
    /// @choice "Load another from \"MyLabel\"" goto:AnotherScript.MyLabel
    /// @stop
    /// 
    /// ; Following example shows how to make an interactive map via `@choice` commands.
    /// ; For this example, we assume, that inside a `Resources/MapButtons` folder you've 
    /// ; stored prefabs with `ChoiceHandlerButton` component attached to their root objects.
    /// # Map
    /// @back Map
    /// @hidePrinter
    /// @choice handler:ButtonArea button:MapButtons/Home pos:-300,-300 goto:.HomeScene
    /// @choice handler:ButtonArea button:MapButtons/Shop pos:300,200 goto:.ShopScene
    /// @stop
    /// 
    /// # HomeScene
    /// @back Home
    /// Home, sweet home!
    /// @goto.Map
    /// 
    /// # ShopScene
    /// @back Shop
    /// Don't forget about cucumbers!
    /// @goto.Map
    /// 
    /// ; You can also set custom variables based on choices.
    /// @choice "I'm humble, one is enough..." set:score++
    /// @choice "Two, please." set:score=score+2
    /// @choice "I'll take the entire stock!" set:karma--;score=999
    /// </example>
    [CommandAlias("choice")]
    public class AddChoice : Command, Command.ILocalizable, Command.IPreloadable
    {
        /// <summary>
        /// Text to show for the choice.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter ChoiceSummary;
        /// <summary>
        /// Path (relative to a `Resources` folder) to a [button prefab](/guide/choices.md#choice-button) representing the choice. 
        /// The prefab should have a `ChoiceHandlerButton` component attached to the root object.
        /// Will use a default button when not provided.
        /// </summary>
        [ParameterAlias("button")]
        public StringParameter ButtonPath;
        /// <summary>
        /// Local position of the choice button inside the choice handler (if supported by the handler implementation).
        /// </summary>
        [ParameterAlias("pos")]
        public DecimalListParameter ButtonPosition;
        /// <summary>
        /// ID of the choice handler to add choice for. Will use a default handler if not provided.
        /// </summary>
        [ParameterAlias("handler")]
        public StringParameter HandlerId;
        /// <summary>
        /// Path to go when the choice is selected by user;
        /// See [@goto] command for the path format.
        /// </summary>
        [ParameterAlias("goto")]
        public NamedStringParameter GotoPath;
        /// <summary>
        /// Set expression to execute when the choice is selected by user; 
        /// see [@set] command for syntax reference.
        /// </summary>
        [ParameterAlias("set")]
        public StringParameter SetExpression;
        /// <summary>
        /// Whether to also show choice handler the choice is added for;
        /// enabled by default.
        /// </summary>
        [ParameterAlias("show")]
        public BooleanParameter ShowHandler = true;
        /// <summary>
        /// Duration (in seconds) of the fade-in (reveal) animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        protected IChoiceHandlerManager HandlerManager => Engine.GetService<IChoiceHandlerManager>();

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(HandlerId) || HandlerId.DynamicValue) return;

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            var handler = await HandlerManager.GetOrAddActorAsync(handlerId);
            await handler.HoldResourcesAsync(this, null);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(HandlerId) || HandlerId.DynamicValue) return;

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            if (HandlerManager.ActorExists(handlerId)) HandlerManager.GetActor(handlerId).ReleaseResources(this, null);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            var choiceHandler = await HandlerManager.GetOrAddActorAsync(handlerId);
            if (cancellationToken.IsCancellationRequested) return;

            if (!choiceHandler.Visible && ShowHandler)
                choiceHandler.ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken).Forget();

            var buttonPos = Assigned(ButtonPosition) ? (Vector2?)ArrayUtils.ToVector2(ButtonPosition) : null;
            var choice = new ChoiceState(ChoiceSummary, ButtonPath, buttonPos, GotoPath?.Name, GotoPath?.NamedValue, SetExpression);
            choiceHandler.AddChoice(choice);
        }
    }
}
