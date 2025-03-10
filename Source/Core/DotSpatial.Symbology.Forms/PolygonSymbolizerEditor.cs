// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using DotSpatial.Serialization;

namespace DotSpatial.Symbology.Forms
{
    /// <summary>
    /// PolygonSymbolizerEditor.
    /// </summary>
    public class PolygonSymbolizerEditor : UITypeEditor
    {
        #region Fields

        private IPolygonSymbolizer _copy;
        private IPolygonSymbolizer _original;

        #endregion

        #region Methods

        /// <summary>
        /// Launches a form for editing the line symbolizer.
        /// </summary>
        /// <param name="context">The type descriptor context.</param>
        /// <param name="provider">The service provider.</param>
        /// <param name="value">A polygon symbolizer.</param>
        /// <returns>The polygon symbolizer.</returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _original = value as IPolygonSymbolizer;
            if (_original == null) return value;
            _copy = _original.Copy();
            IWindowsFormsEditorService dialogProvider = (IWindowsFormsEditorService)provider?.GetService(typeof(IWindowsFormsEditorService));
            DetailedPolygonSymbolDialog dialog = new(_copy);
            dialog.ChangesApplied += DialogChangesApplied;
            if (dialogProvider?.ShowDialog(dialog) != DialogResult.OK) return _original;
            _original.CopyProperties(_copy);
            return value;
        }

        /// <summary>
        /// Indicates to launch a form, rather than using a drop-down edit style.
        /// </summary>
        /// <param name="context">The type descriptor context.</param>
        /// <returns>The UITypeEditorEditStyle.</returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        private void DialogChangesApplied(object sender, EventArgs e)
        {
            _original.CopyProperties(_copy);
        }

        #endregion
    }
}