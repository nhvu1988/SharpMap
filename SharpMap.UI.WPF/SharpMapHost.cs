// Copyright 2014-, SharpMapTeam
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using GeoAPI.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Forms;
using SharpMap.Forms.Tools;
using SharpMap.Layers;
using SharpMap.Rendering.Decoration;
using SharpMap.Rendering.Decoration.ScaleBar;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace SharpMap.UI.WPF
{
	/// <summary>
	/// Extends WindowsFormsHost and encapsulates SharpMap specific code.
	/// </summary>
	public class SharpMapHost : WindowsFormsHost, INotifyPropertyChanged
	{
		// Dependency Property to store MapLayers.
		public static readonly DependencyProperty MapLayersProperty =
			DependencyProperty.Register("MapLayers", typeof(ObservableCollection<ILayer>), typeof(SharpMapHost),
				new PropertyMetadata(SetMapLayersCallback));

		// Dependency Property to store BackgroundLayer.
		public static readonly DependencyProperty BackgroundLayerProperty =
			DependencyProperty.Register("BackgroundLayer", typeof(ILayer), typeof(SharpMapHost),
				new PropertyMetadata(SetBackgroundLayerCallback));

		// Dependency Property to store ActiveTool.
		public static readonly DependencyProperty ActiveToolProperty =
			DependencyProperty.Register("ActiveTool", typeof(MapBox.Tools), typeof(SharpMapHost),
				new PropertyMetadata(SetActiveToolCallback));

		// Dependency Property to store MaxExtent.
		public static readonly DependencyProperty MaxExtentProperty =
			DependencyProperty.Register("MaxExtent", typeof(Envelope), typeof(SharpMapHost),
				new PropertyMetadata(SetMaxExtentCallback));

		// Dependency Property to store MaxCenter.
		public static readonly DependencyProperty MapCenterProperty =
			DependencyProperty.Register("MapCenter", typeof(Coordinate), typeof(SharpMapHost),
				new PropertyMetadata(SetMapCenterCallback));

		// Dependency Property to store MapMaxZoom.
		public static readonly DependencyProperty MapMaxZoomProperty =
			DependencyProperty.Register("MapMaxZoom", typeof(double), typeof(SharpMapHost), new PropertyMetadata(SetMapMaxZoomCallback));

		// Dependency Property to store MapMaxZoom.
		public static readonly DependencyProperty MapMinZoomProperty =
			DependencyProperty.Register("MapMinZoom", typeof(double), typeof(SharpMapHost), new PropertyMetadata(SetMapMinZoomCallback));

		// Dependency Property to store MapZoom.
		public static readonly DependencyProperty MapZoomProperty =
			DependencyProperty.Register("MapZoom", typeof(double), typeof(SharpMapHost), new PropertyMetadata(SetMapZoomCallback));

		// Dependency Property to store CurrentMouseCoordinate.
		public static readonly DependencyProperty CurrentMouseCoordinateProperty =
			DependencyProperty.Register("CurrentMouseCoordinate", typeof(Coordinate), typeof(SharpMapHost));

		// Dependency Property to store MapExtent.
		public static readonly DependencyProperty MapExtentProperty =
			DependencyProperty.Register("MapExtent", typeof(Envelope), typeof(SharpMapHost),
				new PropertyMetadata(SetMapExtentCallback));

		// Dependency Property to store MapExtent.
		public static readonly DependencyProperty MapSRIDProperty =
			DependencyProperty.Register("MapSRID", typeof(int), typeof(SharpMapHost),
				new PropertyMetadata(SetMapSRIDCallback));

		// Dependency Property to store MapCustomTool.
		public static readonly DependencyProperty MapCustomToolProperty =
			DependencyProperty.Register("MapCustomTool", typeof(MapTool), typeof(SharpMapHost),
				new PropertyMetadata(SetMapCustomToolCallback));

		// Dependency Property used when a new geometry is defined.
		public static readonly DependencyProperty DefinedGeometryProperty =
			DependencyProperty.Register("DefinedGeometry", typeof(IGeometry), typeof(SharpMapHost),
				new PropertyMetadata(GeometryDefinedCallback));

		// Dependency Property used when right click in a MapFeature.
		public static readonly DependencyProperty FeatureRightClickedCommandProperty =
			DependencyProperty.Register("FeatureRightClickedCommand", typeof(ICommand), typeof(SharpMapHost));

		// Dependency Property used when left click on map.
		public static readonly DependencyProperty OnMouseClickedCommandProperty =
			DependencyProperty.Register("OnMouseClickedCommand", typeof(ICommand), typeof(SharpMapHost));

		// Dependency Property used when double click on map.
		public static readonly DependencyProperty OnMouseDoubleClickedCommandProperty =
			DependencyProperty.Register("OnMouseDoubleClickedCommand", typeof(ICommand), typeof(SharpMapHost));

		// Dependency Property to store IsMapRendering.
		public static readonly DependencyProperty IsMapRenderingProperty =
			DependencyProperty.Register("IsMapRendering", typeof(bool), typeof(SharpMapHost));

		// Dependency Property to store IsMapVisible.
		public static readonly DependencyProperty IsMapVisibleProperty =
			DependencyProperty.Register("IsMapVisible", typeof(bool), typeof(SharpMapHost));

		// Dependency Property to store IsMapVisible.
		public static readonly DependencyProperty IsMouseDownProperty =
			DependencyProperty.Register("IsMouseDown", typeof(bool), typeof(SharpMapHost));

		public static readonly DependencyProperty FinishDrawingGeometryEventProperty =
			DependencyProperty.Register("FinishDrawingGeometryEvent", typeof(bool), typeof(SharpMapHost), new PropertyMetadata(FinishDrawingGeometryEventCalled));

		public static readonly DependencyProperty UndoDrawingGeometryEventProperty =
			DependencyProperty.Register("UndoDrawingGeometryEvent", typeof(bool), typeof(SharpMapHost), new PropertyMetadata(UndoDrawingGeometryEventCalled));

		public static readonly DependencyProperty CancelDrawingGeometryEventProperty =
			DependencyProperty.Register("CancelDrawingGeometryEvent", typeof(bool), typeof(SharpMapHost), new PropertyMetadata(CancelDrawingGeometryEventCalled));

		private readonly MapBox _mapBox;

		private VectorLayer _editLayer;

		private GeometryProvider _editLayerGeoProvider;

		//private Coordinate CurrentMouseCoordinate;

		/// <summary>
		/// Initializes a new instance of the <see cref="SharpMapHost"/> class. 
		/// </summary>
		public SharpMapHost()
		{
			_mapBox = new MapBox
			{
				BackColor = Color.White,
				Map = {SRID = 900913},
				PreviewMode = MapBox.PreviewModes.Fast
			};
			Child = _mapBox;

			var scaleBar = new ScaleBar
			{
				Anchor = MapDecorationAnchor.LeftBottom
			};
			_mapBox.Map.Decorations.Add(scaleBar);
			_mapBox.PanOnClick = false;

			_mapBox.MouseMove += MapBoxOnMouseMove;
			_mapBox.GeometryDefined += MapBoxOnGeometryDefined;
			_mapBox.MapZoomChanged += MapBoxOnMapZoomChanged;
			_mapBox.MapCenterChanged += MapBoxOnMapCenterChanged;
			_mapBox.MouseUp += MapBoxOnMouseUp;
			_mapBox.MouseDown += MapBoxOnMouseDown;
			_mapBox.MouseDoubleClick += MapBoxOnMouseDoubleClick;
			_mapBox.MapRefreshing += MapBoxOnMapRefreshing;
			_mapBox.MapRefreshed += MapBoxOnMapRefreshed;
			_mapBox.MapExtentChanged += MapBoxOnMapExtentChanged;
			_mapBox.ActiveToolChanged += MapBoxOnActiveToolChanged;

			IsVisibleChanged += OnIsVisibleChanged;
			KeyDown += OnKeyDown;
		}

		private void MapBoxOnMapExtentChanged(Envelope extent)
		{
			MapExtent = extent;
		}

		private void MapBoxOnMapCenterChanged(Coordinate center)
		{
			MapCenter = center;
		}

		private void MapBoxOnActiveToolChanged(MapBox.Tools tool)
		{
			ActiveTool = tool;
		}

		private void MapBoxOnMouseDoubleClick(object sender, MouseEventArgs mouseEventArgs)
		{
			OnMouseDoubleClickedCommand?.Execute(CurrentMouseCoordinate);
		}

		private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			IsMapVisible = (bool) args.NewValue;
		}

		private void MapBoxOnMapZoomChanged(double zoom)
		{
			MapZoom = zoom;
		}

		private void MapBoxOnGeometryDefined(IGeometry geometry)
		{
			DefinedGeometry = geometry;
		}

		private void MapBoxOnMapRefreshed(object sender, EventArgs eventArgs)
		{
			if (Dispatcher.CheckAccess())
			{
				// This thread has access so it can update the UI thread.
				IsMapRendering = false;
			}
			else
			{
				// This thread does not have access to the UI thread.
				// Place the update method on the Dispatcher of the UI thread.
				Dispatcher.Invoke(new Action(() =>
				{
					IsMapRendering = false;
				}));
			}
		}

		private void MapBoxOnMapRefreshing(object sender, EventArgs eventArgs)
		{
			IsMapRendering = true;
		}

		protected override void Dispose(bool disposing)
		{
			_mapBox.MouseMove -= MapBoxOnMouseMove;
			_mapBox.GeometryDefined -= MapBoxOnGeometryDefined;
			_mapBox.MapZoomChanged -= MapBoxOnMapZoomChanged;
			_mapBox.MapCenterChanged -= MapBoxOnMapCenterChanged;
			_mapBox.MouseUp -= MapBoxOnMouseUp;
			_mapBox.MouseDown -= MapBoxOnMouseDown;
			_mapBox.MouseDoubleClick -= MapBoxOnMouseDoubleClick;
			_mapBox.MapRefreshing -= MapBoxOnMapRefreshing;
			_mapBox.MapRefreshed -= MapBoxOnMapRefreshed;
			_mapBox.MapExtentChanged += MapBoxOnMapExtentChanged;
			_mapBox.ActiveToolChanged -= MapBoxOnActiveToolChanged;
			_mapBox.Dispose();

			KeyDown -= OnKeyDown;
			IsVisibleChanged -= OnIsVisibleChanged;
			base.Dispose(disposing);
		}

		private readonly int[] _mouseDownPosition = {0, 0};

		private void MapBoxOnMouseDown(Coordinate worldPos, MouseEventArgs mousePos)
		{
			_mouseDownPosition[0] = mousePos.X;
			_mouseDownPosition[1] = mousePos.Y;
			IsMouseDown = true;
		}

		private void MapBoxOnMouseUp(Coordinate worldPos, MouseEventArgs args)
		{
			// check position on mouse down and on mouse up
			if (_mouseDownPosition[0] == args.X && _mouseDownPosition[1] == args.Y)
				OnMouseClickedCommand?.Execute(worldPos);
			IsMouseDown = false;
		}

		public ObservableCollection<ILayer> MapLayers
		{
			get { return (ObservableCollection<ILayer>)GetValue(MapLayersProperty); }
			set { SetValue(MapLayersProperty, value); }
		}

		public ILayer BackgroundLayer
		{
			get { return (ILayer)GetValue(BackgroundLayerProperty); }
			set { SetValue(BackgroundLayerProperty, value); }
		}

		public MapBox.Tools ActiveTool
		{
			get { return (MapBox.Tools)GetValue(ActiveToolProperty); }
			set { SetValue(ActiveToolProperty, value); }
		}

		public string CurrentMouseCoordinateString
		{
			get
			{
				return CurrentMouseCoordinate != null
					? string.Format("{0:0}, {1:0}", CurrentMouseCoordinate.X, CurrentMouseCoordinate.Y)
					: "";
			}
		}

		public Coordinate CurrentMouseCoordinate
		{
			get { return (Coordinate)GetValue(CurrentMouseCoordinateProperty); }
			set { SetValue(CurrentMouseCoordinateProperty, value); }
		}

		public Envelope MaxExtent
		{
			get { return (Envelope)GetValue(MaxExtentProperty); }
			set { SetValue(MaxExtentProperty, value); }
		}

		public Envelope MapExtent
		{
			get { return _mapBox.Map.Envelope; }
			set { SetValue(MapExtentProperty, value); }
		}

		public Coordinate MapCenter
		{
			get { return _mapBox.Map.Center; }
			set { SetValue(MapCenterProperty, value); }
		}

		public double MapMaxZoom
		{
			get { return _mapBox.Map.MaximumZoom; }
			set { SetValue(MapMaxZoomProperty, value); }
		}

		public double MapMinZoom
		{
			get { return _mapBox.Map.MinimumZoom; }
			set { SetValue(MapMinZoomProperty, value); }
		}

		public double MapZoom
		{
			get { return _mapBox.Map.Zoom; }
			set { SetValue(MapZoomProperty, value); }
		}

		public int MapSRID
		{
			get { return _mapBox.Map.SRID; }
			set { SetValue(MapSRIDProperty, value); }
		}

		public IMapTool MapCustomTool
		{
			get { return _mapBox.CustomTool; }
			set { SetValue(MapCustomToolProperty, value); }
		}

		public IGeometry DefinedGeometry
		{
			get { return (IGeometry)GetValue(DefinedGeometryProperty); }

			set { SetValue(DefinedGeometryProperty, value); }
		}

		/// <summary>
		/// The command that is invoked when a feature is right clicked
		/// </summary>
		public ICommand FeatureRightClickedCommand
		{
			get { return GetValue(FeatureRightClickedCommandProperty) as ICommand; }

			set { SetValue(FeatureRightClickedCommandProperty, value); }
		}

		public ICommand OnMouseClickedCommand
		{
			get { return GetValue(OnMouseClickedCommandProperty) as ICommand ; }
			set { SetValue(OnMouseClickedCommandProperty, value); }
		}

		public ICommand OnMouseDoubleClickedCommand
		{
			get { return GetValue(OnMouseDoubleClickedCommandProperty) as ICommand; }
			set { SetValue(OnMouseDoubleClickedCommandProperty, value); }
		}

		public bool IsMapRendering
		{
			get { return (bool) GetValue(IsMapRenderingProperty); }
			set
			{
				SetValue(IsMapRenderingProperty, value);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsMapRendering"));
			}
		}

		public bool IsMapVisible
		{
			get { return (bool)GetValue(IsMapVisibleProperty); }
			set { SetValue(IsMapVisibleProperty, value); }
		}

		public bool IsMouseDown
		{
			get { return (bool)GetValue(IsMouseDownProperty); }
			set { SetValue(IsMouseDownProperty, value); }
		}

		/// <summary>
		/// Gets called when changes on MapLayers
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetMapLayersCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			var oldLayers = args.OldValue as ObservableCollection<ILayer>;
			if (oldLayers != null)
			{
				oldLayers.CollectionChanged -= host.OnMapLayerChanged;
				foreach (var layer in oldLayers.Where(l => host._mapBox.Map.Layers.Contains(l)))
				{
					host._mapBox.Map.Layers.Remove(layer);
				}
			}

			var newLayers = args.NewValue as ObservableCollection<ILayer>;
			if (newLayers != null)
			{
				newLayers.CollectionChanged += host.OnMapLayerChanged;
				foreach (var layer in newLayers.Where(l => !host._mapBox.Map.Layers.Contains(l)))
				{
					host._mapBox.Map.Layers.Add(layer);
				}
				
			}
		}

		/// <summary>
		/// Gets called when changes on BackgroundLayer
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetBackgroundLayerCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var mapBox = host._mapBox;
			mapBox.Map.BackgroundLayer.Clear();

			var layer = args.NewValue as ILayer;
			if (layer != null)
			{
				mapBox.Map.BackgroundLayer.Add(layer);
			}

			mapBox.Refresh();
		}

		/// <summary>
		/// Gets called when changes on ActiveTool
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetActiveToolCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			var mapBox = host._mapBox;
			var newTool = (MapBox.Tools)args.NewValue;
			mapBox.ActiveTool = newTool;
		}

		/// <summary>
		/// Gets called when changes on MaxExtent
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetMaxExtentCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var mapBox = host._mapBox;
			var extent = (Envelope)args.NewValue;
			if (extent == null || Equals(mapBox.Map.MaximumExtents, extent))
				return;

			mapBox.Map.EnforceMaximumExtents = true;
			mapBox.Map.MaximumExtents = extent;
		}

		/// <summary>
		/// Gets called when changes on Center
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetMapCenterCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var center = (Coordinate)args.NewValue;
			if (center == null || Equals(center, host._mapBox.Map.Center))
				return;

			host._mapBox.Map.Center = center;
			host._mapBox.Refresh();
		}

		/// <summary>
		/// Gets called when changes on MapExtent
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void SetMapExtentCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var mapBox = host._mapBox;
			var extent = (Envelope)args.NewValue;
			if (Equals(host._mapBox.Map.Envelope, extent))
				return;
			mapBox.Map.ZoomToBox(extent);
			mapBox.Refresh();
		}

		private static void SetMapSRIDCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var srId = (int)args.NewValue;
			var mapBox = host._mapBox;
			if (mapBox.Map.SRID == srId)
				return;

			mapBox.Map.SRID = srId;
			mapBox.Refresh();
		}

		private static void SetMapCustomToolCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
				return;

			var customTool = (MapTool)args.NewValue;
			var mapBox = host._mapBox;
			if (mapBox.CustomTool == customTool)
				return;

			mapBox.CustomTool = customTool;
		}

		private static void SetMapMaxZoomCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			var maxZoom = (double)args.NewValue;
			var mapBox = host._mapBox;
			if (Math.Abs(mapBox.Map.MaximumZoom - maxZoom) < 0.0001)
				return;

			mapBox.Map.MaximumZoom = maxZoom;
			mapBox.Refresh();
		}

		private static void SetMapMinZoomCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			var minZoom = (double)args.NewValue;
			var mapBox = host._mapBox;
			if (Math.Abs(mapBox.Map.MinimumZoom - minZoom) < 0.0001)
				return;

			mapBox.Map.MinimumZoom = minZoom;
			mapBox.Refresh();
		}

		private static void SetMapZoomCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			var zoom = (double)args.NewValue;
			var mapBox = host._mapBox;
			if (Math.Abs(mapBox.Map.Zoom - zoom) < 0.0001)
				return;

			mapBox.Map.Zoom = zoom;
			mapBox.Refresh();
		}

		/// <summary>
		/// Gets called when changes on GeometryDefined
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="args">The event arguments</param>
		private static void GeometryDefinedCallback(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			if (host == null)
			{
				return;
			}

			if (host._editLayer == null)
			{
				host._editLayer = new VectorLayer("EditLayer");
				host._editLayerGeoProvider = new GeometryProvider(new List<IGeometry>());
				host._editLayer.DataSource = host._editLayerGeoProvider;
				host.MapLayers.Add(host._editLayer);
			}

			host._editLayerGeoProvider.Geometries.Clear();
			var geom = (IGeometry)args.NewValue;
			if (geom != null)
			{
				host._editLayerGeoProvider.Geometries.Add(geom);
			}

			host._mapBox.Refresh();
		}

		public bool FinishDrawingGeometryEvent { get; set; }

		private static void FinishDrawingGeometryEventCalled(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			host?._mapBox.FinishDrawingPolygon();
		}

		public bool UndoDrawingGeometryEvent { get; set; }

		private static void UndoDrawingGeometryEventCalled(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			host?._mapBox.UndoDrawingPolygon();
		}

		public bool CancelDrawingGeometryEvent { get; set; }

		private static void CancelDrawingGeometryEventCalled(object sender, DependencyPropertyChangedEventArgs args)
		{
			var host = sender as SharpMapHost;
			host?._mapBox.CancelDrawingPolygon();
		}

		/// <summary>
		/// Gets called when keyboard key pressed. Pans the map according to arrow keys.
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="keyEventArgs">The event arguments</param>
		private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
		{
			var currentEnvelope = _mapBox.Map.Envelope;
			var dX = currentEnvelope.Width / 2;
			var dY = currentEnvelope.Height / 2;

			var x = _mapBox.Map.Center.X;
			var y = _mapBox.Map.Center.Y;

			switch (keyEventArgs.Key)
			{
				case Key.Left:
					x -= dX;
					keyEventArgs.Handled = true;
					break;
				case Key.Right:
					x += dX;
					keyEventArgs.Handled = true;
					break;
				case Key.Up:
					y += dY;
					keyEventArgs.Handled = true;
					break;
				case Key.Down:
					y -= dY;
					keyEventArgs.Handled = true;
					break;
			}

			_mapBox.Map.Center = new Coordinate(x, y);
			_mapBox.Refresh();
		}

		/// <summary>
		/// Gets called when changes in MapLayers
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void OnMapLayerChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						var layers = e.NewItems.Cast<ILayer>();
						foreach (var layer in layers.Where(layer => !_mapBox.Map.Layers.Contains(layer)))
						{
							_mapBox.Map.Layers.Add(layer);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var layers = e.OldItems.Cast<ILayer>();
						foreach (var layer in layers.Where(layer => _mapBox.Map.Layers.Contains(layer)))
						{
							_mapBox.Map.Layers.Remove(layer);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					_mapBox.Map.Layers.Clear();
					break;
			}

			_mapBox.Refresh();
		}

		public void ZoomToExtents()
		{
			_mapBox.Map.ZoomToExtents();
			_mapBox.Refresh();
		}

		public void ZoomToEnvelope(Envelope env)
		{
			_mapBox.Map.ZoomToBox(env);
			_mapBox.Refresh();
		}

		/// <summary>
		/// Gets called when mouse moves over map
		/// </summary>
		/// <param name="worldPos">The click coordinate</param>
		/// <param name="mouseEventArgs">The event arguments</param>
		private void MapBoxOnMouseMove(Coordinate worldPos, MouseEventArgs mouseEventArgs)
		{
			CurrentMouseCoordinate = worldPos;
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs("CurrentMouseCoordinate"));
				PropertyChanged(this, new PropertyChangedEventArgs("CurrentMouseCoordinateString"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}