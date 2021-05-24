// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Naninovel.ArabicSupport;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class RevealableTMProText : TextMeshProUGUI, IRevealableText, IPointerClickHandler, IInputTrigger
    {
        [Serializable]
        private class TipClickedEvent : UnityEvent<string> { }

        private class TMProRevealBehaviour : TextRevealBehaviour
        {
            private readonly RevealableTMProText tmPro;

            public TMProRevealBehaviour (RevealableTMProText tmPro)
                : base(tmPro, tmPro.slideClipRect, tmPro.isRightToLeftText, tmPro.revealFadeWidth)
            {
                this.tmPro = tmPro;
            }

            protected override Vector2 GetTextRectSize () => new Vector2(tmPro.m_marginWidth, tmPro.m_marginHeight);
            protected override int GetCharacterCount () => tmPro.textInfo.characterCount;
            protected override RevealableCharacter GetCharacterAt (int index) => tmPro.GetCharacterAt(index);
            protected override RevealableLine GetLineAt (int index) => tmPro.GetLineAt(index);
            protected override IReadOnlyList<Material> GetMaterials () => tmPro.GetMaterials();
        }

        public virtual string Text { get => assignedText; set => SetTextToReveal(value); }
        public virtual Color TextColor { get => color; set => color = value; }
        public virtual GameObject GameObject => gameObject;
        public virtual bool Revealing => revealBehaviour.Revealing;
        public virtual float RevealProgress { get => revealBehaviour.GetRevealProgress(); set => revealBehaviour.SetRevealProgress(value); }

        protected virtual string RubyVerticalOffset => rubyVerticalOffset;
        protected virtual float RubySizeScale => rubySizeScale;
        protected virtual bool FixArabicText => fixArabicText;
        protected virtual Canvas TopmostCanvas => topmostCanvasCache ? topmostCanvasCache : topmostCanvasCache = gameObject.FindTopmostComponent<Canvas>();

        [Tooltip("Width (in pixels) of the gradient fade near the reveal border.")]
        [SerializeField] private float revealFadeWidth = 100f;
        [Tooltip("Whether to smoothly reveal the text. Disable for the `typewriter` effect.")]
        [SerializeField] private bool slideClipRect = true;
        [Tooltip("How much to slant the reveal rect when passing over italic characters.")]
        [SerializeField] private float italicSlantAngle = 10f;
        [Tooltip("Vertical line offset to use for the ruby (furigana) text; supported units: em, px, %.")]
        [SerializeField] private string rubyVerticalOffset = "1em";
        [Tooltip("Font size scale (relative to the main text font size) to apply for the ruby (furigana) text.")]
        [SerializeField] private float rubySizeScale = .5f;
        [Tooltip("Whether to automatically unlock associated tip records when text wrapped in <tip> tags is printed.")]
        [SerializeField] private bool unlockTipsOnPrint = true;
        [Tooltip("Template to use when processing text wrapped in <tip> tags. " + tipTemplateLiteral + " will be replaced with the actual tip content.")]
        [SerializeField] private string tipTemplate = $"<u>{tipTemplateLiteral}</u>";
        [Tooltip("Invoked when a text wrapped in <tip> tags is clicked; returned string argument is the ID of the clicked tip. Be aware, that the default behaviour (showing `ITipsUI` when a tip is clicked) won't be invoked when a custom handler is assigned.")]
        [SerializeField] private TipClickedEvent onTipClicked = default;
        [Tooltip("Whether to modify the text to support arabic languages (fix letters connectivity issues).")]
        [SerializeField] private bool fixArabicText = false;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether also fix Farsi characters.")]
        [SerializeField] private bool fixArabicFarsi = true;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether also fix rich text tags.")]
        [SerializeField] private bool fixArabicTextTags = true;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether preserve numbers.")]
        [SerializeField] private bool fixArabicPreserveNumbers = false;

        private const string tipIdPrefix = "NANINOVEL.TIP.";
        private const string tipTemplateLiteral = "%TIP%";
        private static readonly Regex captureRubyRegex = new Regex(@"<ruby=""([\s\S]*?)"">([\s\S]*?)<\/ruby>", RegexOptions.Compiled);
        private static readonly Regex captureTipRegex = new Regex(@"<tip=""([\w]*?)"">([\s\S]*?)<\/tip>", RegexOptions.Compiled);

        private readonly FastStringBuilder arabicBuilder = new FastStringBuilder(RTLSupport.DefaultBufferSize);
        private bool isEdited => !Application.isPlaying || ObjectUtils.IsEditedInPrefabMode(gameObject);
        private Canvas topmostCanvasCache;
        private Material[] cachedFontMaterials;
        private TMProRevealBehaviour revealBehaviour;
        private string assignedText;

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
            if (string.IsNullOrEmpty(Text) || revealBehaviour.LastRevealedCharIndex < 0 || revealBehaviour.LastRevealedCharIndex >= Text.Length)
                return default;
            return Text[revealBehaviour.LastRevealedCharIndex];
        }

        public void OnPointerClick (PointerEventData eventData)
        {
            var renderCamera = TopmostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TopmostCanvas.worldCamera;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(this, eventData.position, renderCamera);
            if (linkIndex == -1) return;

            var linkInfo = textInfo.linkInfo[linkIndex];
            var linkId = linkInfo.GetLinkID();
            if (!linkId.StartsWithFast(tipIdPrefix)) return;

            var tipId = linkId.GetAfter(tipIdPrefix);
            if (onTipClicked?.GetPersistentEventCount() > 0)
            {
                onTipClicked.Invoke(tipId);
                return;
            }

            var tipsUI = Engine.GetService<IUIManager>()?.GetUI<ITipsUI>();
            tipsUI?.Show();
            tipsUI?.SelectTipRecord(tipId);
        }

        public bool CanTriggerInput ()
        {
            var evtSystem = EventSystem.current;
            if (!evtSystem) return true;
            var inputModule = evtSystem.currentInputModule;
            if (!inputModule) return true;
            var input = inputModule.input;
            if (!input) return true;

            var position = input.mousePosition;
            var renderCamera = TopmostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TopmostCanvas.worldCamera;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(this, position, renderCamera);
            return linkIndex == -1;
        }

        public override void Rebuild (CanvasUpdate update)
        {
            base.Rebuild(update);
            revealBehaviour?.Rebuild();
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange();
            if (isEdited || !canvas || revealBehaviour is null) return;

            // When text layout changes (eg, content size fitter decides to increase height),
            // we need to force-update clip rect; otherwise, the update will be delayed by one frame
            // and user will see incorrectly revealed text for a moment.
            revealBehaviour.UpdateClipRects();
            revealBehaviour.Update();
        }

        protected override void Awake ()
        {
            base.Awake();
            revealBehaviour = new TMProRevealBehaviour(this);
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

        /// <summary>
        /// Given the input text, extracts text wrapped in ruby tags and replace it with expression natively supported by TMPro.
        /// </summary>
        protected virtual string ProcessRubyTags (string content)
        {
            // Replace <ruby> tags with TMPro-supported rich text tags
            // to simulate ruby (furigana) text layout.
            var matches = captureRubyRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 3) continue;
                var fullMatch = match.Groups[0].ToString();
                var rubyValue = match.Groups[1].ToString();
                var baseText = match.Groups[2].ToString();

                var baseTextWidth = GetPreferredValues(baseText).x;
                var rubyTextWidth = GetPreferredValues(rubyValue).x * rubySizeScale;
                var rubyTextOffset = baseTextWidth / 2f + rubyTextWidth / 2f;
                var compensationOffset = (baseTextWidth - rubyTextWidth) / 2f;
                var replace = $"<space={compensationOffset}><voffset={rubyVerticalOffset}><size={rubySizeScale * 100f}%>{rubyValue}</size></voffset><space=-{rubyTextOffset}>{baseText}";
                content = content.Replace(fullMatch, replace);
            }

            return content;
        }

        /// <summary>
        /// Given the input text, extracts text wrapped in tip tags and replace it with expression natively supported by TMPro.
        /// </summary>
        protected virtual string ProcessTipTags (string content)
        {
            var matches = captureTipRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 3) continue;
                var fullMatch = match.Groups[0].ToString();
                var tipID = match.Groups[1].ToString();
                var tipContent = match.Groups[2].ToString();

                if (unlockTipsOnPrint)
                    Engine.GetService<IUnlockableManager>()?.UnlockItem($"Tips/{tipID}");

                var replace = $"<link={tipIdPrefix + tipID}>{tipTemplate.Replace(tipTemplateLiteral, tipContent)}</link>";
                content = content.Replace(fullMatch, replace);
            }

            return content;
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

        private void SetTextToReveal (string value)
        {
            assignedText = value;

            // Pre-process the assigned text handling <ruby> and <tip> tags.
            text = ProcessRubyTags(ProcessTipTags(value));

            if (FixArabicText && !string.IsNullOrWhiteSpace(text))
            {
                arabicBuilder.Clear();
                RTLSupport.FixRTL(text, arabicBuilder, fixArabicFarsi, fixArabicTextTags, fixArabicPreserveNumbers);
                arabicBuilder.Reverse();
                text = arabicBuilder.ToString();
            }
        }

        private RevealableLine GetLineAt (int index)
        {
            if (index < 0 || index >= textInfo.lineInfo.Length)
                return RevealableLine.Invalid;

            var info = textInfo.lineInfo[index];
            return new RevealableLine(index, info.lineHeight, info.ascender, info.firstCharacterIndex, info.lastCharacterIndex);
        }

        private RevealableCharacter GetCharacterAt (int index)
        {
            if (index < 0 || index >= textInfo.characterInfo.Length)
                return RevealableCharacter.Invalid;

            var info = textInfo.characterInfo[index];
            var slantAngle = info.style == FontStyles.Italic ? italicSlantAngle : 0f;
            return new RevealableCharacter(index, info.lineNumber, info.origin, info.xAdvance, slantAngle, info.vertex_BR.position.x);
        }

        private Material[] GetMaterials ()
        {
            if (cachedFontMaterials is null || cachedFontMaterials.Length != textInfo.materialCount)
                cachedFontMaterials = fontMaterials; // Material count change when using fallback fonts or font variants (weights).
            return cachedFontMaterials;
        }
    }
}
