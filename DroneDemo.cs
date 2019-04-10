
using System;

using Fusee.Base.Common;

using Fusee.Base.Core;

using Fusee.Engine.Common;

using Fusee.Engine.Core;

using Fusee.Math.Core;

using Fusee.Serialization;

using static Fusee.Engine.Core.Input;

using static Fusee.Engine.Core.Time;

using Fusee.Engine.GUI;

using Fusee.Xene;

using System.Linq;

using System.Collections.Generic;

namespace FuseeApp

{




    public enum CameraType
    {
        // Free world cam
        FREE = 0,

        // Attached to drone, mouse move rotates around drone, arrow keys moves drone

        FOLLOW,

        // Free cam follows drone, mouselook & wasd steers drone (e.g. Jetfighter)
        DRONE,
        //Resets Camera
        Reset


    }

    internal class Drone
    {
        // fields 

        private float3 _position;
        private float3 _rotation;
        private float3 _yaw;
        private float ldle;
        private float _RotationSpeed;
        private float height;
        private Quaternion Orientation;
        private float3 _scale;
        private float speedx;
        private float speedz;
        private float d = 5;
        private float Yaw;
        private float Pitch;
        public float4x4 view;
        public SceneNodeContainer DroneRoot
        {
            get
            {
                return _cnt;
            }
        }

        public float RotationSpeed
        {
            get
            {
                return _RotationSpeed;
            }
            set
            {
                _RotationSpeed = value;
            }
        }
        public static float i = 0;

        private SceneNodeContainer _cnt;
        public InputDevice _gamepad;

        public Drone(SceneNodeContainer cnt, InputDevice gamepad)
        {
            _cnt = cnt;
            _gamepad = gamepad;
        }

