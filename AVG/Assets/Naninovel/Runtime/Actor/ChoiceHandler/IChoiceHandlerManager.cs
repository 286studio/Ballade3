// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.


namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IChoiceHandlerActor"/> actors.
    /// </summary>
    public interface IChoiceHandlerManager : IActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>
    {
        
    }
}
