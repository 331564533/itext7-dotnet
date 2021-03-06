/*

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Tagutils;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;

namespace iText.Layout.Renderer {
    public class ListItemRenderer : DivRenderer {
        protected internal IRenderer symbolRenderer;

        protected internal float symbolAreaWidth;

        /// <summary>Creates a ListItemRenderer from its corresponding layout object.</summary>
        /// <param name="modelElement">
        /// the
        /// <see cref="iText.Layout.Element.ListItem"/>
        /// which this object should manage
        /// </param>
        public ListItemRenderer(ListItem modelElement)
            : base(modelElement) {
        }

        public virtual void AddSymbolRenderer(IRenderer symbolRenderer, float symbolAreaWidth) {
            this.symbolRenderer = symbolRenderer;
            this.symbolAreaWidth = symbolAreaWidth;
        }

        public override LayoutResult Layout(LayoutContext layoutContext) {
            if (symbolRenderer != null && this.GetProperty<Object>(Property.HEIGHT) == null) {
                // TODO this is actually MinHeight.
                SetProperty(Property.HEIGHT, symbolRenderer.GetOccupiedArea().GetBBox().GetHeight());
            }
            return base.Layout(layoutContext);
        }

        public override void Draw(DrawContext drawContext) {
            bool isTagged = drawContext.IsTaggingEnabled() && GetModelElement() is IAccessibleElement;
            TagTreePointer tagPointer = null;
            if (isTagged) {
                tagPointer = drawContext.GetDocument().GetTagStructureContext().GetAutoTaggingPointer();
                IAccessibleElement modelElement = (IAccessibleElement)GetModelElement();
                PdfName role = modelElement.GetRole();
                if (role != null && !PdfName.Artifact.Equals(role)) {
                    bool lBodyTagIsCreated = tagPointer.IsElementConnectedToTag(modelElement);
                    if (!lBodyTagIsCreated) {
                        tagPointer.AddTag(PdfName.LI);
                    }
                    else {
                        tagPointer.MoveToTag(modelElement).MoveToParent();
                    }
                }
                else {
                    isTagged = false;
                }
            }
            base.Draw(drawContext);
            // It will be null in case of overflow (only the "split" part will contain symbol renderer.
            if (symbolRenderer != null) {
                symbolRenderer.SetParent(parent);
                float x = occupiedArea.GetBBox().GetX();
                if (childRenderers.Count > 0) {
                    float? yLine = ((AbstractRenderer)childRenderers[0]).GetFirstYLineRecursively();
                    if (yLine != null) {
                        if (symbolRenderer is TextRenderer) {
                            ((TextRenderer)symbolRenderer).MoveYLineTo((float)yLine);
                        }
                        else {
                            symbolRenderer.Move(0, (float)yLine - symbolRenderer.GetOccupiedArea().GetBBox().GetY());
                        }
                    }
                    else {
                        symbolRenderer.Move(0, occupiedArea.GetBBox().GetY() + occupiedArea.GetBBox().GetHeight() - (symbolRenderer
                            .GetOccupiedArea().GetBBox().GetY() + symbolRenderer.GetOccupiedArea().GetBBox().GetHeight()));
                    }
                }
                else {
                    symbolRenderer.Move(0, occupiedArea.GetBBox().GetY() + occupiedArea.GetBBox().GetHeight() - symbolRenderer
                        .GetOccupiedArea().GetBBox().GetHeight() - symbolRenderer.GetOccupiedArea().GetBBox().GetY());
                }
                ListSymbolAlignment listSymbolAlignment = (ListSymbolAlignment)parent.GetProperty<ListSymbolAlignment?>(Property
                    .LIST_SYMBOL_ALIGNMENT, ListSymbolAlignment.RIGHT);
                float xPosition = x - symbolRenderer.GetOccupiedArea().GetBBox().GetX();
                if (listSymbolAlignment == ListSymbolAlignment.RIGHT) {
                    xPosition += symbolAreaWidth - symbolRenderer.GetOccupiedArea().GetBBox().GetWidth();
                }
                symbolRenderer.Move(xPosition, 0);
                if (isTagged) {
                    tagPointer.AddTag(0, PdfName.Lbl);
                }
                symbolRenderer.Draw(drawContext);
                if (isTagged) {
                    tagPointer.MoveToParent();
                }
            }
            if (isTagged) {
                tagPointer.MoveToParent();
            }
        }

        public override IRenderer GetNextRenderer() {
            return new iText.Layout.Renderer.ListItemRenderer((ListItem)modelElement);
        }

        protected internal override AbstractRenderer CreateSplitRenderer(int layoutResult) {
            iText.Layout.Renderer.ListItemRenderer splitRenderer = (iText.Layout.Renderer.ListItemRenderer)GetNextRenderer
                ();
            splitRenderer.parent = parent;
            splitRenderer.modelElement = modelElement;
            splitRenderer.occupiedArea = occupiedArea;
            if (layoutResult == LayoutResult.PARTIAL) {
                splitRenderer.symbolRenderer = symbolRenderer;
                splitRenderer.symbolAreaWidth = symbolAreaWidth;
            }
            // TODO retain all the properties ?
            splitRenderer.SetProperty(Property.MARGIN_LEFT, this.GetProperty<Object>(Property.MARGIN_LEFT));
            return splitRenderer;
        }

        protected internal override AbstractRenderer CreateOverflowRenderer(int layoutResult) {
            iText.Layout.Renderer.ListItemRenderer overflowRenderer = (iText.Layout.Renderer.ListItemRenderer)GetNextRenderer
                ();
            overflowRenderer.parent = parent;
            overflowRenderer.modelElement = modelElement;
            if (layoutResult == LayoutResult.NOTHING) {
                overflowRenderer.symbolRenderer = symbolRenderer;
                overflowRenderer.symbolAreaWidth = symbolAreaWidth;
            }
            // TODO retain all the properties ?
            overflowRenderer.SetProperty(Property.MARGIN_LEFT, this.GetProperty<Object>(Property.MARGIN_LEFT));
            return overflowRenderer;
        }
    }
}
