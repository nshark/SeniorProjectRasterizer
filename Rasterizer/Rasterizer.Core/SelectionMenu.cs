using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Rasterizer.Core;

public sealed class SelectionMenu
{
    private readonly SpriteFont _font;
    private readonly List<string> _items;
    private readonly int _lineHeight;
    private readonly int _screenW, _screenH;

    private int _index; 
    private bool _isOpen;
    private bool _enterWasDown; 
    private bool _upWasDown, _downWasDown;

    public bool IsOpen => _isOpen;
    public bool HasSelection { get; private set; }
    public string? SelectedPath { get; private set; }

    public SelectionMenu(GraphicsDevice gd, SpriteFont font, string directory)
    {
        _font = font;
        _screenW = gd.Viewport.Width;
        _screenH = gd.Viewport.Height;
        _lineHeight = _font.LineSpacing;

        _items = new List<string>(Directory.GetFiles(directory, "*.obj",
            SearchOption.TopDirectoryOnly));
        if (_items.Count == 0)
            _items.Add("(no .obj files found)");

        _index = 0;
    }

    public void Open()
    {
        _isOpen = true;
        HasSelection = false;
        SelectedPath = null;
    }

    public void Update(GameTime gameTime)
    {
        if (!_isOpen) return;

        KeyboardState k = Keyboard.GetState();


        if (k.IsKeyDown(Keys.Up) && !_upWasDown)
            _index = (_index - 1 + _items.Count) % _items.Count;
        if (k.IsKeyDown(Keys.Down) && !_downWasDown)
            _index = (_index + 1) % _items.Count;


        if (k.IsKeyDown(Keys.Enter) && !_enterWasDown && _items.Count > 0)
        {
            HasSelection = true;
            SelectedPath = _items[_index];
            _isOpen = false;
        }
        
        if (k.IsKeyDown(Keys.Escape))
        {
            HasSelection = false;
            SelectedPath = null;
            _isOpen = false;
        }

        _upWasDown = k.IsKeyDown(Keys.Up);
        _downWasDown = k.IsKeyDown(Keys.Down);
        _enterWasDown = k.IsKeyDown(Keys.Enter);
    }

    public void Draw(SpriteBatch sb)
    {
        if (!_isOpen) return;

        sb.Begin();

        const int margin = 40;
        int startY = margin;

        for (int i = 0; i < _items.Count; i++)
        {
            string text = Path.GetFileName(_items[i]);
            Color col = (i == _index) ? Color.Yellow : Color.White;
            sb.DrawString(_font, text,
                new Vector2(margin, startY + i * _lineHeight), col);
        }

        // Simple semi-transparent background so text is readable
        Texture2D pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { new Color(0, 0, 0, 0.5f) });
        sb.Draw(pixel, new Rectangle(0, 0, _screenW, _screenH),
            Color.Black * 0.4f);
        pixel.Dispose();

        sb.End();
    }
}