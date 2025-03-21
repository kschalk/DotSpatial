// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using DotSpatial.Serialization;
using NetTopologySuite.Geometries;

namespace DotSpatial.Data
{
    /// <summary>
    /// ShapeRange is used to store geometry information without using NTS.Geometry.
    /// </summary>
    public sealed class ShapeRange : ICloneable
    {
        #region Fields

        /// <summary>
        /// Control the epsilon to use for the intersect calculations.
        /// </summary>
        public const double Epsilon = double.Epsilon;

        private Extent _extent;
        private int _numParts;
        private int _numPoints;
        private ShapeType _shapeType;
        private int _startIndex;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeRange"/> class where vertices can be assigned later.
        /// </summary>
        /// <param name="type">the feature type clarifies point, line, or polygon.</param>
        /// <param name="coordType">The coordinate type clarifies whether M or Z values exist.</param>
        public ShapeRange(FeatureType type, CoordinateType coordType = CoordinateType.Regular)
        {
            FeatureType = type;
            Parts = new List<PartRange>();
            _numParts = -1; // default to relying on the parts list instead of the cached value.
            _numPoints = -1; // rely on accumulation from parts instead of a solid number

            _extent = coordType switch
            {
                CoordinateType.Z => new ExtentMz(),
                CoordinateType.M => new ExtentM(),
                _ => new Extent(),
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeRange"/> class of type point that has only the one point.
        /// </summary>
        /// <param name="v">The vertex used for the point.</param>
        public ShapeRange(Vertex v)
        {
            FeatureType = FeatureType.Point;
            Parts = new List<PartRange>();
            _numParts = -1;
            double[] coords = new double[2];
            coords[0] = v.X;
            coords[1] = v.Y;
            PartRange prt = new(coords, 0, 0, FeatureType.Point)
            {
                NumVertices = 1
            };
            Extent = new Extent(v.X, v.Y, v.X, v.Y);
            Parts.Add(prt);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeRange"/> class.
        /// </summary>
        /// <param name="env">The envelope to turn into a shape range.</param>
        public ShapeRange(Envelope env)
            : this(env.ToExtent())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeRange"/> class.
        /// This creates a polygon shape from an extent.
        /// </summary>
        /// <param name="ext">The extent to turn into a polygon shape.</param>
        public ShapeRange(Extent ext)
        {
            Extent = ext;
            Parts = new List<PartRange>();
            _numParts = -1;

            // Counter clockwise
            // 1 2
            // 4 3
            double[] coords = new double[8];

            // C1
            coords[0] = ext.MinX;
            coords[1] = ext.MaxY;

            // C2
            coords[2] = ext.MaxX;
            coords[3] = ext.MaxY;

            // C3
            coords[4] = ext.MaxX;
            coords[5] = ext.MinY;

            // C4
            coords[6] = ext.MinX;
            coords[7] = ext.MinY;

            FeatureType = FeatureType.Polygon;
            ShapeType = ShapeType.Polygon;
            PartRange pr = new(coords, 0, 0, FeatureType.Polygon)
            {
                NumVertices = 4
            };
            Parts.Add(pr);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the content length.
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the extent of this shape range. Setting this will prevent
        /// the automatic calculations.
        /// </summary>
        public Extent Extent
        {
            get
            {
                return _extent ?? (_extent = CalculateExtents());
            }

            set
            {
                _extent = value;
            }
        }

        /// <summary>
        /// Gets the feature type.
        /// </summary>
        public FeatureType FeatureType { get; private set; }

        /// <summary>
        /// Gets or sets the number of parts. If this is set, then it will cache an integer count that is independant from Parts.Count.
        /// If this is not set, (or set to a negative value) then getting this will return Parts.Count.
        /// </summary>
        public int NumParts
        {
            get
            {
                if (_numParts < 0) return Parts.Count;

                return _numParts;
            }

            set
            {
                _numParts = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of points in the entire shape.
        /// </summary>
        public int NumPoints
        {
            get
            {
                if (_numPoints < 0)
                {
                    int n = 0;
                    foreach (PartRange part in Parts)
                    {
                        n += part.NumVertices;
                    }

                    return n;
                }

                return _numPoints;
            }

            set
            {
                _numPoints = value;
            }
        }

        /// <summary>
        /// Gets the parts. If this is null, then there is only one part for this ShapeIndex.
        /// </summary>
        public List<PartRange> Parts { get; private set; }

        /// <summary>
        /// Gets or sets the record number (for .shp files usually 1-based).
        /// </summary>
        public int RecordNumber { get; set; }

        /// <summary>
        /// Gets or sets the shape type for the header of this shape.
        /// </summary>
        public ShapeType ShapeType
        {
            get
            {
                return _shapeType;
            }

            set
            {
                _shapeType = value;
                UpgradeExtent();
            }
        }

        /// <summary>
        /// Gets or sets the starting index for the entire shape range.
        /// </summary>
        public int StartIndex
        {
            get
            {
                return _startIndex;
            }

            set
            {
                NumPoints = 0;
                foreach (PartRange part in Parts)
                {
                    part.ShapeOffset = value;
                    NumPoints += part.NumVertices;
                }

                _startIndex = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Forces each of the parts to adopt an extent equal to a calculated extents.
        /// The extents for the shape will expand to include those.
        /// </summary>
        /// <returns>The calculated extent.</returns>
        public Extent CalculateExtents()
        {
            Extent ext = new();
            foreach (PartRange part in Parts)
            {
                ext.ExpandToInclude(part.CalculateExtent());
            }

            Extent = ext;
            return ext;
        }

        /// <summary>
        /// Creates a shallow copy of everything except the parts list and extent, which are deep copies.
        /// </summary>
        /// <returns>The copy.</returns>
        public object Clone()
        {
            ShapeRange copy = (ShapeRange)MemberwiseClone();
            copy.Parts = new List<PartRange>(Parts.Count);
            foreach (PartRange part in Parts)
            {
                copy.Parts.Add(part.Copy());
            }

            copy.Extent = Extent.Copy();
            return copy;
        }

        /// <summary>
        /// Gets the integer end index as calculated from the number of points and the start index.
        /// </summary>
        /// <returns>The end index.</returns>
        public int EndIndex()
        {
            return StartIndex + NumPoints - 1;
        }

        /// <summary>
        /// Gets the first vertex from the first part.
        /// </summary>
        /// <returns>The first vertex.</returns>
        public Vertex First()
        {
            double[] verts = Parts[0].Vertices;
            Vertex result = new(verts[StartIndex], verts[StartIndex + 1]);
            return result;
        }

        /// <summary>
        /// Tests the intersection with an extents.
        /// </summary>
        /// <param name="ext">The extent to check against.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(Extent ext)
        {
            return Intersects(new Shape(ext).Range);
        }

        /// <summary>
        /// Tests the intersection using an envelope.
        /// </summary>
        /// <param name="envelope">The envelope to check against.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(Envelope envelope)
        {
            return Intersects(new Shape(envelope).Range);
        }

        /// <summary>
        /// Tests the intersection with a coordinate.
        /// </summary>
        /// <param name="coord">The coordinate to check against.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(Coordinate coord)
        {
            return Intersects(new Shape(coord).Range);
        }

        /// <summary>
        /// Tests the intersection with a vertex.
        /// </summary>
        /// <param name="vert">The vertex to check against.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(Vertex vert)
        {
            return Intersects(new Shape(vert).Range);
        }

        /// <summary>
        /// Tests the intersection with a shape.
        /// </summary>
        /// <param name="shape">The shape to check against.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(Shape shape)
        {
            return Intersects(shape.Range);
        }

        /// <summary>
        /// Test the intersection with a shape range.
        /// </summary>
        /// <param name="shape">The shape to do intersection calculations with.</param>
        /// <returns>True, if both intersect.</returns>
        public bool Intersects(ShapeRange shape)
        {
            // Extent check first. If the extents don't intersect, then this doesn't intersect.
            if (!Extent.Intersects(shape.Extent)) return false;

            switch (FeatureType)
            {
                case FeatureType.Polygon:
                    PolygonShape.Epsilon = Epsilon;
                    return PolygonShape.Intersects(this, shape);
                case FeatureType.Line:
                    LineShape.Epsilon = Epsilon;
                    return LineShape.Intersects(this, shape);
                case FeatureType.Point:
                    PointShape.Epsilon = Epsilon;
                    return PointShape.Intersects(this, shape);
                default: return false;
            }
        }

        /// <summary>
        /// Given a vertex, this will determine the part that the vertex is within.
        /// </summary>
        /// <param name="vertexOffset">The vertex offset.</param>
        /// <returns>Part index that contains the vertex.</returns>
        public int PartIndex(int vertexOffset)
        {
            int i = 0;
            foreach (PartRange part in Parts)
            {
                if (part.StartIndex <= vertexOffset && part.EndIndex >= vertexOffset) return i;

                i++;
            }

            return -1;
        }

        /// <summary>
        /// This sets the vertex array by cycling through each part index and updates.
        /// </summary>
        /// <param name="vertices">The double array of vertices that should be referenced by the parts.</param>
        public void SetVertices(double[] vertices)
        {
            foreach (PartRange prt in Parts)
            {
                prt.Vertices = vertices;
            }
        }

        /// <summary>
        /// Considers the ShapeType and upgrades the extent class to accommodate M and Z.
        /// This is automatically called form the setter of ShapeType.
        /// </summary>
        public void UpgradeExtent()
        {
            if (_shapeType == ShapeType.MultiPointZ || _shapeType == ShapeType.PointZ || _shapeType == ShapeType.PolygonZ || _shapeType == ShapeType.PolyLineZ)
            {
                if (Extent is not IExtentZ zTest)
                {
                    Extent ext = new ExtentMz();
                    if (_extent != null) ext.CopyFrom(_extent);
                    _extent = ext;
                }

                // Already implements M and Z
            }
            else if (_shapeType == ShapeType.MultiPointM || _shapeType == ShapeType.PointM || _shapeType == ShapeType.PolygonM || _shapeType == ShapeType.PolyLineM)
            {
                if (Extent is not IExtentM mTest)
                {
                    Extent ext = new ExtentMz();
                    if (_extent != null) ext.CopyFrom(_extent);
                    _extent = ext;
                }

                // already at least implements M
            }

            // No upgrade necessary
        }

        #endregion
    }
}