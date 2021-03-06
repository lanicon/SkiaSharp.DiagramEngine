﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;
using SynodeTechnologies.SkiaSharp.DiagramEngine.Controls;
using SynodeTechnologies.SkiaSharp.DiagramEngine.Helpers;
using Xamarin.Forms;

namespace SynodeTechnologies.SkiaSharp.DiagramEngine.Layouts
{
    public class HierarchicalTree : HierarchicalBase
    {
        public static readonly BindableProperty HorizontalSpacingProperty = BindableProperty.Create(nameof(HorizontalSpacing), typeof(float), typeof(HierarchicalTree), 6.0f);

        public float HorizontalSpacing
        {
            get
            {
                return (float)this.GetValue(HorizontalSpacingProperty);
            }
            set
            {
                this.SetValue(HorizontalSpacingProperty, value);
            }
        }

        public static readonly BindableProperty VerticalSpacingProperty = BindableProperty.Create(nameof(VerticalSpacing), typeof(float), typeof(HierarchicalTree), 6.0f);

        public float VerticalSpacing
        {
            get
            {
                return (float)this.GetValue(VerticalSpacingProperty);
            }
            set
            {
                this.SetValue(VerticalSpacingProperty, value);
            }
        }

        private Dictionary<int, float> heightByLevel = new Dictionary<int, float>();

        private SKSize totalSize;
        public override SKSize Measure(IList<HierarchicalNode> elements, SKSize availableSize)
        {
            heightByLevel.Clear();
            float maxWidth = 0;
            float maxHeight = 0;
            List<HierarchicalNode> allreadyPassed = new List<HierarchicalNode>();
            foreach (var group in elements.GroupBy(n => n.Level))
            {
                maxWidth = Measure(group.Key, group, maxWidth, availableSize,allreadyPassed);
                float maxHeightByLevel = 0;
                foreach(var element in group)
                {
                    maxHeightByLevel = Math.Max(maxHeightByLevel, element.DesiredSize.Height);
                }
                heightByLevel.Add(group.Key, maxHeightByLevel);
                maxHeight += maxHeightByLevel + VerticalSpacing;
            }
            totalSize = new SKSize(maxWidth - HorizontalSpacing, maxHeight - VerticalSpacing);
            return totalSize;
        }

        public float Measure(int level, IEnumerable<HierarchicalNode> elements,float offsetx, SKSize availableSize,List<HierarchicalNode> allreadyPassed)
        {
            foreach (var element in elements)
            {
                if (allreadyPassed.Contains(element) == false)
                {
                    allreadyPassed.Add(element);
                    element.Measure(availableSize);
                    bool withChild = element.ChildrenNode.Count > 0;
                    if (withChild)
                    {
                        float startOffset = offsetx;
                        float endOffset = Measure(level + 1, element.ChildrenNode,startOffset, availableSize,allreadyPassed);
                        if (FloatUtil.AreClose(endOffset, startOffset))
                        {
                            withChild = false;
                        }
                        else
                        {
                            offsetx = endOffset;
                            element.Bag.ChildrenWidth = endOffset - startOffset;
                        }
                    }
                    else
                    {
                        float elementWidth = element.DesiredSize.Width;
                        offsetx += elementWidth + HorizontalSpacing;
                    }
                }
            }
            return offsetx;
        }

        public override void Arrange(IList<HierarchicalNode> elements, SKRect bounds)
        {
            float widthRatio = 1.0f;
            float heightRatio = 1.0f;

            if (bounds.Width < totalSize.Width)
            {
                widthRatio = bounds.Width / totalSize.Width;
            }
            if(bounds.Height < totalSize.Height)
            {
                heightRatio = bounds.Height / totalSize.Height;
            }

            float offsetX = bounds.Left;
            float offsetY = bounds.Top;
            List<HierarchicalNode> allreadyPassed = new List<HierarchicalNode>();
            foreach (var group in elements.GroupBy(n => n.Level))
            {
                offsetX = Arrange(group.Key, group, offsetY, offsetX, widthRatio, heightRatio, allreadyPassed);
            }
        }

        public float Arrange(int level, IEnumerable<HierarchicalNode> elements,float offsety, float offsetx, float widthRatio, float heightRatio,List<HierarchicalNode> allreadyPassed)
        {
            float levelHeight = heightByLevel[level] * heightRatio;
            foreach (var element in elements)
            {
                if (allreadyPassed.Contains(element) == false)
                {
                    allreadyPassed.Add(element);
                    float elementWidth = (element.DesiredSize.Width * widthRatio);
                    float elementHeight = element.DesiredSize.Height * heightRatio;
                    float elementOffsetY = offsety + ((levelHeight - elementHeight) / 2.0f);
                    bool withChild = element.ChildrenNode.Count > 0;
                    if (withChild)
                    {
                        float childrenWith = element.Bag.ChildrenWidth * widthRatio;
                        float startOffset = offsetx;
                        if(childrenWith < elementWidth)
                        {
                            startOffset += (elementWidth - childrenWith) / 2.0f;
                        }
                        float endOffset = Arrange(level + 1, element.ChildrenNode, offsety + levelHeight + VerticalSpacing, startOffset, widthRatio, heightRatio, allreadyPassed);
                        if (FloatUtil.AreClose(endOffset, startOffset))
                        {
                            withChild = false;
                        }
                        else
                        {
                            float elementOffsetX = startOffset + ((endOffset - startOffset - HorizontalSpacing) / 2.0f) - (elementWidth / 2.0f);
                            SKRect elementBounds = new SKRect(elementOffsetX, elementOffsetY, elementOffsetX + elementWidth, elementOffsetY + elementHeight);
                            element.Arrange(elementBounds);
                            offsetx = endOffset;
                        }
                    }
                    else
                    {
                        SKRect elementBounds = new SKRect(offsetx, elementOffsetY, offsetx + elementWidth, elementOffsetY + elementHeight);
                        element.Arrange(elementBounds);
                        offsetx += elementWidth + HorizontalSpacing;
                    }
                }
            }
            return offsetx;
        }

        public override void Render(IList<HierarchicalNode> elements, SKRect bounds, SKCanvas canvas)
        {
            SKPaint p = new SKPaint();
            p.Color = SKColors.Black;
            p.StrokeWidth = 1;
            foreach (var element in elements)
            {
                foreach(var child in element.ChildrenNode)
                {
                    var midWidthElem = element.Bounds.Left + (element.Bounds.Width / 2.0f);
                    var midWidthChild = child.Bounds.Left + (child.Bounds.Width / 2.0f);
                    var topHeight = element.Bounds.Bottom;
                    var midHeight = element.Bounds.Bottom +(VerticalSpacing /2.0f);
                    var bottomHeight = child.Bounds.Top;

                    canvas.DrawLine(midWidthElem,
                        topHeight,
                        midWidthElem,
                        midHeight,
                        p);

                    canvas.DrawLine(midWidthChild,
                       midHeight,
                       midWidthElem,
                       midHeight,
                       p);


                    canvas.DrawLine(midWidthChild,
                        bottomHeight,
                        midWidthChild,
                        midHeight,
                        p);
                }
            }
            base.Render(elements, bounds, canvas);
        }
    }
}
