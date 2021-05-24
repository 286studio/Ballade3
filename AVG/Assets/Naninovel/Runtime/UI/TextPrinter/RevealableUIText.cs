// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class RevealableUIText : Text, IRevealableText
    {
        private class UGUIRevealBehaviour : TextRevealBehaviour
        {
            private readonly RevealableUIText ugui;
            private readonly Material[] materials = new Material[1];

            public UGUIRevealBehaviour (RevealableUIText ugui)
                : base(ugui, ugui.slideClipRect, false, ugui.revealFadeWidth)
            {
                this.ugui = ugui;
                materials[0] = ugui.material;
            }

            protected override float GetScaleModifier () => 1 / ugui.pixelsPerUnit;
            protected override Vector2 GetTextRectSize () => ugui.cachedTextGenerator.rectExtents.size;
            protected override int GetCharacterCount () => ugui.lastVisibleCharIndex + 1;
            protected override RevealableCharacter GetCharacterAt (int index) => ugui.GetVisibleCharAt(index);
            protected override RevealableLine GetLineAt (int index) => ugui.GetLineAt(index);
            protected override IReadOnlyList<Material> GetMaterials () => materials;
        }

        public virtual string Text { get => text; set => text = value; }
        public virtual Color TextColor { get => color; set => color = value; }
        public virtual GameObject GameObject => gameObject;
        public virtual bool Revealing => revealBehaviour.Revealing;
        public virtual float RevealProgress { get => revealBehaviour.GetRevealProgress(); set => revealBehaviour.SetRevealProgress(value); }

        [Tooltip("Width (in pixels) of the gradient fade near the reveal border.")]
        [SerializeField] private float revealFadeWidth = 100f;
        [Tooltip("Whether to smoothly reveal the text. Disable for the `typewriter` effect.")]
        [SerializeField] private bool slideClipRect = true;
        [Tooltip("How much to slant the reveal rect to compensate for italic characters; 10 is usually enough for most fonts.\n\nNotice, that enabling the slanting (value greater than zero) would introduce minor reveal effect artifacts. TMPro printers are not affected by this issue, so consider using them instead.")]
        [SerializeField] private float italicSlantAngle = 0f;

        private const string textShaderName = "Naninovel/RevealableText";

        private bool isEdited => !Application.isPlaying || ObjectUtils.IsEditedInPrefabMode(gameObject);
        private UGUIRevealBehaviour revealBehaviour;
        private int lastVisibleCharIndex = -1;

        public virtual void RevealNextChars (int count, float duration, CancellationToken cancellationToken)
        {
            revealBehaviour.RevealNextChars(count, duration, cancellationToken);
        }

        public virtual Vector2 GetLastRevealedCharPosition ()
        {
            return revealBehaviour.GetLastRevealedCharPosition();
        }

        public virtual char GetLastRevealedChar ()
        {
            var absIndex = VisibleToAbsoluteCharIndex(revealBehaviour.LastRevealedCharIndex);
            if (Text is null || absIndex < 0 || absIndex >= Text.Length) return default;
            return Text[absIndex];
        }

        public override void Rebuild (CanvasUpdate update)
        {
            base.Rebuild(update);

            lastVisibleCharIndex = FindLastVisibleCharIndex();
            revealBehaviour?.Rebuild();
        }

        protected override void Awake ()
        {
            base.Awake();
            material = new Material(Shader.Find(textShaderName));
            revealBehaviour = new UGUIRevealBehaviour(this);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();
            RegisterDirtyLayoutCallback(revealBehaviour.WaitForRebuild);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            UnregisterDirtyLayoutCallback(revealBehaviour.WaitForRebuild);
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange();
            if (isEdited || !canvas || revealBehaviour is null) return;

            // When text layout changes (eg, content size fitter decides to increase height),
            // we need to force-update clip rect; otherwise it will be delayed by one frame
            // and user will see incorrectly revealed text for a moment.
            revealBehaviour.UpdateClipRects();
            revealBehaviour.Update();
        }

        private void Update ()
        {
            if (isEdited) return;
            revealBehaviour.Update();
        }

        private void LateUpdate ()
        {
            if (isEdited) return;
            revealBehaviour.LateUpdate();
        }

        private RevealableLine GetLineAt (int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= cachedTextGenerator.lines.Count)
                return RevealableLine.Invalid;

            var lineInfo = cachedTextGenerator.lines[lineIndex];
            var lineFirstChar = GetVisibleCharAt(AbsoluteToVisibleCharIndex(lineInfo.startCharIdx)).CharIndex;
            var lineLastChar = GetLastVisibleCharAtLine(lineInfo.startCharIdx, lineIndex).CharIndex;
            return new RevealableLine(lineIndex, lineInfo.height, lineInfo.topY, lineFirstChar, lineLastChar);
        }

        private RevealableCharacter GetVisibleCharAt (int requestedVisibleCharIndex)
        {
            var absoluteIndex = VisibleToAbsoluteCharIndex(requestedVisibleCharIndex);
            if (absoluteIndex < 0 || absoluteIndex >= cachedTextGenerator.characterCount)
                return RevealableCharacter.Invalid;

            var lineInfo = FindLineContainingChar(absoluteIndex, out var lineIndex);
            var charInfo = cachedTextGenerator.characters[absoluteIndex];
            var origin = charInfo.cursorPos.x;
            var xAdvance = charInfo.cursorPos.x + charInfo.charWidth;
            return new RevealableCharacter(requestedVisibleCharIndex, lineIndex, origin, xAdvance, italicSlantAngle, 0);
        }

        private RevealableCharacter GetLastVisibleCharAtLine (int firstAbsoluteCharInLineIndex, int lineIndex)
        {
            var curVisibleCharIndex = -1;
            var resultIndex = -1;
            for (var i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth > 0)
                    curVisibleCharIndex++;
                if (i < firstAbsoluteCharInLineIndex) continue;

                FindLineContainingChar(i, out var curLindeIndex);
                if (lineIndex < curLindeIndex) break;

                resultIndex = curVisibleCharIndex;
            }
            return GetVisibleCharAt(resultIndex);
        }

        private UILineInfo FindLineContainingChar (int absoluteCharIndex, out int lineIndex)
        {
            lineIndex = 0;
            for (int i = 0; i < cachedTextGenerator.lineCount; i++)
            {
                if (cachedTextGenerator.lines[i].startCharIdx > absoluteCharIndex)
                    break;
                lineIndex = i;
            }
            return cachedTextGenerator.lines[lineIndex];
        }

        private int FindNextVisibleCharIndex (int startVisibleCharIndex = 0)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (curVisibleIndex <= startVisibleCharIndex) continue;
                return curVisibleIndex;
            }
            return -1;
        }

        private int FindLastVisibleCharIndex ()
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
            }
            return curVisibleIndex;
        }

        private int AbsoluteToVisibleCharIndex (int absoluteCharIndex)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (i >= absoluteCharIndex) break;
            }
            return curVisibleIndex;
        }

        private int VisibleToAbsoluteCharIndex (int visibleCharIndex)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (curVisibleIndex >= visibleCharIndex) return i;
            }
            return -1;
        }
    }
}
