// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommentLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public CommentLineView (int lineIndex, string lineText, VisualElement container)
            : base(lineIndex, container)
        {
            var value = lineText.GetAfterFirst(Lexing.Constants.CommentLineId)?.TrimFull();
            valueField = new LineTextField(Lexing.Constants.CommentLineId, value);
            valueField.multiline = true;
            Content.Add(valueField);
        }

        public override string GenerateLineText () => $"{Lexing.Constants.CommentLineId} {valueField.value}";
    }
}
