using System;
using Godot;

namespace Randonneur
{
    /// <summary>
    /// Properties of a square in a landscape.
    /// </summary>
    public class LandscapeSquare
    {
        /// <summary>
        /// Bounding box of the square.
        /// </summary>
        public Rect2 BoundingBox { get; }

        /// <summary>
        /// Store the number of objects by type.
        /// </summary>
        private readonly Dictionary<LandscapeObjectType, uint> _content = [];

        /// <summary>
        /// Create a new square.
        /// </summary>
        /// <param name="position">Upper-left corner position.</param>
        /// <param name="size">Width and height of the square (must be positive).</param>
        /// <exception cref="ArgumentException">Thrown when size is not positive.</exception>
        public LandscapeSquare(Vector2 position, Vector2 size)
        {
            if (size.X <= 0 || size.Y <= 0)
            {
                throw new ArgumentException("Square size must be positive", nameof(size));
            }
            BoundingBox = new(position, size);
        }

        /// <summary>
        /// Add an object inside this square.
        /// </summary>
        /// <param name="type"></param>
        public void AddObject(LandscapeObjectType type)
        {
            if (_content.TryGetValue(type, out uint value))
            {
                _content[type]++;
                return;
            }
            _content.Add(type, 1);
        }

        /// <summary>
        /// Retrieve the number of object (by type) inside this square.
        /// </summary>
        /// <param name="type">Requested landscape object type.</param>
        /// <returns>Positive counter.</returns>
        public uint GetObjectCount(LandscapeObjectType type)
        {
            if (_content.TryGetValue(type, out uint value))
                return value;
            return 0;
        }

        public override string ToString()
        {
            return $"LandscapeSquare[Pos={BoundingBox.Position}, Size={BoundingBox.Size}, Objects={_content.Count} types]";
        }
    }
}
