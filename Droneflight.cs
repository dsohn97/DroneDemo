
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

    public interface ICamera
    {
        float MovementSpeed { get; set; }

        float MouseSensitivity { get; set; }

        // get => Mouse.XVel;
        float Yaw { get; }

        float Pitch { get; }

        // => SET creates new view matrix
        Quaternion Rotation { get; set; }

        // => SET creates new view matrix
        float3 Position { get; set; }

        float4x4 ViewMatrix { get; }

        CameraType cameraType { get; set; }

        void SetCameraType();

        float3 ForwardVector { get; }

        // direction.normalize()
        // e.g. this.Position = Transform(direction * ammount, orientation(Yaw, Pitch))
        void SetPositionLocally(float3 direction);

        // call this in RenderAFrame()
        float4x4 Update();

    }

    interface IDrone
    {
        // set => Tilt()
        float3 Position { get; set; }
        Quaternion Rotation { get; set; }

        float3 Scale { get; set; }

        float RotationSpeed { get; set; }
        float idle { get; set; }

        void Idle();

        void MoveRotor();


        // tiltTo = position.normalize()
        void Tilt();

        // if(Keyboard.WS != 0) return;
        // void ResetTilt();

        SceneNodeContainer DroneRoot { get; }


        // call this in RenderAFrame()
        // DroneRoot.GetComponent<TransformComponent>().Positon // etc. updaten
        float4x4 Update();

    }



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

    internal class Drone : IDrone
    {
        // fields 

        private float3 _position;
        private float3 _rotation;
        private Quaternion _yaw;
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

        private CameraType _cameraType;
        private TransformComponent _droneRoot;
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

        public Drone(SceneNodeContainer cnt)
        {
            _cnt = cnt;

            _position = DroneRoot.GetTransform().Translation;
            _rotation = DroneRoot.GetTransform().Rotation;
            _scale = DroneRoot.GetTransform().Scale;

        }

        public float3 Position
        {

            get
            {
                return _position;
            }
            set
            {
                _position = new float3(value);
            }
        }
        public Quaternion Rotation
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
            if (i < 35)

                i += 0.05f;
            
            
                rbl.Rotation.y = i * TimeSinceStart;
                rfl.Rotation.y = i * TimeSinceStart;
                rfr.Rotation.y = i * TimeSinceStart;
                rbr.Rotation.y = i * TimeSinceStart;
            
        }
        public float4x4 Update()
        {
            Idle();
            Tilt();
            MoveRotor();
            var camPosOld = new float3(_position.x, _position.y + 1, _position.z - d);
            var DroneposOld = _position;
            if (Mouse.LeftButton)
            {
                _rotation.y = _rotation.y + (Mouse.XVel * 0.0005f);
            }

            if (Keyboard.WSAxis == 0)
                speedx = 0.02f;

            if (Keyboard.ADAxis == 0)
                speedz = 0.02f;

            if (Keyboard.WSAxis != 0)
                if (speedx <= 0.5f)
                    speedx += 0.005f;

            if (Keyboard.ADAxis != 0)
                if (speedz <= 0.5f)
                    speedz += 0.005f;


            float posVelX = -Keyboard.WSAxis * speedx;
            float posVelZ = -Keyboard.ADAxis * speedz;
            float3 newPos = _position;

            newPos += float3.Transform(float3.UnitX * posVelZ, orientation(_rotation.y, 0));
            newPos += float3.Transform(float3.UnitZ * posVelX, orientation(_rotation.y, 0));

            // Height
            if (Keyboard.GetKey(KeyCodes.R))
                newPos.y += 0.1f;
            if (Keyboard.GetKey(KeyCodes.F))
            {
                height = 0.1f;
                if (newPos.y <= 0.5f)
                    height = 0;
                newPos.y -= height;
            }
            var dronePosNew = _position;

            var posVec = float3.Normalize(camPosOld - dronePosNew);
            var camposnew = dronePosNew + posVec * d;
            if (Mouse.RightButton)
            {
                Yaw += Mouse.XVel * 0.0005f;
                Pitch += Mouse.YVel * 0.0005f;
            }
            if (_cameraType == CameraType.DRONE)
            {
              view =  float4x4.LookAt(
                                                    new float3(DroneposOld) + d * float3.Transform(float3.UnitZ, orientation(_rotation.y, -0.3f)),
                                                    new float3(dronePosNew),
                                                    float3.UnitY
                                                    );
            }
            if(_cameraType == CameraType.FOLLOW)
            {
               view = float4x4.LookAt(
                                                    new float3(DroneposOld) + d * float3.Transform(float3.UnitZ, orientation(Yaw, Pitch)),
                                                    new float3(dronePosNew),
                                                    float3.UnitY
                                                    );
            }
            var Drone = DroneRoot;
            Drone.GetTransform().Translation = dronePosNew;
            Drone.GetTransform().Rotation = _rotation;
            Drone.GetTransform().Scale = _scale;
            return view;
            }
    }

    internal class Camera : ICamera
    {
        public float3 _Position;
        public float3 _ForwardVector;
        public CameraType _cameraType;
        public float4x4 view;
        public float _Yaw;
        public float _Pitch;
        public Camera()
        {

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
                return MouseSensitivity;
            }
            set
            {
                MouseSensitivity = value;
            }
        }
        public float Yaw
        {
            get
            {
                if (Mouse.RightButton)
                _Yaw += Mouse.XVel * 0.00005f;
                return _Yaw;
            }
        }
        public float Pitch
        {
            get
            {
                if (Mouse.RightButton)
                _Pitch += Mouse.YVel * 0.00005f;
                return _Pitch ;
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
                return ViewMatrix;
            }
        }
        public void SetCameraType()
        {
            cameraType++;
            Diagnostics.Log("Der Camera Typ ist " + _cameraType);
        }
        public void SetPositionLocally(float3 pos)
        {
           view = float4x4.LookAt(pos, pos + ForwardVector, float3.UnitY);
        }
        public float4x4 Update()
        {
            if (Keyboard.GetKey(KeyCodes.Q))
                SetCameraType();
            Position += float3.Transform(float3.UnitX * Keyboard.ADAxis * 0.2f, Quaternion.FromAxisAngle(float3.UnitY, Yaw) * Quaternion.FromAxisAngle(float3.UnitX, Pitch));
            Position += float3.Transform(float3.UnitZ * Keyboard.WSAxis * 0.2f, Quaternion.FromAxisAngle(float3.UnitY, Yaw) * Quaternion.FromAxisAngle(float3.UnitX, Pitch));
            if (_cameraType == CameraType.FREE)
            SetPositionLocally(Position);
            return view;
        }
        
    }


    [FuseeApplication(Name = "Droneflight", Description = "Droneflight Demo")]

    public class MyFirstFusee : RenderCanvas

    {

        private Camera _camera;
        private Drone _drone;
        private float4x4 view;

        // Variables init

        private const float RotationSpeed = 7;
        float i = 1;
        public SceneContainer _droneScene;
        private SceneRenderer _sceneRenderer;
        private SceneNodeContainer DroneRoot;

        private CameraType _cameraType;
        // private float4x4 mtxRot, mtxCam;
        private float MovementSpeed = 12;
        // private float3 Front, Right;
        private float4x4 Model;
        private float3 position;




        // Init is called on startup. 
        public override void Init()

        {

            // Set the clear color for the backbuffer to white (100% intensity in all color channels R, G, B, A).

            RC.ClearColor =
            new float4(0.7f, 0.9f, 0.5f, 1);



            // Load the drone model
            _droneScene = AssetStorage.Get<SceneContainer>("GroundNoMat.fus");
            var droneBody = _droneScene.Children.FindNodes(node => node.Name == "Body")?.FirstOrDefault();
            _drone = new Drone(droneBody);

            _camera = new Camera();



            // Wrap a SceneRenderer around the model.

            _sceneRenderer = new SceneRenderer(_droneScene);

            DroneRoot = _droneScene.Children.FindNodes(node => node.Name == "Body")?.FirstOrDefault();


            _camera = new Camera();

        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()

        {



            // Clear the backbuffer

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);


            // Switch between Drone and Freefly

            

                if (_cameraType == CameraType.Reset)
                    _cameraType = CameraType.FREE;

                


            var viewdrone = _drone.Update();
            var viewcam = _camera.Update();
            
            if (_cameraType == CameraType.FREE)
                view = viewcam;
            else 
                view = viewdrone;

            RC.View = viewcam;

            // Render the scene loaded in Init()

            _sceneRenderer.Render(RC);



            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.

            Present();

        }

        private InputDevice Creator(IInputDeviceImp device)

        {

            throw new NotImplementedException();

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
            aspectRatio, 0.01f,
            200.0f);

            RC.Projection = projection;

        }

    }

}
