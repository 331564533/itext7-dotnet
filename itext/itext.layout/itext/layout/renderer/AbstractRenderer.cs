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
using System.Collections.Generic;
using System.Text;
using iText.IO;
using iText.IO.Log;
using iText.IO.Util;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Tagutils;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;

namespace iText.Layout.Renderer {
    /// <summary>
    /// Defines the most common properties and behavior that are shared by most
    /// <see cref="IRenderer"/>
    /// implementations. All default Renderers are subclasses of
    /// this default implementation.
    /// </summary>
    public abstract class AbstractRenderer : IRenderer {
        /// <summary>
        /// The maximum difference between
        /// <see cref="iText.Kernel.Geom.Rectangle"/>
        /// coordinates to consider rectangles equal
        /// </summary>
        public const float EPS = 1e-4f;

        /// <summary>The infinity value which is used while layouting</summary>
        public const float INF = 1e6f;

        protected internal IList<IRenderer> childRenderers = new List<IRenderer>();

        protected internal IList<IRenderer> positionedRenderers = new List<IRenderer>();

        protected internal IPropertyContainer modelElement;

        protected internal bool flushed = false;

        protected internal LayoutArea occupiedArea;

        protected internal IRenderer parent;

        protected internal IDictionary<int, Object> properties = new Dictionary<int, Object>();

        protected internal bool isLastRendererForModelElement = true;

        /// <summary>Creates a renderer.</summary>
        protected internal AbstractRenderer() {
        }

        /// <summary>Creates a renderer for the specified layout element.</summary>
        /// <param name="modelElement">the layout element that will be drawn by this renderer</param>
        protected internal AbstractRenderer(IElement modelElement) {
            // TODO linkedList?
            this.modelElement = modelElement;
        }

        protected internal AbstractRenderer(iText.Layout.Renderer.AbstractRenderer other) {
            this.childRenderers = other.childRenderers;
            this.positionedRenderers = other.positionedRenderers;
            this.modelElement = other.modelElement;
            this.flushed = other.flushed;
            this.occupiedArea = other.occupiedArea.Clone();
            this.parent = other.parent;
            this.properties.AddAll(other.properties);
            this.isLastRendererForModelElement = other.isLastRendererForModelElement;
        }

        /// <summary><inheritDoc/></summary>
        public virtual void AddChild(IRenderer renderer) {
            // https://www.webkit.org/blog/116/webcore-rendering-iii-layout-basics
            // "The rules can be summarized as follows:"...
            int? positioning = renderer.GetProperty<int?>(Property.POSITION);
            if (positioning == null || positioning == LayoutPosition.RELATIVE || positioning == LayoutPosition.STATIC) {
                childRenderers.Add(renderer);
            }
            else {
                if (positioning == LayoutPosition.FIXED) {
                    iText.Layout.Renderer.AbstractRenderer root = this;
                    while (root.parent is iText.Layout.Renderer.AbstractRenderer) {
                        root = (iText.Layout.Renderer.AbstractRenderer)root.parent;
                    }
                    if (root == this) {
                        positionedRenderers.Add(renderer);
                    }
                    else {
                        root.AddChild(renderer);
                    }
                }
            }
        }

        /// <summary><inheritDoc/></summary>
        public virtual IPropertyContainer GetModelElement() {
            return modelElement;
        }

        /// <summary><inheritDoc/></summary>
        public virtual IList<IRenderer> GetChildRenderers() {
            return childRenderers;
        }

        /// <summary><inheritDoc/></summary>
        public virtual bool HasProperty(int property) {
            return HasOwnProperty(property) || (modelElement != null && modelElement.HasProperty(property)) || (parent
                 != null && Property.IsPropertyInherited(property) && parent.HasProperty(property));
        }

        /// <summary><inheritDoc/></summary>
        public virtual bool HasOwnProperty(int property) {
            return properties.ContainsKey(property);
        }