        public float3 Position
        {

            get
            {
                return DroneRoot.GetTransform().Translation;
            }
            set
            {
              _position = new float3(value);
            }
            
        }
        public float3 Rotation
        {
            get
            {
                return _yaw;
            }
            set
            {
                _yaw = value;
            }
        }
        public float3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = new float3(value);
            }
        }
        public float idle
        {
            get
            {
                return ldle;
            }
            set
            {
                ldle = value;
            }

        }
        public void Idle()
        {

            Random random = new Random();
            int rN = random.Next(0, 3);

            if (idle <= 0.5f)
            {
                _position.y += 0.0015f;

                idle += 0.004f;
            }
            if (idle > 0.5 && idle <= 1)
            {
                _position.y -= 0.0015f;
                idle += 0.004f;
            }
            if (idle >= 0.99f)
                idle = 0.01f;
        }

        public void Tilt()
        {
            if (Keyboard.WSAxis == 0)
            {
                if (_rotation.x > 0)
                    _rotation.x -= 0.01f;
                if (_rotation.x < 0)
                    _rotation.x += 0.01f;
            }

            if (Keyboard.ADAxis == 0)
            {
                if (_rotation.z > 0)
                    _rotation.z -= 0.005f;
                if (_rotation.z < 0)
                    _rotation.z += 0.005f;
            }

            if (-Keyboard.WSAxis < 0)
                if (_rotation.x > -0.2)
                    _rotation.x -= 0.005f;

            if (-Keyboard.WSAxis > 0)
                if (_rotation.x < 0.2)
                    _rotation.x += 0.005f;
            // Drone Tilt while Moving
            if (-Keyboard.ADAxis < 0)
                if (_rotation.z < 0.2)
                    _rotation.z += 0.01f;

            if (-Keyboard.ADAxis > 0)
                if (_rotation.z > -0.2)
                    _rotation.z -= 0.01f;

            if (_gamepad.GetAxis(1) != 0)
                _rotation.x = -_gamepad.GetAxis(1) * 0.25f;

            if (_gamepad.GetAxis(0) != 0)
                _rotation.z = _gamepad.GetAxis(0) * 0.25f;

        }
        public Quaternion orientation(float Yaw, float Pitch)
        {
            Orientation = Quaternion.FromAxisAngle(float3.UnitY, Yaw) *
                            Quaternion.FromAxisAngle(float3.UnitX, Pitch);
            return Orientation;
        }

        public void MoveRotor()
        {
            var rbl = DroneRoot.Children.FindNodes(node => node.Name == "Rotor back left")?.FirstOrDefault()?.GetTransform();
            var rfl = DroneRoot.Children.FindNodes(node => node.Name == "Rotor front left")?.FirstOrDefault()?.GetTransform();
            var rfr = DroneRoot.Children.FindNodes(node => node.Name == "Rotor front right")?.FirstOrDefault()?.GetTransform();
            var rbr = DroneRoot.Children.FindNodes(node => node.Name == "Rotor back right")?.FirstOrDefault()?.GetTransform();
            
            if (i <= 23)
                i += 0.05f;

                


            rbl.Rotation.y = i * TimeSinceStart;
            rfl.Rotation.y = i * TimeSinceStart;
            rfr.Rotation.y = -i * TimeSinceStart;
            rbr.Rotation.y = -i * TimeSinceStart;

        }
        public float4x4 Update(CameraType _cameraType)
        {
            Rotation = DroneRoot.GetTransform().Rotation;
            _scale = DroneRoot.GetTransform().Scale;
            Idle();
            Tilt();
            MoveRotor();
            var camPosOld = new float3(Position.x, Position.y + 1, Position.z - d);
            var DroneposOld = DroneRoot.GetTransform().Translation;
            float mouse = 0;
            var rot = _rotation.y;
            if (Mouse.LeftButton)
            {
                 mouse = (Mouse.XVel * 0.0005f);
            }

            _rotation.y = _rotation.y + mouse - _gamepad.GetAxis(4) * DeltaTime + _gamepad.GetAxis(5) * DeltaTime;

            if (Keyboard.WSAxis == 0)
                speedx = 0.02f;

            if (Keyboard.ADAxis == 0)
                speedz = 0.02f;

            // if (Keyboard.WSAxis != 0)
                if (speedx <= 0.5f)
                    speedx += 0.005f;

            if (Keyboard.ADAxis != 0)
                if (speedz <= 0.5f)
                    speedz += 0.005f;


            float posVelX = -Keyboard.WSAxis * speedx * (DeltaTime * 15) - _gamepad.GetAxis(1) * DeltaTime * 8;
            float posVelZ = -Keyboard.ADAxis * speedz * (DeltaTime * 15) - _gamepad.GetAxis(0) * DeltaTime * 8;
            float3 newPos = DroneposOld;

            newPos += float3.Transform(float3.UnitX * posVelZ, orientation(_rotation.y, 0));
            newPos += float3.Transform(float3.UnitZ * posVelX, orientation(_rotation.y, 0));

            // Height
            if (Keyboard.GetKey(KeyCodes.R) || _gamepad.GetButton(7))
                newPos.y += 0.1f;
            if (Keyboard.GetKey(KeyCodes.F) || _gamepad.GetButton(6))
            {
                height = 0.1f;
                if (newPos.y <= 0.5f)
                    height = 0;
                newPos.y -= height;
            }

            Position = newPos;

            var posVec = float3.Normalize(camPosOld - Position);
            var camposnew = Position + posVec * d;
            Yaw += _gamepad.GetAxis(2) *DeltaTime;
            Pitch += _gamepad.GetAxis(3) * DeltaTime;
            if (Mouse.RightButton)
            {
                Yaw += Mouse.XVel * 0.0005f;
                Pitch += Mouse.YVel * 0.0005f;
            }
            if (Keyboard.GetKey(KeyCodes.Z))
            {
                Scale = new float3(Scale.x + 0.01f, Scale.y + 0.01f, Scale.z + 0.01f);
                d += 0.1f;
            }
            if (Keyboard.GetKey(KeyCodes.X))
                if (Scale.y + Scale.x + Scale.z >= 0.03){
                    Scale = new float3(Scale.x - 0.01f, Scale.y - 0.01f, Scale.z - 0.01f);
                d -= 0.1f;
            }

            if (_cameraType == CameraType.DRONE)
            {
                view = float4x4.LookAt(
                                                      new float3(DroneposOld) + d * float3.Transform(float3.UnitZ, orientation(rot, -0.3f)),
                                                      new float3(Position),
                                                      float3.UnitY
                                                      );
            }
            if (_cameraType == CameraType.FOLLOW)
            {
                view = float4x4.LookAt(
                                                     new float3(DroneposOld) + d * float3.Transform(float3.UnitZ, orientation(Yaw, Pitch)),
                                                     new float3(Position),
                                                     float3.UnitY
                                                     );
            }

            var Drone = DroneRoot;
            Drone.GetTransform().Translation = _position;
            Drone.GetTransform().Rotation = _rotation;
            Drone.GetTransform().Scale = _scale;
            return view;
        }
    }

    internal class Camera
    {
        public float3 _Position;
        public CameraType _cameraType;
        public float4x4 view;
        public float _Yaw;
        public float _Pitch;
        public InputDevice _gamepad;
        private float _MouseSensitivity;
        public Camera(InputDevice gamepad)
        {
            _gamepad = gamepad;
        }
        public float MovementSpeed
        {
            get
            {
                return MovementSpeed;
            }
            set
            {
                MovementSpeed = value;
            }
        }
        public float MouseSensitivity
        {
            get
            {
                return _MouseSensitivity;
            }
            set
            {
                _MouseSensitivity = value;
            }
        }
        public float Yaw
        {
            get
            {
                float yaw = 0;
                if (Mouse.RightButton)
                    yaw = Mouse.XVel * MouseSensitivity;
            
                    _Yaw += yaw + (_gamepad.GetAxis(2) * DeltaTime);
                return _Yaw;
            }
        }
        public float Pitch
        {
            get
            {
                float pitch = 0;
                if (Mouse.RightButton)
                    pitch = Mouse.YVel * MouseSensitivity;

                    _Pitch +=  pitch + (_gamepad.GetAxis(3) * -DeltaTime);
                return _Pitch;
            }
        }
        public float3 Position
        {
            get
            {
                return _Position;
            }
            set
            {
                _Position = new float3(value);
            }
        }
        public Quaternion Rotation
        {
            get
            {
                return Rotation;
            }
            set
            {
                Rotation = value;
            }
        }
        public CameraType cameraType
        {
            get
            {
                return _cameraType;
            }

            set
            {
                _cameraType = ((int)_cameraType + 1) <= 2 ? value : 0;

            }

        }
        public float3 ForwardVector
        {
            get
            {
                var Orientation = Quaternion.FromAxisAngle(float3.UnitY, Yaw) * Quaternion.FromAxisAngle(float3.UnitX, Pitch);
                return float3.Transform(float3.UnitZ, Orientation);
            }
        }
        public float4x4 ViewMatrix
        {
            get
            {
                return float4x4.LookAt(Position, Position + ForwardVector, float3.UnitY);;
            }
        }
        public void SetCameraType()
        {
            cameraType++;
            Diagnostics.Log("Der Camera Typ ist " + _cameraType);
        }
        public void SetPositionLocally(float3 pos)
        {
            view = ViewMatrix;
        }
        public float4x4 Update()
        {   
            MouseSensitivity = 0.00005f;
            if (Keyboard.IsKeyUp(KeyCodes.Q) || _gamepad.GetButton(2))
                SetCameraType();
            if (cameraType == CameraType.FREE)
            {
                Position += float3.Transform(float3.UnitX * (Keyboard.ADAxis + _gamepad.GetAxis(0)) * DeltaTime * 8, Quaternion.FromAxisAngle(float3.UnitY, Yaw) * Quaternion.FromAxisAngle(float3.UnitX, Pitch));
                Position += float3.Transform(float3.UnitZ * (Keyboard.WSAxis + _gamepad.GetAxis(1)) * DeltaTime * 8, Quaternion.FromAxisAngle(float3.UnitY, Yaw) * Quaternion.FromAxisAngle(float3.UnitX, Pitch));
                if (_cameraType == CameraType.FREE)
                    SetPositionLocally(Position);
            }
            return view;

        }

    }


    [FuseeApplication(Name = "Droneflight", Description = "Droneflight Demo")]

    public class DroneDemo : RenderCanvas

    {

        private Camera _camera;
        private Drone _drone;
        private float4x4 view;

        // Variables init

        private const float RotationSpeed = 7;
        public SceneContainer _droneScene;
        private SceneRenderer _sceneRenderer;
        private SceneRenderer _guiRenderer;
        private SceneNodeContainer DroneRoot;

        private CameraType _cameraType;
        private SceneContainer _gui;
        public String _text;
        private InputDevice _gamepad;
        private float wait;




        // Init is called on startup. 
        public override void Init()

        {

            // Set the clear color for the backbuffer to white (100% intensity in all color channels R, G, B, A).

            RC.ClearColor =
            new float4(0.7f, 0.9f, 0.5f, 1);
            _gui = CreateGui();


            // Load the drone model
            _droneScene = AssetStorage.Get<SceneContainer>("GroundNoMat.fus");
            var droneBody = _droneScene.Children.FindNodes(node => node.Name == "Body")?.FirstOrDefault();
            
            _gamepad = Devices.First(dev => dev.Category == DeviceCategory.GameController);

            _drone = new Drone(droneBody, _gamepad);

            _camera = new Camera(_gamepad);

            


            // Wrap a SceneRenderer around the model.

            _sceneRenderer = new SceneRenderer(_droneScene);
            _guiRenderer = new SceneRenderer(_gui);

            DroneRoot = _droneScene.Children.FindNodes(node => node.Name == "Body")?.FirstOrDefault();


            

        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()

        {



            // Clear the backbuffer

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            // Switch between Drone and Freefly            
            if (_cameraType == CameraType.Reset)
                _cameraType = CameraType.FREE;
            
            wait++;
            
            if (wait >= 25)
                if (Keyboard.IsKeyUp(KeyCodes.Q)||_gamepad.GetButton(2)){
                _cameraType++;
                wait = 0;

            Diagnostics.Log(_cameraType);
            }

            if(_gamepad.GetButton(4))
                TimeScale = 0;
            if(_gamepad.GetButton(5))
                TimeScale = 1;

            if (_cameraType == CameraType.FREE)
                view = _camera.Update();
            if (_cameraType == CameraType.FOLLOW || _cameraType == CameraType.DRONE)
                view = _drone.Update(_cameraType);

            RC.View = view;

            

            // Render the scene loaded in Init()

            _sceneRenderer.Render(RC);

            _guiRenderer.Render(RC);



            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.

            Present();

        }

        private InputDevice Creator(IInputDeviceImp device)

        {

            throw new NotImplementedException();

        }
        private SceneContainer CreateGui()
        {
            var vsTex = AssetStorage.Get<string>("texture.vert");
            var psTex = AssetStorage.Get<string>("texture.frag");
            var color = ColorUint.Tofloat4(ColorUint.Greenery);

            var fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            var _guiLatoBlack = new FontMap(fontLato, 36);

            // Initialize the information text line.
            var textToDisplay = "Drone Simulation";

            var text = new TextNodeContainer(
                textToDisplay,
                "ButtonText",
                vsTex,
                psTex,
                new MinMaxRect
                {
                    Min = new float2(0, 0),
                    Max = new float2(1, 0)
                },
                new MinMaxRect
                {
                    Min = new float2(4f, 0f),
                    Max = new float2(-4, 0.5f)
                },
                _guiLatoBlack,
                color,
                 0.3f);

            var canvas = new CanvasNodeContainer(
                "Canvas",
                CanvasRenderMode.SCREEN,
                new MinMaxRect
                {
                    Min = new float2(-8, -4.5f),
                    Max = new float2(8, 4.5f)
                }
            )
            {
                Children = new List<SceneNodeContainer>()
                {
                    //Simple Texture Node, contains the fusee logo
                    text
                }
            };

            return new SceneContainer
            {
                Children = new List<SceneNodeContainer>
                {
                    //Add canvas.
                    canvas
                }
            };
        }


        // Is called when the window was resized
        public override void Resize()

        {

            // Set the new rendering area to the entire new windows size

            RC.Viewport(0,
            0, Width,
            Height);



            // Create a new projection matrix generating undistorted images on the new aspect ratio.

            var aspectRatio =
            Width / (float)Height;



            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio

            // Front clipping happens at 0.01 (Objects nearer than 1 world unit get clipped)

            // Back clipping happens at 200 (Anything further away from the camera than 200 world units gets clipped, polygons will be cut)

            var projection =
            float4x4.CreatePerspectiveFieldOfView(M.PiOver4,
            aspectRatio, 1,
            20000);

            RC.Projection = projection;

        }

    }

}
