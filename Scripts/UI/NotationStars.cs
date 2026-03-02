using Godot;

namespace Randonneur
{
    public class NotationStar
    {
        /// <summary>
        /// Star textures.
        /// </summary>
        private Texture2D _starOnTexture = GD.Load<Texture2D>("res://Art/UI/starOn.png");
        private Texture2D _starOffTexture = GD.Load<Texture2D>("res://Art/UI/starOff.png");

        /// <summary>
        /// Stores the stars.
        /// </summary>
        private TextureRect?[] _starTextures;

        private uint _maxStarsCount = 4;

        /// <summary>
        /// Const.
        /// </summary>
        private const float _starScale = 0.25f;
        private const float _starWidthSpacing = 10.0f;
        private const float _starHeightSpacing = 4.0f;

        /// <summary>
        /// Constructor of notation stars.
        /// </summary>
        /// <param name="parent">Parent node to add stars as a child.</param>
        /// <param name="maxStarsCount">Optional maximum number of stars.</param>
        /// <param name="positionY">Optional starting Y position.</param>
        public NotationStar(Node2D parent, uint maxStarsCount = 4, float positionY = 0.0f)
        {
            // Gap width between stars
            float starWidthGap = _starOnTexture.GetWidth() * _starScale + _starWidthSpacing;
            // Height position of top left corner of a star
            float startHeightPosition = positionY + _starHeightSpacing;

            _maxStarsCount = maxStarsCount;
            _starTextures = new TextureRect[maxStarsCount];

            // allocate stars without texture
            for (int i = 0; i < _starTextures.Length; i++)
            {
                // display the note with stars
                TextureRect starTrect = new()
                {
                    Scale = new Vector2(_starScale, _starScale),
                    ZIndex = Global.ZIndexUILayer1,
                    Position = new Vector2(i * starWidthGap, startHeightPosition),
                };
                _starTextures[i] = starTrect;
                parent.AddChild(starTrect);
            }
        }

        /// <summary>
        /// Add stars over depending of the note.
        /// </summary>
        /// <param name="nbStars">Number of ligthed stars.</param>
        public void Add(uint nbStars)
        {
            // sanity check
            if (nbStars < 0 || nbStars > _maxStarsCount)
            {
                GD.PushWarning($"Add(): wrong number of stars {nbStars}");
                return;
            }

            for (int i = 0; i < _starTextures.Length; i++)
            {
                if (_starTextures[i] is TextureRect star)
                    star.Texture = i < nbStars ? _starOnTexture : _starOffTexture;
            }
        }

        /// <summary>
        /// Remove previous stars (remove texture).
        /// </summary>
        public void Remove()
        {
            for (int i = 0; i < _starTextures.Length; i++)
            {
                if (_starTextures[i] is TextureRect star)
                {
                    star.Texture = null;
                }
            }
        }
    }
}