        /// <summary>
        /// Checks if this renderer or its model element have the specified property,
        /// i.e.
        /// </summary>
        /// <remarks>
        /// Checks if this renderer or its model element have the specified property,
        /// i.e. if it was set to this very element or its very model element earlier.
        /// </remarks>
        /// <param name="property">the property to be checked</param>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if this instance or its model element have given own property,
        /// <see langword="false"/>
        /// otherwise
        /// </returns>
        public virtual bool HasOwnOrModelProperty(int property) {
            return properties.ContainsKey(property) || (null != GetModelElement() && GetModelElement().HasProperty(property
                ));
        }

        /// <summary><inheritDoc/></summary>
        public virtual void DeleteOwnProperty(int property) {
            properties.JRemove(property);
        }

        /// <summary>
        /// Deletes property from this very renderer, or in case the property is specified on its model element, the
        /// property of the model element is deleted
        /// </summary>
        /// <param name="property">the property key to be deleted</param>
        public virtual void DeleteProperty(int property) {
            if (properties.ContainsKey(property)) {
                properties.JRemove(property);
            }
            else {
                if (modelElement != null) {
                    modelElement.DeleteOwnProperty(property);
                }
            }
        }

        /// <summary><inheritDoc/></summary>
        public virtual T1 GetProperty<T1>(int key) {
            Object property;
            if ((property = properties.Get(key)) != null || properties.ContainsKey(key)) {
                return (T1)property;
            }
            if (modelElement != null && ((property = modelElement.GetProperty<T1>(key)) != null || modelElement.HasProperty
                (key))) {
                return (T1)property;
            }
            // TODO in some situations we will want to check inheritance with additional info, such as parent and descendant.
            if (parent != null && Property.IsPropertyInherited(key) && (property = parent.GetProperty<T1>(key)) != null
                ) {
                return (T1)property;
            }
            property = this.GetDefaultProperty<T1>(key);
            if (property != null) {
                return (T1)property;
            }
            return modelElement != null ? modelElement.GetDefaultProperty<T1>(key) : (T1)(Object)null;
        }

        /// <summary><inheritDoc/></summary>
        public virtual T1 GetOwnProperty<T1>(int property) {
            return (T1)properties.Get(property);
        }

        /// <summary><inheritDoc/></summary>
        public virtual T1 GetProperty<T1>(int property, T1 defaultValue) {
            T1 result = this.GetProperty<T1>(property);
            return result != null ? result : defaultValue;
        }

        /// <summary><inheritDoc/></summary>
        public virtual void SetProperty(int property, Object value) {
            properties[property] = value;
        }

        /// <summary><inheritDoc/></summary>
        public virtual T1 GetDefaultProperty<T1>(int property) {
            return (T1)(Object)null;
        }

        /// <summary>Returns a property with a certain key, as a font object.</summary>
        /// <param name="property">
        /// an
        /// <see cref="iText.Layout.Properties.Property">enum value</see>
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </returns>
        public virtual PdfFont GetPropertyAsFont(int property) {
            return this.GetProperty<PdfFont>(property);
        }

        /// <summary>Returns a property with a certain key, as a color.</summary>
        /// <param name="property">
        /// an
        /// <see cref="iText.Layout.Properties.Property">enum value</see>
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Colors.Color"/>
        /// </returns>
        public virtual Color GetPropertyAsColor(int property) {
            return this.GetProperty<Color>(property);
        }

        /// <summary>Returns a property with a certain key, as a boolean value.</summary>
        /// <param name="property">
        /// an
        /// <see cref="iText.Layout.Properties.Property">enum value</see>
        /// </param>
        /// <returns>
        /// a
        /// <see cref="bool?"/>
        /// </returns>
        public virtual bool? GetPropertyAsBoolean(int property) {
            return this.GetProperty<bool?>(property);
        }

        /// <summary>Returns a string representation of the renderer.</summary>
        /// <returns>
        /// a
        /// <see cref="System.String"/>
        /// </returns>
        /// <seealso cref="System.Object.ToString()"/>
        public override String ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (IRenderer renderer in childRenderers) {
                sb.Append(renderer.ToString());
            }
            return sb.ToString();
        }

        /// <summary><inheritDoc/></summary>
        public virtual LayoutArea GetOccupiedArea() {
            return occupiedArea;
        }

