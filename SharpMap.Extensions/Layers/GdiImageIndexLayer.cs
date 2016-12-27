using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Common.Logging;
using GeoAPI.Geometries;
using OSGeo.GDAL;
using SharpMap.Data;
using SharpMap.Data.Providers;

namespace SharpMap.Layers
{
	/// <summary>
	/// Implementation of TILEINDEX of GDAL raster-layers using image rendering
	/// </summary>
	public class GdiImageIndexLayer : GdiImageLayer
	{
		class CacheHolder
		{
			public WorldFile WorldFile;
			public Envelope Envelope;
			public float Transparency;
		}

		private static readonly ILog _logger = LogManager.GetLogger(typeof(GdiImageIndexLayer));

		private readonly ShapeFile _shapeFile;
		private readonly string _fieldName;
		private readonly string _fileName;
		private readonly Envelope _extents;
		private readonly Dictionary<string, CacheHolder> _openDatasets;

		/// <summary>
		/// Open a TileIndex shapefile
		/// 
		/// A tileindex is a shapefile that ties together several datasets into a single layer. Therefore, you don’t need to create separate layers for each piece of imagery or each county’s road data; make a tileindex and let SharpMap piece the mosaic together on the fly.
		/// Making a tileindex is easy using gdaltindex for GDAL data sources (rasters). You just run the tool, specifying the index file to create and the list of data sources to add to the index.
		///
		/// For example, to make a mosaic of several TIFFs:
		///
		/// gdaltindex imagery.shp imagery/*.tif
		/// </summary>
		/// <param name="layerName">Name of the layer</param>
		/// <param name="fileName">Path to the ShapeFile containing tile-indexes</param>
		/// <param name="fieldName">FieldName in the shapefile storing full or relative path-names to the datasets</param>
		public GdiImageIndexLayer(string layerName, string fileName, string fieldName)
        {
			GdalConfiguration.ConfigureGdal();

			LayerName = layerName;
			_fileName = fileName;
			_shapeFile = new ShapeFile(fileName, true);
			_shapeFile.Open();
			_extents = _shapeFile.GetExtents();
			_shapeFile.Close();
			_fieldName = fieldName;
			_openDatasets = new Dictionary<string, CacheHolder>();
		}

		public override Envelope Envelope
		{
			get
			{
				return _extents;
			}
		}

		public override void Render(Graphics g, MapViewport map)
		{
			try
			{
				_shapeFile.Open();
				var ds = new FeatureDataSet();
				_shapeFile.ExecuteIntersectionQuery(map.Envelope, ds);

				var dt = ds.Tables[0];
				foreach (FeatureDataRow fdr in dt.Rows)
				{
					var file = fdr[_fieldName] as string;
					if (!Path.IsPathRooted(file))
						file = Path.Combine(Path.GetDirectoryName(_fileName), file);

					if (file == null || !File.Exists(file))
						continue;

					if (_logger.IsDebugEnabled)
						_logger.Debug("Drawing " + file);

					if (!_openDatasets.ContainsKey(file))
					{
						var gdalDataset = Gdal.OpenShared(file, Access.GA_ReadOnly);
						var geoTrans = new double[6];
						gdalDataset.GetGeoTransform(geoTrans);
						_worldFile = new WorldFile(geoTrans[1], geoTrans[2], geoTrans[4], geoTrans[5], geoTrans[0], geoTrans[3]);
						_image = Image.FromFile(file);
						_envelope = _worldFile.ToGroundBounds(_image.Width, _image.Height).EnvelopeInternal;
						_openDatasets.Add(file, new CacheHolder()
						{
							WorldFile = _worldFile,
							Envelope = _envelope
						});
					}
					else
					{
						CacheHolder hld = _openDatasets[file];
						_worldFile = hld.WorldFile;
						_envelope = hld.Envelope;
						_image = Image.FromFile(file);
					}

					base.Render(g, map);
					_envelope = null;
					_image.Dispose();
				}
			}
			catch (Exception)
			{
				_shapeFile.Close();
			}
		}

		protected override void ReleaseManagedResources()
		{
			_shapeFile.Dispose();
			base.ReleaseManagedResources();
		}
	}
}
