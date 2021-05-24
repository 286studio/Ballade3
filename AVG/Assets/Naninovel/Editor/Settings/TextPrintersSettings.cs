// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class TextPrintersSettings : OrthoActorManagerSettings<TextPrintersConfiguration, ITextPrinterActor, TextPrinterMetadata>
    {
        protected override string HelpUri => "guide/text-printers.html";
        protected override string ResourcesSelectionTooltip => GetTooltip();

        protected override Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers ()
        {
            var drawers = base.OverrideMetaDrawers();
            drawers[nameof(TextPrinterMetadata.EnableDepthPass)] = null;
            drawers[nameof(TextPrinterMetadata.DepthAlphaCutoff)] = null;
            drawers[nameof(TextPrinterMetadata.CustomTextureShader)] = null;
            drawers[nameof(TextPrinterMetadata.CustomSpriteShader)] = null;
            drawers[nameof(TextPrinterMetadata.RenderTexture)] = null;
            drawers[nameof(TextPrinterMetadata.CorrectRenderAspect)] = null;
            drawers[nameof(TextPrinterMetadata.SplitBacklogMessages)] = p => { if (EditedMetadata.AddToBacklog) EditorGUILayout.PropertyField(p); };
            return drawers;
        }
        
        private string GetTooltip ()
        {
            if (EditedActorId == Configuration.DefaultPrinterId)
                return "This printer will be active by default: all the generic text and `@print` commands will use it to output the text. Use `@printer PrinterID` action to change active printer.";
            return $"Use `@printer {EditedActorId}` in naninovel scripts to set this printer active; all the consequent generic text and `@print` commands will then use it to output the text.";
        }

        [MenuItem("Naninovel/Resources/Text Printers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
