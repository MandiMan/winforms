﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using static Interop.UiaCore;

namespace System.Windows.Forms;

public abstract partial class TextBoxBase
{
    internal class TextBoxBaseAccessibleObject : ControlAccessibleObject
    {
        private readonly TextBoxBase _owningTextBoxBase;
        private TextBoxBaseUiaTextProvider? _textProvider;

        public TextBoxBaseAccessibleObject(TextBoxBase owner) : base(owner)
        {
            _owningTextBoxBase = owner;
            _textProvider = new TextBoxBaseUiaTextProvider(owner);
        }

        internal void ClearObjects()
        {
            _textProvider = null;

            // A place for future memory leak fixing:
            //
            // 1) This method should be added to the ControlAccessibleObject class:
            //    internal void ClearOwnerControl()
            //    {
            //        this.Owner = null;
            //    }
            //
            // 2) Owner property shoud be changed from this
            //        public Control Owner { get; }
            //    to something like this
            //        public Control? Owner { get; private set; }
            //
            // 3) These changes will produce different sorts of warnings:
            //     non-nullable member will became nullable, additional checks for null should be added, etc.
            //
            // 4) This method call should be uncommented
            //        ClearOwnerControl();
        }

        internal override object? GetPropertyValue(UIA propertyID)
            => propertyID switch
            {
                UIA.IsPasswordPropertyId => _owningTextBoxBase.PasswordProtect,
                _ => base.GetPropertyValue(propertyID),
            };

        internal override bool IsIAccessibleExSupported() => true;

        internal override bool IsPatternSupported(UIA patternId)
            => patternId switch
            {
                UIA.TextPatternId => true,
                UIA.TextPattern2Id => true,
                UIA.ValuePatternId => true,
                _ => base.IsPatternSupported(patternId)
            };

        internal override bool IsReadOnly => _owningTextBoxBase.ReadOnly;

        public override string? Name
        {
            get
            {
                var name = base.Name;
                return name is not null || !_owningTextBoxBase.PasswordProtect ? name : string.Empty;
            }
            set => base.Name = value;
        }

        public override string? Value => !_owningTextBoxBase.PasswordProtect ? ValueInternal : SR.AccessDenied;

        protected virtual string ValueInternal
            => this.TryGetOwnerAs(out Control? owner) && owner.Text is { } text ? text : string.Empty;

        internal override void SetFocus()
        {
            if (!this.TryGetOwnerAs(out Control? owner) || !owner.IsHandleCreated)
            {
                return;
            }

            base.SetFocus();

            RaiseAutomationEvent(UIA.AutomationFocusChangedEventId);
        }

        internal override void SetValue(string? newValue)
        {
            if (this.TryGetOwnerAs(out Control? owner))
            {
                owner.Text = newValue;
            }

            base.SetValue(newValue);
        }

        internal override ITextRangeProvider? DocumentRangeInternal
            => _textProvider?.DocumentRange;

        internal override ITextRangeProvider[]? GetTextSelection()
            => _textProvider?.GetSelection();

        internal override ITextRangeProvider[]? GetTextVisibleRanges()
            => _textProvider?.GetVisibleRanges();

        internal override ITextRangeProvider? GetTextRangeFromChild(IRawElementProviderSimple childElement)
            => _textProvider?.RangeFromChild(childElement);

        internal override ITextRangeProvider? GetTextRangeFromPoint(Point screenLocation)
            => _textProvider?.RangeFromPoint(screenLocation);

        internal override SupportedTextSelection SupportedTextSelectionInternal
            => _textProvider?.SupportedTextSelection ?? SupportedTextSelection.None;

        internal override ITextRangeProvider? GetTextCaretRange(out BOOL isActive)
        {
            isActive = false;
            return _textProvider?.GetCaretRange(out isActive);
        }

        internal override ITextRangeProvider? GetRangeFromAnnotation(IRawElementProviderSimple annotationElement)
            => _textProvider?.RangeFromAnnotation(annotationElement);
    }
}