        /// <summary><inheritDoc/></summary>
        public virtual void Draw(DrawContext drawContext) {
            ApplyDestination(drawContext.GetDocument());
            ApplyAction(drawContext.GetDocument());
            bool relativePosition = IsRelativePosition();
            if (relativePosition) {
                ApplyAbsolutePositioningTranslation(false);
            }
            DrawBackground(drawContext);
            DrawBorder(drawContext);
            DrawChildren(drawContext);
            if (relativePosition) {
                ApplyAbsolutePositioningTranslation(true);
            }
            flushed = true;
        }

        /// <summary>
        /// Draws a background layer if it is defined by a key
        /// <see cref="iText.Layout.Properties.Property.BACKGROUND"/>
        /// in either the layout element or this
        /// <see cref="IRenderer"/>
        /// itself.
        /// </summary>
        /// <param name="drawContext">the context (canvas, document, etc) of this drawing operation.</param>
        public virtual void DrawBackground(DrawContext drawContext) {
            Background background = this.GetProperty<Background>(Property.BACKGROUND);
            if (background != null) {
                Rectangle bBox = GetOccupiedAreaBBox();
                bool isTagged = drawContext.IsTaggingEnabled() && GetModelElement() is IAccessibleElement;
                if (isTagged) {
                    drawContext.GetCanvas().OpenTag(new CanvasArtifact());
                }
                Rectangle backgroundArea = ApplyMargins(bBox, false);
                if (backgroundArea.GetWidth() <= 0 || backgroundArea.GetHeight() <= 0) {
                    ILogger logger = LoggerFactory.GetLogger(typeof(iText.Layout.Renderer.AbstractRenderer));
                    logger.Error(String.Format(LogMessageConstant.RECTANGLE_HAS_NEGATIVE_OR_ZERO_SIZES, "background"));
                    return;
                }
                drawContext.GetCanvas().SaveState().SetFillColor(background.GetColor()).Rectangle(backgroundArea.GetX() - 
                    background.GetExtraLeft(), backgroundArea.GetY() - background.GetExtraBottom(), backgroundArea.GetWidth
                    () + background.GetExtraLeft() + background.GetExtraRight(), backgroundArea.GetHeight() + background.GetExtraTop
                    () + background.GetExtraBottom()).Fill().RestoreState();
                if (isTagged) {
                    drawContext.GetCanvas().CloseTag();
                }
            }
        }

        /// <summary>
        /// Performs the drawing operation for all
        /// <see cref="IRenderer">children</see>
        /// of this renderer.
        /// </summary>
        /// <param name="drawContext">the context (canvas, document, etc) of this drawing operation.</param>
        public virtual void DrawChildren(DrawContext drawContext) {
            foreach (IRenderer child in childRenderers) {
                child.Draw(drawContext);
            }
        }

