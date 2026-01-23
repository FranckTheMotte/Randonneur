using System;
using Godot;

namespace Randonneur
{
    /// <summary>
    /// This class provide tools to share a landscape in zones from top/down view
    /// for adding objects on it.
    /// Class makes assumption that landscape has a valid size.
    /// <param name="scene">Scene with required elements:
    ///  - Robin
    ///  - Tips
    ///  - DeployPolyXX
    /// </param>
    /// </summary>
    public class Landscape(Node3D scene)
    {
        /// <summary>
        /// Bounds of landscape.
        /// </summary>
        private Vector3 _planeStartPosition;
        private Vector3 _planeEndPosition;

        /// <summary>
        /// Array of Square.
        /// </summary>
        private LandscapeSquare[]? _Squares;

        // Outside of landscape
        const float OFFLANDSCAPE = 1000000.0f;

        const string DEPLOY_AREAS_PREFIX = "DeployPoly2D";
        const int DEPLOY_AREAS_COUNT = 9;

        /// <summary>
        /// Properties of virtual repartition grid.
        /// </summary>
        private uint _columns = 2;
        private uint _rows = 2;

        private readonly Random _rand = new();

        /// <summary>
        /// Ref to plane.
        /// </summary>
        private MeshInstance3D? _plane;

        private Node3D? _hillLandscape;

        private readonly Node3D? _mainScene = scene;

        /// <summary>
        /// Initialize landscape properties.
        /// </summary>
        /// <param name="columns">Defines the colums of virtual grid repartition.</param>
        /// <param name="rows">Defines the rows of virtual grid repartition.</param>
        /// <returns></returns>
        public bool Init(uint columns, uint rows)
        {
            if (_mainScene == null)
            {
                GD.PushError("Init(): main scene is not defined.");
                return false;
            }

            _hillLandscape = _mainScene.GetNode<Node3D>("HillLandscape");
            _plane = _hillLandscape.GetNode<MeshInstance3D>("Plane");

            Vector3 planeSize = _plane.Mesh.GetAabb().Size * _plane.Scale;

            // Divide by 2 because it's centered
            _planeStartPosition = (-planeSize / 2) + _hillLandscape.Position;
            _planeEndPosition = (planeSize / 2) + _hillLandscape.Position;

            if (Split(columns, rows) == false)
                return false;

            return true;
        }

        /// <summary>
        /// Split the landscape in a grid hParts * vParts.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        /// <returns> true if success, false otherwise</returns>
        private bool Split(uint columns, uint rows)
        {
            // sanity checks
            if (columns < 1 || columns > 16 || rows < 1 || rows > 16)
            {
                GD.PushError("Split(): rows and colums must be [1-16].");
                return false;
            }

            // landscape properties, for a top view, X and Z represent
            // respectively width and length.
            float width  = _planeEndPosition.X - _planeStartPosition.X;
            float length = _planeEndPosition.Z - _planeStartPosition.Z;

            if (width < 1.0f || length < 1.0f)
            {
                GD.PushError("Split(): landscape is too small.");
                return false;
            }

            // 1 square
            Vector2 squareSize = new(width / columns, length / columns);
            Vector2 startPoint = new(_planeStartPosition.X, _planeStartPosition.Z);

            // save
            _columns = columns;
            _rows = rows;
            _Squares = new LandscapeSquare[columns * rows];

            int squareCounter = 0;
            for (uint i = 0; i < columns; i++)
            {
                for (uint j = 0; j < rows; j++)
                {
                    Vector2 squareStartPoint =
                        startPoint + new Vector2(i * squareSize.X, j * squareSize.Y);
                    _Squares[squareCounter] = new LandscapeSquare(startPoint, squareSize);
                    squareCounter++;
                }
            }

            return true;
        }

        /// <summary>
        /// Return a random position inside a random free square.
        /// </summary>
        /// <param name="type">Object type expected to be added.</param>
        /// <returns>
        ///  -  0 : success, return a valid vector and a slot is reserved
        ///  - -1 : internal error
        ///  - -2 : no slot are available
        /// </returns>
        public (int result, Vector2 position) GetPosition(LandscapeObjectType type)
        {
            int result = -1;
            // sanity checks
            if (_Squares == null)
            {
                GD.PushError("GetPosition(): no squares are defined.");
                return (result, new Vector2());
            }
            uint[] indexesUsed = new uint[_Squares.Length];
            int nbIndexesUsed = 0;
            uint index;
            Rect2 boundingBox;
            float x = 0.0f;
            float y = 0.0f;

            // look until a place is found
            bool found = false;
            do
            {
                index = (uint)_rand.Next(_Squares.Length);
                if (indexesUsed[index] != 0)
                {
                    // already used, reroll.
                    continue;
                }

                boundingBox = _Squares[index].BoundingBox;

                // a free slot ?
                if (_Squares[index].GetObjectCount(type) < 2)
                {
                    x = _rand.NextSingle() * boundingBox.Size.X + boundingBox.Position.X;
                    y = _rand.NextSingle() * boundingBox.Size.Y + boundingBox.Position.Y;
                    _Squares[index].AddObject(type);
                    found = true;
                    result = 0;
                }
                else
                {
                    indexesUsed[index] = 1;
                    nbIndexesUsed++;
                    if (nbIndexesUsed >= _Squares.Length)
                    {
                        GD.PushWarning("All landscape squares have been used.");
                        result = -2;
                    }
                }
            } while (!found && nbIndexesUsed < _Squares.Length);

            return (result, new Vector2(x, y));
        }

