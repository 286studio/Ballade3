// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Assigns result of a [script expression](/guide/script-expressions.md) to a [custom variable](/guide/custom-variables.md).
    /// </summary>
    /// <remarks>
    /// Variable name should be alphanumeric (latin characters only) and can contain underscores, eg: `name`, `Char1Score`, `my_score`;
    /// the names are case-insensitive, eg: `myscore` is equal to `MyScore`. If a variable with the provided name doesn't exist, it will be automatically created.
    /// <br/><br/>
    /// It's possible to define multiple set expressions in one line by separating them with `;`. The expressions will be executed in sequence by the order of declaration.
    /// <br/><br/>
    /// Custom variables are stored in **local scope** by default. This means, that if you assign some variable in the course of gameplay 
    /// and player starts a new game or loads another saved game slot, where that variable wasn't assigned — the value will be lost. 
    /// If you wish to store the variable in **global scope** instead, prepend `G_` or `g_` to its name, eg: `G_FinishedMainRoute` or `g_total_score`.
    /// <br/><br/>
    /// In case variable name starts with `T_` or `t_` it's considered a reference to a value stored in 'Script' [managed text](/guide/managed-text.md) document. 
    /// Such variables can't be assigned and mostly used for referencing localizable text values.
    /// <br/><br/>
    /// You can get and set custom variables in C# scripts via `CustomVariableManager` [engine service](/guide/engine-services.md).
    /// </remarks>
    [CommandAlias("set")]
    public class SetCustomVariable : Command, Command.IForceWait
    {
        /// <summary>
        /// Set expression. 
        /// <br/><br/>
        /// The expression should be in the following format: `VariableName=ExpressionBody`, where `VariableName` is the name of the custom 
        /// variable to assign and `ExpressionBody` is a [script expression](/guide/script-expressions.md), the result of which should be assigned to the variable.
        /// <br/><br/>
        /// It's also possible to use increment and decrement unary operators, eg: `@set foo++`, `@set foo--`.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, IDEConstant(IDEConstantAttribute.Expression)]
        public StringParameter Expression;

        protected ICustomVariableManager VariableManager => Engine.GetService<ICustomVariableManager>();
        protected IStateManager StateManager => Engine.GetService<IStateManager>();

        private const string assignmentLiteral = "=";
        private const string incrementLiteral = "++";
        private const string decrementLiteral = "--";
        private const string separatorLiteral = ";";

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var saveStatePending = false;
            var expressions = Expression.Value.Split(separatorLiteral[0]);
            for (int i = 0; i < expressions.Length; i++)
            {
                var expression = expressions[i];
                if (string.IsNullOrEmpty(expression)) continue;

                if (expression.EndsWithFast(incrementLiteral))
                    expression = expression.Replace(incrementLiteral, $"={expression.GetBefore(incrementLiteral)}+1");
                else if (expression.EndsWithFast(decrementLiteral))
                    expression = expression.Replace(decrementLiteral, $"={expression.GetBefore(decrementLiteral)}-1");

                var variableName = expression.GetBefore(assignmentLiteral)?.TrimFull();
                var expressionBody = expression.GetAfterFirst(assignmentLiteral)?.TrimFull();
                if (string.IsNullOrWhiteSpace(variableName) || string.IsNullOrWhiteSpace(expressionBody))
                {
                    LogErrorMsg("Failed to extract variable name and expression body. Make sure the expression starts with a variable name followed by assignment operator `=`.");
                    continue;
                }

                var result = ExpressionEvaluator.Evaluate<string>(expressionBody, LogErrorMsg);
                if (result is null) continue;

                VariableManager.SetVariableValue(variableName, result);
                saveStatePending = saveStatePending || CustomVariablesConfiguration.IsGlobalVariable(variableName);
            }

            if (saveStatePending)
                await StateManager.SaveGlobalAsync();
        }

        private void LogErrorMsg (string desc = null) => LogErrorWithPosition($"Failed to evaluate set expression `{Expression}`. {desc ?? string.Empty}");
    }
}
