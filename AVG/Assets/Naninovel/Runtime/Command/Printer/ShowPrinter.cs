// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows a text printer.
    /// </summary>
    public class ShowPrinter : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), IDEActor(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        /// <summary>
        /// Duration (in seconds) of the show animation.
        /// Default value for each printer is set in the actor configuration.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;

        protected override string AssignedPrinterId => PrinterId;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.CancelASAP) return;

            var printerMeta = PrinterManager.Configuration.GetMetadataOrDefault(printer.Id);
            var showDuration = Assigned(Duration) ? Duration.Value : printerMeta.ChangeVisibilityDuration;

            if (cancellationToken.CancelLazy)
                printer.Visible = true;
            else await printer.ChangeVisibilityAsync(true, showDuration, cancellationToken: cancellationToken);
        }
    } 
}
