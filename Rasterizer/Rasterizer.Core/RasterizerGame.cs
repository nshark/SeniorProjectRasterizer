using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Point = System.Drawing.Point;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;



namespace Rasterizer.Core;

internal enum GameMode { SelectingObj, Running }

/// <summary>
///     The main class for the game, responsible for managing game components, settings,
///     and platform-specific configurations.
/// </summary>
public class RasterizerGame : Game
{
    public static bool activateLog = false;
    private GameMode _mode = GameMode.SelectingObj;
    private SelectionMenu _menu;
    private SpriteFont _font;
    private string? _currentObjPath;
    private const float MouseSensitivity = 0.0025f;
    private const float ViewportWidth = 1;
    private const float DistanceOfViewport = 1;
    private const float ViewportHeight = 1;
    private const int PixelWidth = 640;
    private const int PixelHeight = 640;
    private const int PxHalf = PixelWidth >> 1;
    private const int PyHalf = PixelHeight >> 1;
    private bool _menuKeyWasDown;
    private const float InvPwDv = 1f / (PixelWidth * DistanceOfViewport);
    private const float InvPhDv = 1f / (PixelHeight * DistanceOfViewport);
    private Instance _curInstance;

    public static readonly bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public static readonly bool IsDesktop =
        OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    private Matrix _cameraMatrix = Matrix.Identity;
    private readonly Plane[] _cameraPlanes = new Plane[5];

    private readonly Vector3[] _cameraPlanesVectors =
    {
        new(0, 0, 1),
        new(1, 0, 0.5f),
        new(-1, 0, 0.5f),
        new(0, 1, 0.5f),
        new(0, -1, 0.5f)
    };

    private Vector4 _cameraPosition = new(0, 0, -15, 1);
    

    private Quaternion _cameraRotation = Quaternion.Identity;
    private double[] _depthBuffer;

    private readonly List<Light> _lights = new();

    private Color[] _pixelBuffer;
    private Texture2D _pixelTexture;
    private Matrix _projectionMatrix = Matrix.Identity;
    private SpriteBatch _spriteBatch;
    private Point _windowCentre; 
    
    private float _yaw, _pitch; 
    
    private readonly GraphicsDeviceManager graphicsDeviceManager;
    
    public RasterizerGame()
    {
        graphicsDeviceManager = new GraphicsDeviceManager(this);

        Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);

        Content.RootDirectory = "Content";