        /// <summary>
        /// Performs the drawing operation for the border of this renderer, if
        /// defined by any of the
        /// <see cref="iText.Layout.Properties.Property.BORDER"/>
        /// values in either the layout
        /// element or this
        /// <see cref="IRenderer"/>
        /// itself.
        /// </summary>
        /// <param name="drawContext">the context (canvas, document, etc) of this drawing operation.</param>
        public virtual void DrawBorder(DrawContext drawContext) {
            Border[] borders = GetBorders();
            bool gotBorders = false;
            foreach (Border border in borders) {
                gotBorders = gotBorders || border != null;
            }
            if (gotBorders) {
                float topWidth = borders[0] != null ? borders[0].GetWidth() : 0;
                float rightWidth = borders[1] != null ? borders[1].GetWidth() : 0;
                float bottomWidth = borders[2] != null ? borders[2].GetWidth() : 0;
                float leftWidth = borders[3] != null ? borders[3].GetWidth() : 0;
                Rectangle bBox = GetBorderAreaBBox();
                if (bBox.GetWidth() <= 0 || bBox.GetHeight() <= 0) {
                    ILogger logger = LoggerFactory.GetLogger(typeof(iText.Layout.Renderer.AbstractRenderer));
                    logger.Error(String.Format(LogMessageConstant.RECTANGLE_HAS_NEGATIVE_OR_ZERO_SIZES, "border"));
                    return;
                }
                float x1 = bBox.GetX();
                float y1 = bBox.GetY();
                float x2 = bBox.GetX() + bBox.GetWidth();
                float y2 = bBox.GetY() + bBox.GetHeight();
                bool isTagged = drawContext.IsTaggingEnabled() && GetModelElement() is IAccessibleElement;
                PdfCanvas canvas = drawContext.GetCanvas();
                if (isTagged) {
                    canvas.OpenTag(new CanvasArtifact());
                }
                if (borders[0] != null) {
                    canvas.SaveState();
                    borders[0].Draw(canvas, x1, y2, x2, y2, leftWidth, rightWidth);
                    canvas.RestoreState();
                }
                if (borders[1] != null) {
                    canvas.SaveState();
                    borders[1].Draw(canvas, x2, y2, x2, y1, topWidth, bottomWidth);
                    canvas.RestoreState();
                }
                if (borders[2] != null) {
                    canvas.SaveState();
                    borders[2].Draw(canvas, x2, y1, x1, y1, rightWidth, leftWidth);
                    canvas.RestoreState();
                }
                if (borders[3] != null) {
                    canvas.SaveState();
                    borders[3].Draw(canvas, x1, y1, x1, y2, bottomWidth, topWidth);
                    canvas.RestoreState();
                }
                if (isTagged) {
                    canvas.CloseTag();
                }
            }
        }

        /// <summary>Indicates whether this renderer is flushed or not, i.e.</summary>
        /// <remarks>
        /// Indicates whether this renderer is flushed or not, i.e. if
        /// <see cref="Draw(DrawContext)"/>
        /// has already
        /// been called.
        /// </remarks>
        /// <returns>whether the renderer has been flushed</returns>
        /// <seealso cref="Draw(DrawContext)"/>
        public virtual bool IsFlushed() {
            return flushed;
        }

        /// <summary><inheritDoc/></summary>
        public virtual IRenderer SetParent(IRenderer parent) {
            this.parent = parent;
            return this;
        }

        /// <summary><inheritDoc/></summary>
        public virtual void Move(float dxRight, float dyUp) {
            occupiedArea.GetBBox().MoveRight(dxRight);
            occupiedArea.GetBBox().MoveUp(dyUp);
            foreach (IRenderer childRenderer in childRenderers) {
                childRenderer.Move(dxRight, dyUp);
            }
        }

        /// <summary>
        /// Gets all rectangles that this
        /// <see cref="IRenderer"/>
        /// can draw upon in the given area.
        /// </summary>
        /// <param name="area">
        /// a physical area on the
        /// <see cref="DrawContext"/>
        /// </param>
        /// <returns>
        /// a list of
        /// <see cref="iText.Kernel.Geom.Rectangle">rectangles</see>
        /// </returns>
        public virtual IList<Rectangle> InitElementAreas(LayoutArea area) {
            return JavaCollectionsUtil.SingletonList(area.GetBBox());
        }

        /// <summary>
        /// Gets the bounding box that contains all content written to the
        /// <see cref="DrawContext"/>
        /// by this
        /// <see cref="IRenderer"/>
        /// .
        /// </summary>
        /// <returns>
        /// the smallest
        /// <see cref="iText.Kernel.Geom.Rectangle"/>
        /// that surrounds the content
        /// </returns>
        public virtual Rectangle GetOccupiedAreaBBox() {
            return occupiedArea.GetBBox().Clone();
        }

        /// <summary>Gets the border box of a renderer.</summary>
        /// <remarks>
        /// Gets the border box of a renderer.
        /// This is a box used to draw borders.
        /// </remarks>
        /// <returns>border box of a renderer</returns>
        public virtual Rectangle GetBorderAreaBBox() {
            Rectangle rect = GetOccupiedAreaBBox();
            ApplyMargins(rect, false);
            ApplyBorderBox(rect, false);
            return rect;
        }

        public virtual Rectangle GetInnerAreaBBox() {
            Rectangle rect = GetOccupiedAreaBBox();
            ApplyMargins(rect, false);
            ApplyBorderBox(rect, false);
            ApplyPaddings(rect, false);
            return rect;
        }

