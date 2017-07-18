using System;
using System.Drawing;
using System.IO;
using Common.Logging;
using GeoAPI.Geometries;
using SharpMap.Data;
using SharpMap.Data.Providers;

namespace SharpMap.Layers
{
	/// <summary>
	/// Implementation of TILEINDEX of GDAL raster-layers using image rendering
	/// </summary>
	public class GdiImageIndexLayer : GdiImageLayer
	{
		private static readonly ILog _logger = LogManager.GetLogger(typeof(GdiImageIndexLayer));

		private readonly ShapeFile _shapeFile;
		private readonly string _fieldName;
		private readonly string _fileName;
		private readonly Stream _notFoundStreamImage;

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
			Envelope = _shapeFile.GetExtents();
			_shapeFile.Close();
			_fieldName = fieldName;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="layerName"></param>
		/// <param name="fileName"></param>
		/// <param name="fieldName"></param>
		/// <param name="notFoundStreamImage"></param>
		public GdiImageIndexLayer(string layerName, string fileName, string fieldName, Stream notFoundStreamImage) : this(layerName, fileName, fieldName)
		{
			_notFoundStreamImage = notFoundStreamImage;
		}

		/// <inheritdoc />
		public override Envelope Envelope { get; }

		/// <inheritdoc />
		public override void Render(Graphics g, MapViewport map)
		{
			try
			{
				_shapeFile.Open();
				var ds = new FeatureDataSet();
				_shapeFile.ExecuteIntersectionQuery(map.Envelope, ds);
				_shapeFile.Close();

			    var dt = ds.Tables[0];
				foreach (FeatureDataRow fdr in dt.Rows)
				{
					var file = fdr[_fieldName] as string;
					if (!Path.IsPathRooted(file))
						file = Path.Combine(Path.GetDirectoryName(_fileName), file);

				    FileStream fs = null;
				    if (file == null || !File.Exists(file))
					{
						if (_notFoundStreamImage == null)
							continue;
						_image = Image.FromStream(_notFoundStreamImage);
					}
					else
					{
                        fs = new FileStream(file, FileMode.Open, FileAccess.Read);
					    if (!fs.CanRead) throw new FileLoadException("Cannot read file stream");
					    fs.Position = 0;
                        
					    _image = Bitmap.FromStream(fs, false, false);
					}

					if (_logger.IsDebugEnabled)
						_logger.Debug("Drawing " + file);
					
					_envelope = fdr.Geometry.EnvelopeInternal;
					var xres = (_envelope.MaxX - _envelope.MinX) / _image.Width;
					var yres = (_envelope.MaxY - _envelope.MinY)/_image.Height;
					var geoTrans = new[] { _envelope.MinX, xres, 0, _envelope.MaxY, 0, -yres };
					_worldFile = new WorldFile(geoTrans[1], geoTrans[2], geoTrans[4], geoTrans[5], geoTrans[0], geoTrans[3]);

					base.Render(g, map);
					_image.Dispose();
				    fs?.Dispose();
                }
			}
			catch (Exception ex)
			{
				_shapeFile.Close();
			}
		}

		/// <inheritdoc />
		protected override void ReleaseManagedResources()
		{
			_shapeFile.Dispose();
			base.ReleaseManagedResources();
		}
	}
}
