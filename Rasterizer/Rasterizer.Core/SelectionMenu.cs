using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Rasterizer.Core
{
    public sealed class SelectionMenu
    {
        private readonly SpriteFont _font;
        private List<string> _items;
        private readonly int _lineHeight;
        private readonly int _screenW, _screenH;

        private int _index;
        private bool _isOpen;
        private bool _enterWasDown;
        private bool _upWasDown, _downWasDown;
        private bool _rWasDown;
        private bool _isPromptMode = false;

        private StringBuilder _promptInput = new StringBuilder();
        private GameWindow _window;
        private bool _subscribedToTextInput = false;

        public bool IsOpen => _isOpen;
        public bool HasSelection { get; private set; }
        public string? SelectedPath { get; private set; }
        private string dir;
        public SelectionMenu(GraphicsDevice gd, SpriteFont font, string directory, GameWindow window)
        {
            this.dir = directory;
            _font = font;
            _screenW = gd.Viewport.Width;
            _screenH = gd.Viewport.Height;
            _lineHeight = _font.LineSpacing;

            _items = new List<string>(Directory.GetFiles(directory, "*.obj", SearchOption.TopDirectoryOnly));
            if (_items.Count == 0)
                _items.Add("(no .obj files found)");

            _items.Add("Ask an AI for your own Model!");
            _index = 0;
            _window = window;
        }

        public void Open()
        {
            _isOpen = true;
            HasSelection = false;
            SelectedPath = null;
            _isPromptMode = false;
            _promptInput.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (!_isOpen) return;

            if (_isPromptMode)
            {
                HandlePromptModeKeys();
                return;
            }
            
            KeyboardState k = Keyboard.GetState();
            GamePadState p = GamePad.GetState(PlayerIndex.One);

            if ((k.IsKeyDown(Keys.R) || p.IsButtonDown(Buttons.X)) && !_rWasDown)
            {
                _items = new List<string>(Directory.GetFiles(dir, "*.obj", SearchOption.TopDirectoryOnly));
                if (_items.Count == 0)
                    _items.Add("(no .obj files found)");

                _items.Add("Ask an AI for your own Model!");
                _index = 0;
            }
            if ((k.IsKeyDown(Keys.Up) || p.IsButtonDown(Buttons.DPadUp)) && !_upWasDown)
                _index = (_index - 1 + _items.Count) % _items.Count;

            if ((k.IsKeyDown(Keys.Down) || p.IsButtonDown(Buttons.DPadDown)) && !_downWasDown)
                _index = (_index + 1) % _items.Count;

            if ((k.IsKeyDown(Keys.Enter) || p.IsButtonDown(Buttons.A)) && !_enterWasDown && _items.Count > 0)
            {
                if (_index == _items.Count - 1)
                {
                    _isPromptMode = true;
                    _promptInput.Clear();
                    SubscribeTextInput();
                }
                else
                {
                    HasSelection = true;
                    SelectedPath = _items[_index];
                    _isOpen = false;
                }
            }

            if (k.IsKeyDown(Keys.Escape) || p.IsButtonDown(Buttons.B))
            {
                HasSelection = false;
                SelectedPath = null;
                _isOpen = false;
            }

            _upWasDown = k.IsKeyDown(Keys.Up) || p.IsButtonDown(Buttons.DPadUp);
            _downWasDown = k.IsKeyDown(Keys.Down) || p.IsButtonDown(Buttons.DPadDown);
            _enterWasDown = k.IsKeyDown(Keys.Enter) || p.IsButtonDown(Buttons.A);
            _rWasDown = k.IsKeyDown(Keys.R) || p.IsButtonDown(Buttons.X);
        }

        private void HandlePromptModeKeys()
        {
            KeyboardState k = Keyboard.GetState();
            GamePadState p = GamePad.GetState(PlayerIndex.One);
            // Handle Enter = finish, Escape = cancel, Backspace already handled in the event
            if ((k.IsKeyDown(Keys.Enter) || p.IsButtonDown(Buttons.A))&& !_enterWasDown)
            {
                string txt = _promptInput.ToString();
                HandleCustomEntry(txt);
                UnsubscribeTextInput();
                _isPromptMode = false;
                _isOpen = true;
                HasSelection = false;
            }
            else if (k.IsKeyDown(Keys.Escape))
            {
                _promptInput.Clear();
                UnsubscribeTextInput();
                _isPromptMode = false;
            }

            _enterWasDown = k.IsKeyDown(Keys.Enter) || p.IsButtonDown(Buttons.A);
        }

        private void SubscribeTextInput()
        {
            if (!_subscribedToTextInput)
            {
                _window.TextInput += OnTextInput;
                _subscribedToTextInput = true;
            }
        }

        private void UnsubscribeTextInput()
        {
            if (_subscribedToTextInput)
            {
                _window.TextInput -= OnTextInput;
                _subscribedToTextInput = false;
            }
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (!_isPromptMode) return;

            // Handle deletion
            if (e.Key == Keys.Back)
            {
                if (_promptInput.Length > 0)
                    _promptInput.Length--;
                return;
            }

            // Only append printable chars
            if (!char.IsControl(e.Character))
            {
                _promptInput.Append(e.Character);
            }
        }

        // Dummy handler for custom entry
        private void HandleCustomEntry(string input)
        {
            // REPLACE WITH YOUR OWN LOGIC!
            MeshyClient meshyClient =
                new MeshyClient("msy_dummy_api_key_for_test_mode_12345678",
                    "/Users/noah/RiderProjects/Rasterizer/Rasterizer/Rasterizer.Core/objs");
            meshyClient.GenerateAndDownloadObjAsync(input);
        }

        public void Draw(SpriteBatch sb)
        {
            if (!_isOpen) return;

            sb.Begin();

            const int margin = 40;
            int startY = margin;

            if (_isPromptMode)
            {
                string prompt = "Prompt: " + _promptInput;
                sb.DrawString(_font, prompt, new Vector2(margin, startY), Color.Cyan);
            }
            else
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    string text = (i == _items.Count - 1) ? _items[i] : Path.GetFileName(_items[i]);
                    Color col = (i == _index) ? Color.Yellow : Color.White;
                    sb.DrawString(_font, text, new Vector2(margin, startY + i * _lineHeight), col);
                }
            }

            // Simple semi-transparent background so text is readable
            Texture2D pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { new Color(0, 0, 0, 0.5f) });
            sb.Draw(pixel, new Rectangle(0, 0, _screenW, _screenH), Color.Black * 0.4f);
            pixel.Dispose();

            sb.End();
        }
    }
}