        protected internal virtual float? RetrieveWidth(float parentBoxWidth) {
            return RetrieveUnitValue(parentBoxWidth, Property.WIDTH);
        }

        protected internal virtual float? RetrieveHeight() {
            return this.GetProperty<float?>(Property.HEIGHT);
        }

        protected internal virtual float? RetrieveUnitValue(float basePercentValue, int property) {
            UnitValue value = this.GetProperty<UnitValue>(property);
            if (value != null) {
                if (value.GetUnitType() == UnitValue.POINT) {
                    return value.GetValue();
                }
                else {
                    if (value.GetUnitType() == UnitValue.PERCENT) {
                        return value.GetValue() * basePercentValue / 100;
                    }
                    else {
                        throw new InvalidOperationException("invalid unit type");
                    }
                }
            }
            else {
                return null;
            }
        }

        //TODO is behavior of copying all properties in split case common to all renderers?
        protected internal virtual IDictionary<int, Object> GetOwnProperties() {
            return properties;
        }

        protected internal virtual void AddAllProperties(IDictionary<int, Object> properties) {
            this.properties.AddAll(properties);
        }

        /// <summary>Gets the first yLine of the nested children recursively.</summary>
        /// <remarks>
        /// Gets the first yLine of the nested children recursively. E.g. for a list, this will be the yLine of the
        /// first item (if the first item is indeed a paragraph).
        /// NOTE: this method will no go further than the first child.
        /// Returns null if there is no text found.
        /// </remarks>
        protected internal virtual float? GetFirstYLineRecursively() {
            if (childRenderers.Count == 0) {
                return null;
            }
            return ((iText.Layout.Renderer.AbstractRenderer)childRenderers[0]).GetFirstYLineRecursively();
        }

        /// <summary>Applies margins of the renderer on the given rectangle</summary>
        /// <param name="rect">a rectangle margins will be applied on.</param>
        /// <param name="reverse">
        /// indicates whether margins will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        /// <seealso cref="GetMargins()"/>
        protected internal virtual Rectangle ApplyMargins(Rectangle rect, bool reverse) {
            return this.ApplyMargins(rect, GetMargins(), reverse);
        }

