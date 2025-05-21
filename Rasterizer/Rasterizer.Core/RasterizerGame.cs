using System;
using Rasterizer.Core.Localization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Rasterizer.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    public class RasterizerGame : Game
    {
        // Resources for drawing.
        
        private Instance CUBE;
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch _spriteBatch;
        private Quaternion _cameraRotation = Quaternion.Identity;
        private Vector4 _cameraPosition = new Vector4(0,0,0,1);
        public Dictionary<string, Model> Models = new Dictionary<string, Model>();
        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop =
            OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the game. Configures platform-specific settings, 
        /// initializes services like settings and leaderboard managers, and sets up the 
        /// screen manager for screen transitions.
        /// </summary>
        private const float ViewportWidth = 1;
        private const float DistanceOfViewport = 1;
        private const float ViewportHeight = 1;
        private Texture2D _pixelTexture;
        private Color[] _pixelBuffer;
        private const int PixelWidth = 640;
        private const int PixelHeight = 640;
        private Matrix _cameraMatrix = Matrix.Identity;
        private Matrix _projectionMatrix = Matrix.Identity;
        private double[][] _depthBuffer;
        private Vector3[] _cameraPlanesVectors = new Vector3[]
        {
            new Vector3(0,0,1),
            new Vector3(1, 0, 0.5f),
            new Vector3(-1, 0, 0.5f),
            new Vector3(0, 1, 0.5f),
            new Vector3(0, -1, 0.5f)
        };

        private Plane[] _cameraPlanes = new Plane[5];
        public RasterizerGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Share GraphicsDeviceManager as a service.
            Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);

            Content.RootDirectory = "Content";
            
            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations =
                DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            graphicsDeviceManager.PreferredBackBufferHeight = PixelHeight;
            graphicsDeviceManager.PreferredBackBufferWidth = PixelWidth;
        }

        /// <summary>
        /// Initializes the game, including setting up localization and adding the 
        /// initial screens to the ScreenManager.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();
            for (int i = 0; i < 5; i++)
            {
                _cameraPlanesVectors[i].Normalize();
                if (i==0)
                {
                    _cameraPlanes[i] = new Plane(_cameraPlanesVectors[i], -DistanceOfViewport);
                }
                else
                {
                    _cameraPlanes[i] = new Plane(_cameraPlanesVectors[i], 0);
                }
            }
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixelTexture = new Texture2D(GraphicsDevice, PixelWidth, PixelHeight);
            _pixelBuffer = new Color[PixelWidth * PixelHeight];
            _depthBuffer = new double[PixelWidth][];
            for (int i = 0; i < PixelWidth; i++)
            {
                _depthBuffer[i] = new double[PixelHeight];
            }
            Models.Add("Cube", new Model(new Vector4[]
            {
                new Vector4(1,  1,  1, 1),
                new Vector4(-1,  1,  1, 1),
                new Vector4(-1, -1, 1, 1),
                new Vector4(1, -1, 1, 1),
                new Vector4(1, 1, -1, 1),
                new Vector4(-1, 1, -1, 1),
                new Vector4(-1, -1, -1, 1),
                new Vector4(1, -1, -1,1)
            }, new int[][]
            {
                [0,1,2],
                [0,2,3],
                [4,0,3],
                [4,3,7],
                [5,4,7],
                [5,7,6],
                [1,5,6],
                [1,6,2],
                [4,5,1],
                [4,1,0],
                [2,6,7],
                [2,7,3]
            }, double.Sqrt(2)));
            _projectionMatrix[0, 0] = DistanceOfViewport * PixelWidth / ViewportWidth;
            _projectionMatrix[1, 1] = DistanceOfViewport * PixelHeight / ViewportHeight;
            _projectionMatrix[3, 3] = 0;
            CUBE = new Instance(new Vector4(-1, 2,7, 1), Quaternion.Identity, 1, Models["Cube"], Color.White);
        }

        /// <summary>
        /// Loads game content, such as textures and particle systems.
        /// </summary>

        /// <summary>
        /// Updates the game's logic, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for game updates.
        /// </param>
        protected override void Update(GameTime gameTime)
        {   
            Matrix cameraTransform = Matrix.Identity;
            cameraTransform[0, 3] = _cameraPosition.X;
            cameraTransform[1, 3] = _cameraPosition.Y;
            cameraTransform[2, 3] = _cameraPosition.Z;
            _cameraMatrix = Matrix.Invert(Matrix.CreateFromQuaternion(_cameraRotation)) * Matrix.Invert(cameraTransform);
            // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            for (int y = 0; y < PixelHeight; y++)
            {
                for (int x = 0; x < PixelWidth; x++)
                {
                    int index = y * PixelWidth + x;
                    _pixelBuffer[index] = Color.Black;
                    _depthBuffer[x][y] = Double.MaxValue;
                }
            }
            //float deltaAngle = 0.001f * gameTime.ElapsedGameTime.Milliseconds;
            //Quaternion increment = Quaternion.CreateFromAxisAngle(Vector3.UnitX, deltaAngle);
            //CUBE.Rot = Quaternion.Normalize(Quaternion.Concatenate(CUBE.Rot, increment));
            CUBE.Pos += new Vector4(0, 0, -(float)0.001*gameTime.ElapsedGameTime.Milliseconds, 0);
            RenderInstance(CUBE);
            // Apply your rasterization logic / pixel manipulation here
            // ...
            
            // Copy the CPU-side pixel data to the GPU texture (1 call per frame).
            _pixelTexture.SetData(_pixelBuffer);

            base.Update(gameTime);

        }

        /// <summary>
        /// Draws the game's graphics, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for rendering.
        /// </param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
    
            // Display at original size or scale up to desired resolution
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, PixelWidth, PixelHeight), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public void WriteToPixel(int x, int y, Color c)
        {
            int y1 = y + PixelHeight / 2;
            int x1 = x + PixelWidth / 2;
            if (y > -1 * PixelHeight / 2 & y < PixelHeight/2 & x > -1 * PixelWidth / 2 & x < PixelWidth/2  )
            {
                _pixelBuffer[y1 * PixelWidth + x1] = c;
            }
        }

        public void WriteToPixel(Vector2 v, Color c)
        {
            WriteToPixel((int)double.Round(v.X), (int)double.Round(v.Y), c);
        }

        public void DrawLine(Vector2 pointA, Vector2 pointB, Color c)
        {
            if (Math.Abs(pointB.X-pointA.X) > Math.Abs(pointB.Y-pointA.Y))
            {
                if (pointA.X > pointB.X)
                {
                    (pointA, pointB) = (pointB, pointA);
                }
                double[] values = RasterizerLogic.Interpolate(pointA.X, pointA.Y,pointB.X, pointB.Y);
                for (int x = (int)double.Round(pointA.X); x < (int)double.Round(pointB.X); x++)
                {
                    WriteToPixel(x, (int)double.Round(values[(int)double.Round(x-pointA.X)]), c);
                }
            }
            else
            {
                if (pointA.Y > pointB.Y)
                {
                    (pointA, pointB) = (pointB, pointA);
                }
                double[] values = RasterizerLogic.Interpolate(pointA.Y, pointA.X,pointB.Y, pointB.X);
                for (int y = (int)double.Round(pointA.Y); y < (int)double.Round(pointB.Y); y++)
                {
                    WriteToPixel((int)double.Round(values[(int)double.Round(y-pointA.Y)]), y, c);
                }
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
            if (pointB.Y < pointA.Y) { (pointA, pointB) = (pointB, pointA); }
            if (pointC.Y < pointA.Y) { (pointA, pointC) = (pointC, pointA); }
            if (pointC.Y < pointB.Y) { (pointC, pointB) = (pointB, pointC); }
            double[] values01 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointB.Y, pointB.X).SkipLast(1).ToArray();
            double[] values12 = RasterizerLogic.Interpolate(pointB.Y, pointB.X, pointC.Y, pointC.X);
            double[] values02 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointC.Y, pointC.X);
            double[] values012 = values01.Concat(values12).ToArray();
            int m = (int)Math.Floor((double)values02.Length / 2);
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
            {
                for (double x = xLeft[(int)Math.Floor(y-pointA.Y)]; x < xRight[(int)Math.Floor(y-pointA.Y)]; x++)
                {
                    WriteToPixel((int)Math.Round(x),(int)Math.Round(y),c);
                }
            }
        }

        public void DrawVisibleTriangle(Vector2 pointA, Vector2 pointB, Vector2 pointC, Color c, double[] hs)
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
            double[] values01 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointB.Y, pointB.X).SkipLast(1).ToArray();
            double[] hValues01 = RasterizerLogic.Interpolate(pointA.Y, hs[0], pointB.Y, hs[1]).SkipLast(1).ToArray();
            
            double[] values12 = RasterizerLogic.Interpolate(pointB.Y, pointB.X, pointC.Y, pointC.X);
            double[] hValues12 = RasterizerLogic.Interpolate(pointB.Y, hs[1], pointC.Y, hs[2]);
            
            double[] values02 = RasterizerLogic.Interpolate(pointA.Y, pointA.X, pointC.Y, pointC.X);
            double[] hValues02 = RasterizerLogic.Interpolate(pointA.Y, hs[0], pointC.Y, hs[2]);
            
            double[] values012 = values01.Concat(values12).ToArray();
            double[] hValues012 = hValues01.Concat(hValues12).ToArray();
            
            int m = (int)Math.Floor((double)values02.Length / 2);
            double[] xLeft;
            double[] hLeft;
            double[] xRight;
            double[] hRight;
            if (values02[m] < values012[m])
            {
                xLeft = values02;
                hLeft = hValues02;
                xRight = values012;
                hRight = hValues012;
            }
            else
            {
                xLeft = values012;
                hLeft = hValues012;
                xRight = values02;
                hRight = hValues02;
            }

            for (double y = pointA.Y; y < pointC.Y; y++)
            {
                double[] hSegment = RasterizerLogic.Interpolate(xLeft[(int)Math.Round(y-pointA.Y)],hLeft[(int)Math.Round(y-pointA.Y)],xRight[(int)Math.Round(y-pointA.Y)], hRight[(int)Math.Round(y-pointA.Y)]);
                for (double x = xLeft[(int)Math.Floor(y-pointA.Y)]; x < xRight[(int)Math.Floor(y-pointA.Y)]; x++)
                {
                    double z = hSegment[(int)Math.Round(x-xLeft[(int)Math.Round(y-pointA.Y)])];
                    if (y > -1 * PixelHeight / 2 & y < PixelHeight/2 & x > -1 * PixelWidth / 2 & x < PixelWidth/2  )
                    {
                        if (z < _depthBuffer[(int)Math.Floor(x + PixelWidth/2)][(int)Math.Floor(y + PixelHeight/2)])
                        {
                            WriteToPixel((int)Math.Round(x),(int)Math.Round(y), c);
                            _depthBuffer[(int)Math.Floor(x + PixelWidth/2)][(int)Math.Floor(y + PixelHeight/2)] = z;
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
            return (ViewportToCanvas(new Vector2(pos.X * DistanceOfViewport/ pos.Z, pos.Y * DistanceOfViewport/ pos.Z)), pos.Z);
        }

        public void RenderInstance(Instance instance)
        {
            Matrix instanceTransform = Matrix.CreateScale((float)instance.Scale) * Matrix.CreateFromQuaternion(instance.Rot) * Matrix.CreateTranslation(instance.Pos.X,instance.Pos.Y,instance.Pos.Z);
            var (projected, depthBuffer, triangles) = ClipTriangles(instance, instanceTransform);
            if (projected == null || triangles == null)
            {
                return;
            }
            foreach (int[] triangle in triangles)
            {
                DrawVisibleTriangle(projected[triangle[0]], projected[triangle[1]], projected[triangle[2]], instance.C, new double[]{
                    depthBuffer[triangle[0]],
                    depthBuffer[triangle[1]],
                    depthBuffer[triangle[2]]
                });
                DrawWireframeTriangle(projected[triangle[0]], projected[triangle[1]], projected[triangle[2]],Color.Red);
            }
        }

        public (Vector2[], double[], int[][]) ClipTriangles(Instance instance, Matrix instanceTransform)
        {
            var vertices = new System.Collections.Generic.List<Vector4>();
            int[][] triangles = new int[instance.Model.Triangles.Length][];
            instance.Model.Triangles.CopyTo(triangles,0);
            foreach (var t in instance.Model.Vertices)
            {
                vertices.Add(Vector4.Transform(t,instanceTransform*_cameraMatrix));
            }
            foreach (var plane in _cameraPlanes)
            {
                double signedDistance = RasterizerLogic.SignedDistance(Vector4.Transform(instance.Pos,_cameraMatrix), plane);
                if (instance.Model.Radius > signedDistance)
                {
                    if (instance.Model.Radius + signedDistance > 0)
                    {
                        var temp = new int[triangles.Length][];
                        triangles.CopyTo(temp,0);
                        foreach (var triangle in temp)
                        {
                            (vertices, triangles) = ClipTriangleAgainstPlane(triangle,vertices,triangles, plane);
                            if (vertices == null || triangles == null)
                            {
                                return (null, null, null);
                            }
                        }
                        
                    }
                    else
                    {
                        return (null, null, null);
                    }
                }
            }
            Vector2[] result = new Vector2[vertices.Count()];
            double[] depthBuffer = new double[vertices.Count()];
            for (int i = 0; i < vertices.Count(); i++)
            {
                (result[i],depthBuffer[i]) = ProjectVertex(new Vector3(vertices[i].X/vertices[i].W, vertices[i].Y/vertices[i].W, vertices[i].Z/vertices[i].W));
            }

            return (result, depthBuffer, triangles);
        }

        public (List<Vector4>, int[][]) ClipTriangleAgainstPlane(int[] triangle, List<Vector4> vertices, int[][] triangles, Plane plane)
        {
            List<int[]> triangleList = new List<int[]>();
            double d0 = RasterizerLogic.SignedDistance(vertices[triangle[0]], plane);
            double d1 = RasterizerLogic.SignedDistance(vertices[triangle[1]], plane);
            double d2 = RasterizerLogic.SignedDistance(vertices[triangle[2]], plane);
            if (d0 <= 0 && d1 <= 0 && d2 <= 0)
            {
                foreach (var tri in triangles)
                {
                    if (!tri.Equals( triangle))
                    {
                        triangleList.Add(tri);
                    }
                }
                return (vertices, triangleList.ToArray());
            }

            int numAbove0 = 0;
            if (d0>=0)
            {
                numAbove0++;
            }

            if (d1>=0)
            {
                numAbove0++;
            }

            if (d2 >= 0 )
            {
                numAbove0++;
            }

            if (numAbove0 == 3)
            {
                return (vertices, triangles);
            }

            Vector3 v0 = new Vector3(vertices[triangle[0]].X/vertices[triangle[0]].W, vertices[triangle[0]].Y/vertices[triangle[0]].W, vertices[triangle[0]].Z/vertices[triangle[0]].W);
            Vector3 v1 = new Vector3(vertices[triangle[1]].X/vertices[triangle[1]].W, vertices[triangle[1]].Y/vertices[triangle[1]].W, vertices[triangle[1]].Z/vertices[triangle[1]].W);
            Vector3 v2 = new Vector3(vertices[triangle[2]].X/vertices[triangle[2]].W, vertices[triangle[2]].Y/vertices[triangle[2]].W, vertices[triangle[2]].Z/vertices[triangle[2]].W);
            
            if (numAbove0 == 2)
            {
                foreach (var tri in triangles)
                {
                    if (!tri.Equals( triangle))
                    {
                        triangleList.Add(tri);
                    }
                }
                if (d0 < 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v0,v1,plane));
                    vertices.Add(RasterizerLogic.Intersect(v0,v2,plane));
                    triangleList.Add(new int[]{
                        triangle[1], vertices.Count - 2, triangle[2]
                    });
                    triangleList.Add(new int[]{
                        triangle[2], vertices.Count - 2, vertices.Count - 1
                    });
                    return (vertices, triangleList.ToArray());
                }
                if (d1 < 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v1,v0,plane));
                    vertices.Add(RasterizerLogic.Intersect(v1,v2,plane));
                    triangleList.Add(new int[]{
                        triangle[0], vertices.Count - 2, triangle[2]
                    });
                    triangleList.Add(new int[]{
                        triangle[2], vertices.Count - 2, vertices.Count - 1
                    });
                    return (vertices, triangleList.ToArray());
                }
                if (d2 < 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v2,v0,plane));
                    vertices.Add(RasterizerLogic.Intersect(v2,v1,plane));
                    triangleList.Add(new int[]{
                        triangle[0], vertices.Count - 2, triangle[1]
                    });
                    triangleList.Add(new int[]{
                        triangle[1], vertices.Count - 2, vertices.Count - 1
                    });
                    return (vertices, triangleList.ToArray());
                }
            }

            if (numAbove0 == 1)
            {
                
                foreach (var tri in triangles)
                {
                    if (!tri.Equals( triangle))
                    {
                        triangleList.Add(tri);
                    }
                }
                if (d0 >= 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v0,v1,plane));
                    vertices.Add(RasterizerLogic.Intersect(v0,v2,plane));
                    triangleList.Add(new int[]{triangle[0], vertices.Count - 2, vertices.Count() - 1});
                    return (vertices, triangleList.ToArray());
                }
                if (d1 >= 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v1,v0,plane));
                    vertices.Add(RasterizerLogic.Intersect(v1,v2,plane));
                    triangleList.Add(new int[]{triangle[1], vertices.Count - 2, vertices.Count() - 1});
                    return (vertices, triangleList.ToArray());
                }
                if (d2 >= 0)
                {
                    vertices.Add(RasterizerLogic.Intersect(v2,v1,plane));
                    vertices.Add(RasterizerLogic.Intersect(v2,v0,plane));
                    triangleList.Add(new int[]{triangle[2], vertices.Count - 2, vertices.Count() - 1});
                    return (vertices, triangleList.ToArray());
                }
            }
            return (null, null);
        }
    }
}