        /// <summary>
        /// Place randomly a PNJ.
        /// </summary>
        public void InitHiddenPNJ(VisibleOnScreenNotifier3D pnjNotifier)
        {
            // sanity checks
            if (_plane == null || _mainScene == null)
            {
                GD.PushError("InitHiddenPNJ() sanity check failed.");
                return;
            }

            // inside a lower square
            float x = _planeStartPosition.X + _rand.Next(100, 900);
            float z = _planeStartPosition.Z + _rand.Next(100, 900);
            float landscapeY = GetYOnLandscape(x, z);
            if (landscapeY == OFFLANDSCAPE)
            {
                GD.PushError("Failed to place the PNJ.");
                return;
            }

            // Only the robin can be placed for the moment
            Sprite3D pnj = pnjNotifier.GetNode<Sprite3D>("Robin");
            pnjNotifier.Position = new Vector3(
                x,
                landscapeY + (pnj.Texture.GetSize().Y / _plane.Scale.Y) + 1,
                z
            );

            // Debug: add a flower over the robin to easily find him.
            Sprite3D tips = _mainScene.GetNode<Sprite3D>("Tips");
            tips.Position = pnjNotifier.Position + new Vector3(0.0f, 100.0f, 0.0f);

            GD.Print($"robin visible {pnjNotifier.IsVisibleInTree()}");
        }

        /// <summary>
        /// Place randomly trees in areas.
        /// </summary>
        public void InitTrees(int treeDensity, int treeSize)
        {
            // sanity checks
            if (_plane == null || _mainScene == null)
            {
                GD.PushError("InitTrees() sanity check failed.");
                return;
            }

            // Load tree Textures
            Texture2D[] treeTexture2D =
            {
                GD.Load<Texture2D>("res://Art/Background/Tree1.png"),
                GD.Load<Texture2D>("res://Art/Background/Tree2.png"),
                GD.Load<Texture2D>("res://Art/Background/Tree3.png"),
            };

            int treesAreas = 0;

            // use various polygons to define tree areas
            // polygons forms are defined in the scene
            List<Polygon2D> polygons = [];
            for (treesAreas = 0; treesAreas < DEPLOY_AREAS_COUNT; treesAreas++)
            {
                // place somewhere in the landscape
                (int result, Vector2 randomPosition) = GetPosition(LandscapeObjectType.Trees);
                if (result < 0)
                {
                    GD.PushWarning("Failed to place another trees areas, stop.");
                    treesAreas--;
                    break;
                }
                Polygon2D currentPolygon2D = _mainScene.GetNode<Polygon2D>(
                    DEPLOY_AREAS_PREFIX + (treesAreas + 1).ToString("00")
                );
                Vector2[] polygon = currentPolygon2D.Polygon;

                // shift each polygon's coordinates
                for (int j = 0; j < polygon.Length; j++)
                {
                    polygon[j] += randomPosition;
                }
                currentPolygon2D.Polygon = polygon;

                // hide defined Polygon2D
                currentPolygon2D.Visible = false;
                polygons.Add(currentPolygon2D);
            }

            int treeCount = 0;
            // Populate trees in each polygon
            for (int i = 0; i < treesAreas; i++)
            {
                // Browse all coords to place items randomly
                for (float x = _planeStartPosition.X; x < _planeEndPosition.X; x += treeDensity)
                {
                    for (float z = _planeStartPosition.Z; z < _planeEndPosition.Z; z += treeDensity)
                    {
                        // Trees are added randomly within allowed areas, not on every pixel.
                        if (
                            Geometry2D.IsPointInPolygon(new Vector2(x, z), polygons[i].Polygon)
                            && _rand.Next(0, treeDensity * 15) == 0
                        )
                        {
                            treeCount++;
                            float landscapeY = GetYOnLandscape(x, z);
                            if (landscapeY != OFFLANDSCAPE)
                            {
                                Sprite3D tree = new()
                                {
                                    Texture = treeTexture2D[_rand.Next(0, treeTexture2D.Length)],
                                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                                    Shaded = true,
                                    TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                                    Scale = new Vector3(treeSize, treeSize, treeSize),
                                };
                                tree.Position = new Vector3(
                                    x,
                                    landscapeY
                                        + ((tree.Texture.GetSize().Y * treeSize) / _plane.Scale.Y)
                                        + treeSize,
                                    z
                                );
                                _mainScene.AddChild(tree);
                            }
                        }
                    }
                }
            }
            GD.Print($"{treeCount} trees added.");
        }

        /// <summary>
        /// From a specific point, use ray casting to retrieve the Y coordinate on the landscape.
        /// </summary>
        /// <param name="x">X coordinate in world space</param>
        /// <param name="z">Z coordinate in world space</param>
        /// <returns>
        /// The Y coordinate where the ray hits the landscape (StaticBody3D),
        /// or
        /// - Global.SANITY_CHECK_ERROR if env error is detected.
        /// - OFFLANDSCAPE if no collision detected (point outside terrain bounds).
        /// </returns>
        private float GetYOnLandscape(float x, float z)
        {
            if (_mainScene == null)
            {
                return Global.SANITY_CHECK_ERROR;
            }

            var spaceState = _mainScene.GetWorld3D().DirectSpaceState;
            Vector3 rayOrigin = new(x, 1000f, z);
            Vector3 rayEnd = new(x, -1000f, z);

            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;

            var result = spaceState.IntersectRay(query);
            if (result.Count > 0)
            {
                return ((Vector3)result["position"]).Y;
            }

            // no collision
            return OFFLANDSCAPE;
        }
    }
}