        graphicsDeviceManager.SupportedOrientations =
            DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        graphicsDeviceManager.PreferredBackBufferHeight = PixelHeight;
        graphicsDeviceManager.PreferredBackBufferWidth = PixelWidth;
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        for (var i = 0; i < 5; i++)
        {
            _cameraPlanesVectors[i].Normalize();
            if (i == 0)
                _cameraPlanes[i] = new Plane(_cameraPlanesVectors[i], -DistanceOfViewport);
            else
                _cameraPlanes[i] = new Plane(_cameraPlanesVectors[i], 0);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixelTexture = new Texture2D(GraphicsDevice, PixelWidth, PixelHeight);
        _pixelBuffer = new Color[PixelWidth * PixelHeight];
        _depthBuffer = new double[PixelWidth * PixelHeight];
        _projectionMatrix[0, 0] = DistanceOfViewport * PixelWidth / ViewportWidth;
        _projectionMatrix[1, 1] = DistanceOfViewport * PixelHeight / ViewportHeight;
        _projectionMatrix[3, 3] = 0;
        _lights.Add(new Light(0.2));
        _lights.Add(new DirectionalLight(0.3, Vector3.Down));
        _lights.Add(new PointLight(0.5, new Vector3(0, 2, 5))); 
        _windowCentre = new Point(PixelWidth / 2, PixelHeight / 2);
        IsMouseVisible = false;
        _font = Content.Load<SpriteFont>("Fonts/Hud");
        _menu = new SelectionMenu(GraphicsDevice, _font, "/Users/noah/RiderProjects/Rasterizer/Rasterizer/Rasterizer.Core/objs");
        _menu.Open();
        
        Mouse.SetPosition(_windowCentre.X, _windowCentre.Y);
    }
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Space))
        {
            activateLog = true;
        }
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dtMs = gameTime.ElapsedGameTime.Milliseconds;
        if (_mode == GameMode.SelectingObj)
        {
            _menu.Update(gameTime);

            if (_menu.HasSelection)
            {
                _currentObjPath = _menu.SelectedPath;
                _curInstance = new Instance(new Vector4(0,0,0,1),Quaternion.Identity, 1,Model.FromOBJ(_currentObjPath),Color.White,1);          // <-- YOU IMPLEMENT THIS
                _mode = GameMode.Running;
            }
            else if (!_menu.IsOpen)                
            {
                _menu.Open();                      
            }
            return;                                
        }
        KeyboardState k = Keyboard.GetState();
        if (k.IsKeyDown(Keys.M) && !_menuKeyWasDown)
        {
            _mode = GameMode.SelectingObj;
            _menu.Open();
            _menuKeyWasDown = true;
            return;
        }
        _menuKeyWasDown = k.IsKeyDown(Keys.M);
        var m = Mouse.GetState();
        var dx = m.X - _windowCentre.X;
        var dy = m.Y - _windowCentre.Y;
        if (GamePad.GetState(PlayerIndex.One).IsConnected)
        {
            dx = (int)(10*GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X);
            dy = -(int)(10*GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y);
        }
        
        if (dx != 0 || dy != 0)
        {
            _yaw += dx * MouseSensitivity;
            _pitch -= dy * MouseSensitivity;
            _pitch = MathHelper.Clamp(_pitch,
                -MathHelper.PiOver2 + 0.01f,
                MathHelper.PiOver2 - 0.01f);

            _cameraRotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0f);

            // re-centre the cursor so next frame we only get deltas
            Mouse.SetPosition(_windowCentre.X, _windowCentre.Y);
        }

        /* ------------------------------------------
         * 2)  WASD movement (relative to camera)
         * ----------------------------------------*/
        var kb = Keyboard.GetState();
        var move = Vector3.Zero;

        if (kb.IsKeyDown(Keys.W)) move -= Vector3.Forward;
        if (kb.IsKeyDown(Keys.S)) move -= Vector3.Backward;
        if (kb.IsKeyDown(Keys.A)) move += Vector3.Left;
        if (kb.IsKeyDown(Keys.D)) move += Vector3.Right;
        if (GamePad.GetState(PlayerIndex.One).IsConnected)
        {
            move += new Vector3(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X,0,GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y);
        }
        if (move.LengthSquared() > 0)
        {
            move.Normalize();
            move = Vector3.Transform(move, _cameraRotation);
            var speed = 0.01f * dtMs; 
            _cameraPosition += new Vector4(move * speed, 0f);
        }
        Vector3 camPos = new Vector3(_cameraPosition.X,
            _cameraPosition.Y,
            _cameraPosition.Z);

        Vector3 camForward = Vector3.Transform(Vector3.Forward, _cameraRotation);
        Vector3 camUp = Vector3.Transform(Vector3.Up, _cameraRotation);

        _cameraMatrix = Matrix.CreateLookAt(camPos, camPos + camForward, camUp);
        for (var y = 0; y < PixelHeight; y++)
        for (var x = 0; x < PixelWidth; x++)
        {
            var idx = y * PixelWidth + x;
            _pixelBuffer[idx] = Color.Black;
            _depthBuffer[idx] = double.MaxValue;
        }

        if (_curInstance != null)
        {
            RenderInstance(_curInstance);
        }
        _pixelTexture.SetData(_pixelBuffer);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_mode == GameMode.SelectingObj)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _menu.Draw(_spriteBatch);
            return;
        }
        GraphicsDevice.Clear(Color.Black);
        
        _spriteBatch.Begin();
        
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, PixelWidth, PixelHeight), Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    public void WriteToPixelWithLight(int x, int y, Color c, float i)
    {
        var y1 = y + PyHalf;
        var x1 = x + PxHalf;
        if (y is > -1 * PyHalf and < PyHalf && x is > -1 * PxHalf and < PxHalf)
        {
            var c1 = RasterizerLogic.ScaleColor(c, i);
            _pixelBuffer[y1 * PixelWidth + x1] = c1;
        }
    }

    public void WriteToPixel(int x, int y, Color c)
    {
        var y1 = y + PixelHeight / 2;
        var x1 = x + PixelWidth / 2;
        if ((y > -1 * PixelHeight / 2) & (y < PixelHeight / 2) & (x > -1 * PixelWidth / 2) & (x < PixelWidth / 2))
            _pixelBuffer[y1 * PixelWidth + x1] = c;
    }

    public void DrawLine(Vector2 pointA, Vector2 pointB, Color c)
    {
        if (Math.Abs(pointB.X - pointA.X) > Math.Abs(pointB.Y - pointA.Y))
        {
            if (pointA.X > pointB.X) (pointA, pointB) = (pointB, pointA);
            var values = RasterizerLogic.Interpolate(pointA.X, pointA.Y, pointB.X, pointB.Y);
            for (var x = (int)double.Round(pointA.X); x < (int)double.Round(pointB.X); x++)
                WriteToPixel(x, (int)double.Round(values[(int)double.Round(x - pointA.X)]), c);
        }
        else
        {
            if (pointA.Y > pointB.Y) (pointA, pointB) = (pointB, pointA);
            var values = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointB.Y, pointB.X);
            for (var y = (int)double.Round(pointA.Y); y < (int)double.Round(pointB.Y); y++)
                WriteToPixel((int)double.Round(values[(int)double.Round(y - pointA.Y)]), y, c);
        }
    }

    public void DrawWireframeTriangle(Vector2 pointA, Vector2 pointB, Vector2 pointC, Color c)
    {
        DrawLine(pointA, pointB, c);
        DrawLine(pointB, pointC, c);
        DrawLine(pointC, pointA, c);
    }

    public void DrawTriangle(Vector2 pointA, Vector2 pointB, Vector2 pointC, Color c)
    {
        if (pointB.Y < pointA.Y) (pointA, pointB) = (pointB, pointA);
        if (pointC.Y < pointA.Y) (pointA, pointC) = (pointC, pointA);
        if (pointC.Y < pointB.Y) (pointC, pointB) = (pointB, pointC);
        var values01 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointB.Y, pointB.X).SkipLast(1).ToArray();
        var values12 = RasterizerLogic.Interpolate(pointB.Y, pointB.X, pointC.Y, pointC.X);
        var values02 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointC.Y, pointC.X);
        var values012 = values01.Concat(values12).ToArray();
        var m = (int)Math.Floor((double)values02.Length / 2);
        double[] xLeft;
        double[] xRight;
        if (values02[m] < values012[m])
        {
            xLeft = values02;
            xRight = values012;
        }
        else
        {
            xLeft = values012;
            xRight = values02;
        }

        for (double y = pointA.Y; y < pointC.Y; y++)
        for (var x = xLeft[(int)Math.Floor(y - pointA.Y)]; x < xRight[(int)Math.Floor(y - pointA.Y)]; x++)
            WriteToPixel((int)Math.Round(x), (int)Math.Round(y), c);
    }

    public void DrawVisibleTriangle(Vector2 pointA, Vector2 pointB, Vector2 pointC, Color c, double[] hs,
        Instance instance, Vector3 normal)
    {
        if (pointB.Y < pointA.Y)
        {
            (pointA, pointB) = (pointB, pointA);
            (hs[0], hs[1]) = (hs[1], hs[0]);
        }

        if (pointC.Y < pointA.Y)
        {
            (pointA, pointC) = (pointC, pointA);
            (hs[0], hs[2]) = (hs[2], hs[0]);
        }

        if (pointC.Y < pointB.Y)
        {
            (pointC, pointB) = (pointB, pointC);
            (hs[2], hs[1]) = (hs[1], hs[2]);
        }

        double pointAi = 0;
        double pointBi = 0;
        double pointCi = 0;

        var v1 = new Vector3((float)(pointA.X * ViewportWidth / (PixelWidth * DistanceOfViewport * hs[0])),
            (float)(pointA.Y * ViewportHeight / (PixelHeight * DistanceOfViewport * hs[0])), (float)hs[0]);
        var v2 = new Vector3((float)(pointB.X * ViewportWidth / (PixelWidth * DistanceOfViewport * hs[1])),
            (float)(pointB.Y * ViewportHeight / (PixelHeight * DistanceOfViewport * hs[1])), (float)hs[2]);
        var v3 = new Vector3((float)(pointC.X * ViewportWidth / (PixelWidth * DistanceOfViewport * hs[2])),
            (float)(pointC.Y * ViewportHeight / (PixelHeight * DistanceOfViewport * hs[2])), (float)hs[1]);

        foreach (var l in _lights)
        {
            pointAi += l.computeLightingOnPoint(v1, normal, instance.Specular, _cameraPosition, _cameraMatrix);
            pointBi += l.computeLightingOnPoint(v2, normal, instance.Specular, _cameraPosition, _cameraMatrix);
            pointCi += l.computeLightingOnPoint(v3, normal, instance.Specular, _cameraPosition, _cameraMatrix);
        }


        var values01 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointB.Y, pointB.X, activateLog).SkipLast(1).ToArray();
        var hValues01 = RasterizerLogic.Interpolate(pointA.Y, hs[0], pointB.Y, hs[1]).SkipLast(1).ToArray();
        var iValues01 = RasterizerLogic.Interpolate(pointA.Y, pointAi, pointB.Y, pointBi).SkipLast(1)
            .ToArray();

        var values12 = RasterizerLogic.Interpolate(pointB.Y, pointB.X, pointC.Y, pointC.X, activateLog);
        var hValues12 = RasterizerLogic.Interpolate(pointB.Y, hs[1], pointC.Y, hs[2]);
        var iValues12 = RasterizerLogic.Interpolate(pointB.Y, pointBi, pointC.Y, pointCi);

        var values02 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointC.Y, pointC.X);
        var hValues02 = RasterizerLogic.Interpolate(pointA.Y, hs[0], pointC.Y, hs[2]);
        var iValues02 = RasterizerLogic.Interpolate(pointA.Y, pointAi, pointC.Y, pointCi);

        var values012 = values01.Concat(values12).ToArray();
        var hValues012 = hValues01.Concat(hValues12).ToArray();
        var iValues012 = iValues01.Concat(iValues12).ToArray();
        
        var m = (int)Math.Floor((double)values02.Length / 2);
        double[] xLeft;
        double[] hLeft;
        double[] iLeft;
        double[] xRight;
        double[] hRight;
        double[] iRight;
        if (values02[m] < values012[m])
        {
            xLeft = values02;
            hLeft = hValues02;
            iLeft = iValues02;
            xRight = values012;
            hRight = hValues012;
            iRight = iValues012;
        }
        else
        {
            xLeft = values012;
            hLeft = hValues012;
            iLeft = iValues012;
            xRight = values02;
            hRight = hValues02;
            iRight = iValues02;
        }
        
        var yMin = (int)Math.Ceiling(pointA.Y);
        var yMax = (int)Math.Floor(pointC.Y);

        for (var y = yMin; y <= yMax; ++y)
        {
            var yIndex = y - (int)Math.Round(pointA.Y);

            var hSegment = RasterizerLogic.Interpolate(
                xLeft[yIndex], hLeft[yIndex],
                xRight[yIndex], hRight[yIndex]);
            var iSegment = RasterizerLogic.Interpolate(xLeft[yIndex], iLeft[yIndex], xRight[yIndex], iRight[yIndex]);
            
            var xl = (int)Math.Ceiling(xLeft[yIndex]);
            var xr = (int)Math.Floor(xRight[yIndex]);

            for (var x = xl; x <= xr; ++x) 
            {
                var z = hSegment[x - xl];
                var i = iSegment[x - xl];
                
                if (y > -PixelHeight / 2 && y < PixelHeight / 2 &&
                    x > -PixelWidth / 2 && x < PixelWidth / 2)
                {
                    var bufX = x + PixelWidth / 2;
                    var bufY = y + PixelHeight / 2;

                    
                    if (z < _depthBuffer[bufY * PixelWidth + bufX])
                    {
                        _depthBuffer[bufY * PixelWidth + bufX] = z;
                        WriteToPixelWithLight(x, y, c, (float)i);
                    }
                }
            }
        }
    }

    public Vector2 ViewportToCanvas(Vector2 pos)
    {
        return new Vector2(pos.X * PixelWidth / ViewportWidth, pos.Y * PixelHeight / ViewportHeight);
    }

    public (Vector2, double) ProjectVertex(Vector3 pos)
    {
        return (ViewportToCanvas(new Vector2(pos.X * DistanceOfViewport / pos.Z, pos.Y * DistanceOfViewport / pos.Z)),
            pos.Z);
    }

    public void RenderInstance(Instance instance)
    {
        var instanceTransform = Matrix.CreateScale((float)instance.Scale) * Matrix.CreateFromQuaternion(instance.Rot) *
                                Matrix.CreateTranslation(instance.Pos.X, instance.Pos.Y, instance.Pos.Z);
        var (projected, depthBuffer, triangles, normals) = ClipTriangles(instance, instanceTransform);
        if (projected == null || triangles == null) return;

        for (var i = 0; i < triangles.Length; i++)
        {
            var triangle = triangles[i];
            DrawVisibleTriangle(projected[triangle[0]], projected[triangle[1]], projected[triangle[2]], instance.C,
                new[]
                {
                    depthBuffer[triangle[0]],
                    depthBuffer[triangle[1]],
                    depthBuffer[triangle[2]]
                }, instance, normals[i]);
        }
    }

    public (Vector2[], double[], int[][], Vector3[]) ClipTriangles(Instance instance, Matrix instanceTransform)
    {
        var vertices = new List<Vector4>();
        var normals = new List<Vector3>();
        var triangles = new int[instance.Model.Triangles.Length][];
        instance.Model.Triangles.CopyTo(triangles, 0);
        foreach (var t in instance.Model.Vertices)
            vertices.Add(Vector4.Transform(t, Matrix.Multiply(instanceTransform, _cameraMatrix)));
        foreach (var plane in _cameraPlanes)
        {
            var signedDistance = RasterizerLogic.SignedDistance(Vector4.Transform(instance.Pos, _cameraMatrix), plane);
            if (instance.Model.Radius > signedDistance)
            {
                if (instance.Model.Radius + signedDistance > 0)
                {
                    var temp = new int[triangles.Length][];
                    triangles.CopyTo(temp, 0);
                    foreach (var triangle in temp)
                    {
                        (vertices, triangles) = ClipTriangleAgainstPlane(triangle, vertices, triangles, plane);
                        if (vertices == null || triangles == null) return (null, null, null, null);
                    }
                }
                else
                {
                    return (null, null, null, null);
                }
            }
        }

        var result = new Vector2[vertices.Count()];
        var depthBuffer = new double[vertices.Count()];
        for (var i = 0; i < vertices.Count(); i++)
            (result[i], depthBuffer[i]) = ProjectVertex(new Vector3(vertices[i].X / vertices[i].W,
                vertices[i].Y / vertices[i].W, vertices[i].Z / vertices[i].W));

        foreach (var triangle in triangles)
            normals.Add(RasterizerLogic.ComputeNormal(
                new Vector3(vertices[triangle[0]].X / vertices[triangle[0]].W,
                    vertices[triangle[0]].Y / vertices[triangle[0]].W,
                    vertices[triangle[0]].Z / vertices[triangle[0]].W),
                new Vector3(vertices[triangle[1]].X / vertices[triangle[1]].W,
                    vertices[triangle[1]].Y / vertices[triangle[1]].W,
                    vertices[triangle[1]].Z / vertices[triangle[1]].W),
                new Vector3(vertices[triangle[2]].X / vertices[triangle[2]].W,
                    vertices[triangle[2]].Y / vertices[triangle[2]].W,
                    vertices[triangle[2]].Z / vertices[triangle[2]].W)));
        return (result, depthBuffer, triangles, normals.ToArray());
    }

    public (List<Vector4>, int[][]) ClipTriangleAgainstPlane(int[] triangle, List<Vector4> vertices, int[][] triangles,
        Plane plane)
    {
        var triangleList = new List<int[]>();
        var d0 = RasterizerLogic.SignedDistance(vertices[triangle[0]], plane);
        var d1 = RasterizerLogic.SignedDistance(vertices[triangle[1]], plane);
        var d2 = RasterizerLogic.SignedDistance(vertices[triangle[2]], plane);
        if (d0 <= 0 && d1 <= 0 && d2 <= 0)
        {
            foreach (var tri in triangles)
                if (!tri.Equals(triangle))
                    triangleList.Add(tri);
            return (vertices, triangleList.ToArray());
        }

        var numAbove0 = 0;
        if (d0 >= 0) numAbove0++;

        if (d1 >= 0) numAbove0++;

        if (d2 >= 0) numAbove0++;

        if (numAbove0 == 3) return (vertices, triangles);

        var v0 = new Vector3(vertices[triangle[0]].X / vertices[triangle[0]].W,
            vertices[triangle[0]].Y / vertices[triangle[0]].W, vertices[triangle[0]].Z / vertices[triangle[0]].W);
        var v1 = new Vector3(vertices[triangle[1]].X / vertices[triangle[1]].W,
            vertices[triangle[1]].Y / vertices[triangle[1]].W, vertices[triangle[1]].Z / vertices[triangle[1]].W);
        var v2 = new Vector3(vertices[triangle[2]].X / vertices[triangle[2]].W,
            vertices[triangle[2]].Y / vertices[triangle[2]].W, vertices[triangle[2]].Z / vertices[triangle[2]].W);

        if (numAbove0 == 2)
        {
            foreach (var tri in triangles)
                if (!tri.Equals(triangle))
                    triangleList.Add(tri);
            if (d0 < 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v0, v1, plane));
                vertices.Add(RasterizerLogic.Intersect(v0, v2, plane));
                triangleList.Add(new[]
                {
                    triangle[1], vertices.Count - 2, triangle[2]
                });
                triangleList.Add(new[]
                {
                    triangle[2], vertices.Count - 2, vertices.Count - 1
                });
                return (vertices, triangleList.ToArray());
            }

            if (d1 < 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v1, v0, plane));
                vertices.Add(RasterizerLogic.Intersect(v1, v2, plane));
                triangleList.Add(new[]
                {
                    triangle[0], vertices.Count - 2, triangle[2]
                });
                triangleList.Add(new[]
                {
                    triangle[2], vertices.Count - 2, vertices.Count - 1
                });
                return (vertices, triangleList.ToArray());
            }

            if (d2 < 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v2, v0, plane));
                vertices.Add(RasterizerLogic.Intersect(v2, v1, plane));
                triangleList.Add(new[]
                {
                    triangle[0], vertices.Count - 2, triangle[1]
                });
                triangleList.Add(new[]
                {
                    triangle[1], vertices.Count - 2, vertices.Count - 1
                });
                return (vertices, triangleList.ToArray());
            }
        }

        if (numAbove0 == 1)
        {
            foreach (var tri in triangles)
                if (!tri.Equals(triangle))
                    triangleList.Add(tri);
            if (d0 >= 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v0, v1, plane));
                vertices.Add(RasterizerLogic.Intersect(v0, v2, plane));
                triangleList.Add(new[] { triangle[0], vertices.Count - 2, vertices.Count() - 1 });
                return (vertices, triangleList.ToArray());
            }

            if (d1 >= 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v1, v0, plane));
                vertices.Add(RasterizerLogic.Intersect(v1, v2, plane));
                triangleList.Add(new[] { triangle[1], vertices.Count - 2, vertices.Count() - 1 });
                return (vertices, triangleList.ToArray());
            }

            if (d2 >= 0)
            {
                vertices.Add(RasterizerLogic.Intersect(v2, v1, plane));
                vertices.Add(RasterizerLogic.Intersect(v2, v0, plane));
                triangleList.Add(new[] { triangle[2], vertices.Count - 2, vertices.Count() - 1 });
                return (vertices, triangleList.ToArray());
            }
        }

        return (null, null);
    }
}