// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class LabelLineView : ScriptLineView
    {
        public readonly LineTextField ValueField;

        public LabelLineView (int lineIndex, string lineText, VisualElement container)
            : base(lineIndex, container)
        {
            var value = lineText.GetAfterFirst(Lexing.Constants.LabelLineId)?.TrimFull();
            ValueField = new LineTextField(Lexing.Constants.LabelLineId, value);
            Content.Add(ValueField);
        }

        public override string GenerateLineText () => $"{Lexing.Constants.LabelLineId} {ValueField.value}";
    }
}