        /// <summary>Applies given margins on the given rectangle</summary>
        /// <param name="rect">a rectangle margins will be applied on.</param>
        /// <param name="margins">the margins to be applied on the given rectangle</param>
        /// <param name="reverse">
        /// indicates whether margins will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        protected internal virtual Rectangle ApplyMargins(Rectangle rect, float[] margins, bool reverse) {
            if (IsPositioned()) {
                return rect;
            }
            return rect.ApplyMargins<Rectangle>(margins[0], margins[1], margins[2], margins[3], reverse);
        }

        /// <summary>Returns margins of the renderer</summary>
        /// <returns>
        /// a
        /// <see>float[] margins</see>
        /// of the renderer
        /// </returns>
        protected internal virtual float[] GetMargins() {
            return new float[] { (float)this.GetPropertyAsFloat(Property.MARGIN_TOP), (float)this.GetPropertyAsFloat(Property
                .MARGIN_RIGHT), (float)this.GetPropertyAsFloat(Property.MARGIN_BOTTOM), (float)this.GetPropertyAsFloat
                (Property.MARGIN_LEFT) };
        }

        /// <summary>Returns paddings of the renderer</summary>
        /// <returns>
        /// a
        /// <see>float[] paddings</see>
        /// of the renderer
        /// </returns>
        protected internal virtual float[] GetPaddings() {
            return new float[] { (float)this.GetPropertyAsFloat(Property.PADDING_TOP), (float)this.GetPropertyAsFloat(
                Property.PADDING_RIGHT), (float)this.GetPropertyAsFloat(Property.PADDING_BOTTOM), (float)this.GetPropertyAsFloat
                (Property.PADDING_LEFT) };
        }

        /// <summary>Applies paddings of the renderer on the given rectangle</summary>
        /// <param name="rect">a rectangle paddings will be applied on.</param>
        /// <param name="reverse">
        /// indicates whether paddings will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        /// <seealso cref="GetPaddings()"/>
        protected internal virtual Rectangle ApplyPaddings(Rectangle rect, bool reverse) {
            return ApplyPaddings(rect, GetPaddings(), reverse);
        }

        /// <summary>Applies given paddings on the given rectangle</summary>
        /// <param name="rect">a rectangle paddings will be applied on.</param>
        /// <param name="paddings">the paddings to be applied on the given rectangle</param>
        /// <param name="reverse">
        /// indicates whether paddings will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        protected internal virtual Rectangle ApplyPaddings(Rectangle rect, float[] paddings, bool reverse) {
            return rect.ApplyMargins<Rectangle>(paddings[0], paddings[1], paddings[2], paddings[3], reverse);
        }

        /// <summary>
        /// Applies the border box of the renderer on the given rectangle
        /// If the border of a certain side is null, the side will remain as it was.
        /// </summary>
        /// <param name="rect">a rectangle the border box will be applied on.</param>
        /// <param name="reverse">
        /// indicates whether the border box will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        /// <seealso cref="GetBorders()"/>
        protected internal virtual Rectangle ApplyBorderBox(Rectangle rect, bool reverse) {
            Border[] borders = GetBorders();
            return ApplyBorderBox(rect, borders, reverse);
        }

        /// <summary>Applies the given border box (borders) on the given rectangle</summary>
        /// <param name="rect">a rectangle paddings will be applied on.</param>
        /// <param name="borders">
        /// the
        /// <see cref="iText.Layout.Borders.Border">borders</see>
        /// to be applied on the given rectangle
        /// </param>
        /// <param name="reverse">
        /// indicates whether the border box will be applied
        /// inside (in case of false) or outside (in case of false) the rectangle.
        /// </param>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle">border box</see>
        /// of the renderer
        /// </returns>
        protected internal virtual Rectangle ApplyBorderBox(Rectangle rect, Border[] borders, bool reverse) {
            float topWidth = borders[0] != null ? borders[0].GetWidth() : 0;
            float rightWidth = borders[1] != null ? borders[1].GetWidth() : 0;
            float bottomWidth = borders[2] != null ? borders[2].GetWidth() : 0;
            float leftWidth = borders[3] != null ? borders[3].GetWidth() : 0;
            return rect.ApplyMargins<Rectangle>(topWidth, rightWidth, bottomWidth, leftWidth, reverse);
        }

        protected internal virtual void ApplyAbsolutePositioningTranslation(bool reverse) {
            float top = (float)this.GetPropertyAsFloat(Property.TOP);
            float bottom = (float)this.GetPropertyAsFloat(Property.BOTTOM);
            float left = (float)this.GetPropertyAsFloat(Property.LEFT);
            float right = (float)this.GetPropertyAsFloat(Property.RIGHT);
            int reverseMultiplier = reverse ? -1 : 1;
            float dxRight = left != 0 ? left * reverseMultiplier : -right * reverseMultiplier;
            float dyUp = top != 0 ? -top * reverseMultiplier : bottom * reverseMultiplier;
            if (dxRight != 0 || dyUp != 0) {
                Move(dxRight, dyUp);
            }
        }

        protected internal virtual void ApplyDestination(PdfDocument document) {
            String destination = this.GetProperty<String>(Property.DESTINATION);
            if (destination != null) {
                PdfArray array = new PdfArray();
                array.Add(document.GetPage(occupiedArea.GetPageNumber()).GetPdfObject());
                array.Add(PdfName.XYZ);
                array.Add(new PdfNumber(occupiedArea.GetBBox().GetX()));
                array.Add(new PdfNumber(occupiedArea.GetBBox().GetY() + occupiedArea.GetBBox().GetHeight()));
                array.Add(new PdfNumber(1));
                document.AddNamedDestination(destination, ((PdfArray)array.MakeIndirect(document)));
                DeleteProperty(Property.DESTINATION);
            }
        }

        protected internal virtual void ApplyAction(PdfDocument document) {
            PdfAction action = this.GetProperty<PdfAction>(Property.ACTION);
            if (action != null) {
                PdfLinkAnnotation link = new PdfLinkAnnotation(GetOccupiedArea().GetBBox());
                link.SetAction(action);
                Border border = this.GetProperty<Border>(Property.BORDER);
                if (border != null) {
                    link.SetBorder(new PdfArray(new float[] { 0, 0, border.GetWidth() }));
                }
                else {
                    link.SetBorder(new PdfArray(new float[] { 0, 0, 0 }));
                }
                document.GetPage(GetOccupiedArea().GetPageNumber()).AddAnnotation(link);
            }
        }

        [System.ObsoleteAttribute(@"Use IsNotFittingLayoutArea(iText.Layout.Layout.LayoutArea) instead.")]
        protected internal virtual bool IsNotFittingHeight(LayoutArea layoutArea) {
            return IsNotFittingLayoutArea(layoutArea);
        }

        protected internal virtual bool IsNotFittingLayoutArea(LayoutArea layoutArea) {
            Rectangle area = ApplyMargins(layoutArea.GetBBox().Clone(), false);
            area = ApplyPaddings(area, false);
            return !IsPositioned() && (occupiedArea.GetBBox().GetHeight() > area.GetHeight() || occupiedArea.GetBBox()
                .GetWidth() > area.GetWidth());
        }

        /// <summary>Indicates whether the renderer's position is fixed or not.</summary>
        /// <returns>
        /// a
        /// <see>boolean</see>
        /// </returns>
        protected internal virtual bool IsPositioned() {
            return IsFixedLayout();
        }

        /// <summary>Indicates whether the renderer's position is fixed or not.</summary>
        /// <returns>
        /// a
        /// <see>boolean</see>
        /// </returns>
        protected internal virtual bool IsFixedLayout() {
            Object positioning = this.GetProperty<Object>(Property.POSITION);
            return System.Convert.ToInt32(LayoutPosition.FIXED).Equals(positioning);
        }

        protected internal virtual bool IsRelativePosition() {
            int? positioning = this.GetPropertyAsInteger(Property.POSITION);
            return System.Convert.ToInt32(LayoutPosition.RELATIVE).Equals(positioning);
        }

        protected internal virtual bool IsKeepTogether() {
            return true.Equals(GetPropertyAsBoolean(Property.KEEP_TOGETHER));
        }

        protected internal virtual void AlignChildHorizontally(IRenderer childRenderer, float availableWidth) {
            HorizontalAlignment? horizontalAlignment = childRenderer.GetProperty<HorizontalAlignment?>(Property.HORIZONTAL_ALIGNMENT
                );
            if (horizontalAlignment != null && horizontalAlignment != HorizontalAlignment.LEFT) {
                float freeSpace = availableWidth - childRenderer.GetOccupiedArea().GetBBox().GetWidth();
                switch (horizontalAlignment) {
                    case HorizontalAlignment.RIGHT: {
                        childRenderer.Move(freeSpace, 0);
                        break;
                    }

                    case HorizontalAlignment.CENTER: {
                        childRenderer.Move(freeSpace / 2, 0);
                        break;
                    }
                }
            }
        }

        /// <summary>Gets borders of the element in the specified order: top, right, bottom, left.</summary>
        /// <returns>
        /// an array of BorderDrawer objects.
        /// In case when certain border isn't set <code>Property.BORDER</code> is used,
        /// and if <code>Property.BORDER</code> is also not set then <code>null<code/> is returned
        /// on position of this border
        /// </returns>
        protected internal virtual Border[] GetBorders() {
            Border border = this.GetProperty<Border>(Property.BORDER);
            Border topBorder = this.GetProperty<Border>(Property.BORDER_TOP);
            Border rightBorder = this.GetProperty<Border>(Property.BORDER_RIGHT);
            Border bottomBorder = this.GetProperty<Border>(Property.BORDER_BOTTOM);
            Border leftBorder = this.GetProperty<Border>(Property.BORDER_LEFT);
            Border[] borders = new Border[] { topBorder, rightBorder, bottomBorder, leftBorder };
            if (!HasOwnOrModelProperty(Property.BORDER_TOP)) {
                borders[0] = border;
            }
            if (!HasOwnOrModelProperty(Property.BORDER_RIGHT)) {
                borders[1] = border;
            }
            if (!HasOwnOrModelProperty(Property.BORDER_BOTTOM)) {
                borders[2] = border;
            }
            if (!HasOwnOrModelProperty(Property.BORDER_LEFT)) {
                borders[3] = border;
            }
            return borders;
        }

        protected internal virtual iText.Layout.Renderer.AbstractRenderer SetBorders(Border border, int borderNumber
            ) {
            switch (borderNumber) {
                case 0: {
                    SetProperty(Property.BORDER_TOP, border);
                    break;
                }

                case 1: {
                    SetProperty(Property.BORDER_RIGHT, border);
                    break;
                }

                case 2: {
                    SetProperty(Property.BORDER_BOTTOM, border);
                    break;
                }

                case 3: {
                    SetProperty(Property.BORDER_LEFT, border);
                    break;
                }
            }
            return this;
        }

        /// <summary>
        /// Calculates the bounding box of the content in the coordinate system of the pdf entity on which content is placed,
        /// e.g.
        /// </summary>
        /// <remarks>
        /// Calculates the bounding box of the content in the coordinate system of the pdf entity on which content is placed,
        /// e.g. document page or form xObject. This is particularly useful for the cases when element is nested in the rotated
        /// element.
        /// </remarks>
        /// <returns>
        /// a
        /// <see cref="iText.Kernel.Geom.Rectangle"/>
        /// which is a bbox of the content not relative to the parent's layout area but rather to
        /// the some pdf entity coordinate system.
        /// </returns>
        protected internal virtual Rectangle CalculateAbsolutePdfBBox() {
            Rectangle contentBox = GetOccupiedAreaBBox();
            IList<Point> contentBoxPoints = RectangleToPointsList(contentBox);
            iText.Layout.Renderer.AbstractRenderer renderer = this;
            while (renderer.parent != null) {
                if (renderer is BlockRenderer) {
                    float? angle = renderer.GetProperty<float?>(Property.ROTATION_ANGLE);
                    if (angle != null) {
                        BlockRenderer blockRenderer = (BlockRenderer)renderer;
                        AffineTransform rotationTransform = blockRenderer.CreateRotationTransformInsideOccupiedArea();
                        TransformPoints(contentBoxPoints, rotationTransform);
                    }
                }
                renderer = (iText.Layout.Renderer.AbstractRenderer)renderer.parent;
            }
            return CalculateBBox(contentBoxPoints);
        }

        /// <summary>Calculates bounding box around points.</summary>
        /// <param name="points">list of the points calculated bbox will enclose.</param>
        /// <returns>array of float values which denote left, bottom, right, top lines of bbox in this specific order</returns>
        protected internal virtual Rectangle CalculateBBox(IList<Point> points) {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = -double.MaxValue;
            double maxY = -double.MaxValue;
            foreach (Point p in points) {
                minX = Math.Min(p.GetX(), minX);
                minY = Math.Min(p.GetY(), minY);
                maxX = Math.Max(p.GetX(), maxX);
                maxY = Math.Max(p.GetY(), maxY);
            }
            return new Rectangle((float)minX, (float)minY, (float)(maxX - minX), (float)(maxY - minY));
        }

        protected internal virtual IList<Point> RectangleToPointsList(Rectangle rect) {
            IList<Point> points = new List<Point>();
            points.AddAll(iText.IO.Util.JavaUtil.ArraysAsList(new Point(rect.GetLeft(), rect.GetBottom()), new Point(rect
                .GetRight(), rect.GetBottom()), new Point(rect.GetRight(), rect.GetTop()), new Point(rect.GetLeft(), rect
                .GetTop())));
            return points;
        }

        protected internal virtual IList<Point> TransformPoints(IList<Point> points, AffineTransform transform) {
            foreach (Point point in points) {
                transform.Transform(point, point);
            }
            return points;
        }

        public abstract IRenderer GetNextRenderer();

        public abstract LayoutResult Layout(LayoutContext arg1);
    }
}
