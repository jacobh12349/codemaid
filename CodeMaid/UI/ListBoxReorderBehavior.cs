﻿#region CodeMaid is Copyright 2007-2014 Steve Cadwallader.

// CodeMaid is free software: you can redistribute it and/or modify it under the terms of the GNU
// Lesser General Public License version 3 as published by the Free Software Foundation.
//
// CodeMaid is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details <http://www.gnu.org/licenses/>.

#endregion CodeMaid is Copyright 2007-2014 Steve Cadwallader.

using SteveCadwallader.CodeMaid.UI.Enumerations;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace SteveCadwallader.CodeMaid.UI
{
    /// <summary>
    /// A behavior for supporting list box drag and drop reordering, and optionally merging.
    /// </summary>
    public class ListBoxReorderBehavior : Behavior<ListBox>
    {
        #region Fields

        private ListBoxItem _dragCandidate;
        private Point? _dragStartPoint;

        #endregion Fields

        #region Behavior Methods

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AllowDrop = true;
            AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
            AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
            AssociatedObject.PreviewMouseUp += OnPreviewMouseUp;
            AssociatedObject.DragEnter += OnDragEvent;
            AssociatedObject.DragOver += OnDragEvent;
            AssociatedObject.DragLeave += OnDragLeave;
            AssociatedObject.Drop += OnDrop;
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has
        /// actually occurred.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.AllowDrop = false;
                AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
                AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
                AssociatedObject.PreviewMouseUp -= OnPreviewMouseUp;
                AssociatedObject.DragEnter -= OnDragEvent;
                AssociatedObject.DragOver -= OnDragEvent;
                AssociatedObject.DragLeave -= OnDragLeave;
                AssociatedObject.Drop -= OnDrop;
            }
        }

        #endregion Behavior Methods

        #region CanMerge (Dependency Property)

        /// <summary>
        /// The dependency property definition for the CanMerge property.
        /// </summary>
        public static readonly DependencyProperty CanMergeProperty = DependencyProperty.Register(
            "CanMerge", typeof(bool), typeof(ListBoxReorderBehavior));

        /// <summary>
        /// Gets or sets the flag indicating if items can be merged.
        /// </summary>
        public bool CanMerge
        {
            get { return (bool)GetValue(CanMergeProperty); }
            set { SetValue(CanMergeProperty, value); }
        }

        #endregion CanMerge (Dependency Property)

        #region Event Handlers

        /// <summary>
        /// Called when a PreviewMouseDown event is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs" /> instance containing the event data.
        /// </param>
        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _dragCandidate = FindParentListBoxItem(e.OriginalSource);
                _dragStartPoint = e.GetPosition(null);
            }
        }

        /// <summary>
        /// Called when a PreviewMouseMove event is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragCandidate == null || !_dragStartPoint.HasValue) return;

            var delta = _dragStartPoint.Value - e.GetPosition(null);
            if (Math.Abs(delta.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) <= SystemParameters.MinimumVerticalDragDistance) return;

            _dragCandidate.SetValue(DragDropAttachedProperties.IsBeingDraggedProperty, true);

            DragDrop.DoDragDrop(_dragCandidate, new DataObject(typeof(object), _dragCandidate.DataContext), DragDropEffects.Move);

            _dragCandidate.SetValue(DragDropAttachedProperties.IsBeingDraggedProperty, false);

            _dragCandidate = null;
            _dragStartPoint = null;
        }

        /// <summary>
        /// Called when a PreviewMouseUp event is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs" /> instance containing the event data.
        /// </param>
        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragCandidate = null;
            _dragStartPoint = null;
        }

        /// <summary>
        /// Called when a drag event (DragEnter, DragOver) is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void OnDragEvent(object sender, DragEventArgs e)
        {
            var target = FindParentListBoxItem(e.OriginalSource);
            if (target != null && e.Data.GetDataPresent(typeof(object)))
            {
                var sourceData = e.Data.GetData(typeof(object));
                var targetData = target.DataContext;

                if (sourceData != null && targetData != null && sourceData != targetData)
                {
                    switch (GetDropPostion(e, target))
                    {
                        case DropPosition.Above:
                            target.SetValue(DragDropAttachedProperties.IsDropAboveTargetProperty, true);
                            target.SetValue(DragDropAttachedProperties.IsDropBelowTargetProperty, false);
                            target.SetValue(DragDropAttachedProperties.IsDropOnTargetProperty, false);
                            break;

                        case DropPosition.Below:
                            target.SetValue(DragDropAttachedProperties.IsDropAboveTargetProperty, false);
                            target.SetValue(DragDropAttachedProperties.IsDropBelowTargetProperty, true);
                            target.SetValue(DragDropAttachedProperties.IsDropOnTargetProperty, false);
                            break;

                        case DropPosition.On:
                            target.SetValue(DragDropAttachedProperties.IsDropAboveTargetProperty, false);
                            target.SetValue(DragDropAttachedProperties.IsDropBelowTargetProperty, false);
                            target.SetValue(DragDropAttachedProperties.IsDropOnTargetProperty, true);
                            break;
                    }

                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                    return;
                }
            }

            // Not a valid drop target.
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Called when a DragLeave event is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            var target = FindParentListBoxItem(e.OriginalSource);
            if (target != null)
            {
                target.SetValue(DragDropAttachedProperties.IsDropAboveTargetProperty, false);
                target.SetValue(DragDropAttachedProperties.IsDropBelowTargetProperty, false);
                target.SetValue(DragDropAttachedProperties.IsDropOnTargetProperty, false);
            }
        }

        /// <summary>
        /// Called when a Drop event is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(object))) return;

            var target = FindParentListBoxItem(e.OriginalSource);
            if (target == null) return;

            var sourceData = e.Data.GetData(typeof(object));
            var targetData = target.DataContext;

            if (sourceData == null || targetData == null || sourceData == targetData) return;

            var collection = AssociatedObject.ItemsSource as ObservableCollection<object>;
            if (collection == null) return;

            var sourceIndex = collection.IndexOf(sourceData);
            var targetIndex = collection.IndexOf(targetData);

            switch (GetDropPostion(e, target))
            {
                case DropPosition.Above:
                    MoveTargetAboveSource(collection, sourceIndex, targetIndex);
                    break;

                case DropPosition.Below:
                    MoveTargetBelowSource(collection, sourceIndex, targetIndex);
                    break;

                case DropPosition.On:
                    MergeTargetIntoSource(collection, sourceIndex, targetIndex);
                    break;
            }

            target.SetValue(DragDropAttachedProperties.IsDropAboveTargetProperty, false);
            target.SetValue(DragDropAttachedProperties.IsDropBelowTargetProperty, false);
            target.SetValue(DragDropAttachedProperties.IsDropOnTargetProperty, false);
            e.Handled = true;
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Attempts to find the parent ListBoxItem from the specified event source.
        /// </summary>
        /// <param name="eventSource">The event source.</param>
        /// <returns>The parent ListBoxItem, otherwise null.</returns>
        private static ListBoxItem FindParentListBoxItem(object eventSource)
        {
            var source = eventSource as DependencyObject;
            if (source == null) return null;

            var listBoxItem = source.FindVisualAncestor<ListBoxItem>();

            return listBoxItem;
        }

        /// <summary>
        /// Determines the drop position for the specified drag event and the drop target.
        /// </summary>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        /// <param name="target">The target.</param>
        /// <returns>The drop position.</returns>
        private DropPosition GetDropPostion(DragEventArgs e, ListBoxItem target)
        {
            var dropPoint = e.GetPosition(target);
            var targetHeight = target.ActualHeight;

            if (CanMerge)
            {
                bool isTopThird = dropPoint.Y <= targetHeight / 3;
                bool isBottomThird = dropPoint.Y > targetHeight * 2 / 3;

                return isTopThird ? DropPosition.Above : (isBottomThird ? DropPosition.Below : DropPosition.On);
            }

            bool isTopHalf = dropPoint.Y <= targetHeight / 2;

            return isTopHalf ? DropPosition.Above : DropPosition.Below;
        }

        /// <summary>
        /// Moves the item at the target index above the item at the source index within the specified collection.
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="targetIndex">The target index.</param>
        private void MoveTargetAboveSource(ObservableCollection<object> collection, int sourceIndex, int targetIndex)
        {
            // If the source is in front of the target, offset the target by 1 as the indices will change then the source is removed.
            if (sourceIndex < targetIndex)
            {
                targetIndex--;
            }

            collection.Move(sourceIndex, targetIndex);
        }

        /// <summary>
        /// Moves the item at the target index below the item at the source index within the specified collection.
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="targetIndex">The target index.</param>
        private void MoveTargetBelowSource(ObservableCollection<object> collection, int sourceIndex, int targetIndex)
        {
            // Increase target index by 1 to go after the specified target.
            targetIndex++;

            // If the source is in front of the target, offset the target by 1 as the indices will change then the source is removed.
            if (sourceIndex < targetIndex)
            {
                targetIndex--;
            }

            collection.Move(sourceIndex, targetIndex);
        }

        /// <summary>
        /// Merges the item at the target index into the item at the source index within the specified collection.
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="targetIndex">The target index.</param>
        private void MergeTargetIntoSource(ObservableCollection<object> collection, int sourceIndex, int targetIndex)
        {
        }

        #endregion Methods
    }
}