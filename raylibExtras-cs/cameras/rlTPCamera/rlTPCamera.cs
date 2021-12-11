
using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_CsLo;
using raylibExtras;

using static Raylib_CsLo.Raylib;
using static Raylib_CsLo.RayMath;
using static Raylib_CsLo.RlGl;


/**********************************************************************************************
*
*   raylibExtras * Utilities and Shared Components for Raylib
*
*   TPOrbitCamera * Third Person Camera Example
*
*   LICENSE: MIT
*
*   Copyright (c) 2021 Jeffery Myers
*
*   Permission is hereby granted, free of charge, to any person obtaining a copy
*   of this software and associated documentation files (the "Software"), to deal
*   in the Software without restriction, including without limitation the rights
*   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
*   copies of the Software, and to permit persons to whom the Software is
*   furnished to do so, subject to the following conditions:
*
*   The above copyright notice and this permission notice shall be included in all
*   copies or substantial portions of the Software.
*
*   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
*   SOFTWARE.
*
**********************************************************************************************/


//#include "rlTPCamera.h"
//#include "raylib.h"
//#include "rlgl.h"
//#include <stdlib.h>
//#include <math.h>

namespace raylibExtras
{
	public class rlTPCamera
	{
		public enum CameraControls
		{
			MOVE_FRONT = 0,
			MOVE_BACK,
			MOVE_RIGHT,
			MOVE_LEFT,
			MOVE_UP,
			MOVE_DOWN,
			TURN_LEFT,
			TURN_RIGHT,
			TURN_UP,
			TURN_DOWN,
			SPRINT,
		}


		// the speed in units/second to move 
		// X = sidestep
		// Y = jump/fall
		// Z = forward
		Vector3 MoveSpeed;

		// the speed for turning when using keys to look
		// degrees/second
		Vector2 TurnSpeed;

		// use the mouse for looking?
		bool UseMouse;
		int UseMouseButton;

		// how many pixels equate out to an angle move, larger numbers mean slower, more accurate mouse
		float MouseSensitivity;

		// how far down can the camera look
		float MinimumViewY;

		// how far up can the camera look
		float MaximumViewY;

		// the position of the base of the camera (on the floor)
		// note that this will not be the view position because it is offset by the eye height.
		// this value is also not changed by the view bobble
		public Vector3 CameraPosition;

		// how far from the target position to the camera's view point (the zoom)
		float CameraPullbackDistance;

		// the Raylib camera to pass to raylib view functions.
		Camera3D ViewCamera;

		//// the vector in the ground plane that the camera is facing
		//Vector3 ViewForward;

		// the field of view in X and Y
		Vector2 FOV;

		// state for mouse movement
		Vector2 PreviousMousePosition;

		// state for view angles
		Vector2 ViewAngles;

		// state for window focus
		bool Focused;

		//clipping planes
		// note must use BeginMode3D and EndMode3D on the camera object for clipping planes to work
		double NearPlane = 0.01;
		double FarPlane = 1000;


		public Dictionary<CameraControls, KeyboardKey> ControlsKeys = new();

		static void ResizeTPOrbitCameraView(rlTPCamera camera)
		{
			if (camera == null)
				return;

			float width = (float)GetScreenWidth();
			float height = (float)GetScreenHeight();

			camera.FOV.Y = camera.ViewCamera.fovy;

			if (height != 0)
				camera.FOV.X = camera.FOV.Y * (width / height);
		}

		//rlTPCamera() : ControlsKeys{ 'W', 'S', 'D', 'A', 'E', 'Q', KEY_LEFT, KEY_RIGHT, KEY_UP, KEY_DOWN, KEY_LEFT_SHIFT
		//	}
		//{

		//}
		public rlTPCamera()
		{
			ControlsKeys.Add(CameraControls.MOVE_FRONT, KeyboardKey.KEY_W);
			ControlsKeys.Add(CameraControls.MOVE_BACK, KeyboardKey.KEY_S);
			ControlsKeys.Add(CameraControls.MOVE_LEFT, KeyboardKey.KEY_A);
			ControlsKeys.Add(CameraControls.MOVE_RIGHT, KeyboardKey.KEY_D);
			ControlsKeys.Add(CameraControls.MOVE_UP, KeyboardKey.KEY_E);
			ControlsKeys.Add(CameraControls.MOVE_DOWN, KeyboardKey.KEY_Q);

			ControlsKeys.Add(CameraControls.TURN_LEFT, KeyboardKey.KEY_LEFT);
			ControlsKeys.Add(CameraControls.TURN_RIGHT, KeyboardKey.KEY_RIGHT);
			ControlsKeys.Add(CameraControls.TURN_UP, KeyboardKey.KEY_UP);
			ControlsKeys.Add(CameraControls.TURN_DOWN, KeyboardKey.KEY_DOWN);
			ControlsKeys.Add(CameraControls.SPRINT, KeyboardKey.KEY_LEFT_SHIFT);

			//PreviousMousePosition = Raylib.GetMousePosition();
		}

		public void Setup(float fovY, Vector3 position)
		{
			MoveSpeed = new Vector3(3, 3, 3);
			TurnSpeed = new Vector2(90, 90);

			MouseSensitivity = 600;

			MinimumViewY = 1.0f;
			MaximumViewY = 89.0f;

			PreviousMousePosition = GetMousePosition();
			Focused = IsWindowFocused();

			CameraPullbackDistance = 5;

			ViewAngles = new Vector2(0, 0);

			CameraPosition = position;
			FOV.Y = fovY;

			ViewCamera.target = position;
			ViewCamera.position = Vector3.Add(ViewCamera.target, new Vector3(0, 0, CameraPullbackDistance));
			ViewCamera.up = new Vector3(0.0f, 1.0f, 0.0f);
			ViewCamera.fovy = fovY;
			ViewCamera.projection_ = CameraProjection.CAMERA_PERSPECTIVE;

			NearPlane = 0.01;
			FarPlane = 1000.0;

			ResizeTPOrbitCameraView(this);
			SetUseMouse(true, 1);
		}

		void SetUseMouse(bool useMouse, int button)
		{
			UseMouse = useMouse;
			UseMouseButton = button;

			bool showCursor = !useMouse || button >= 0;

			if (!showCursor && IsWindowFocused())
				DisableCursor();
			else if (showCursor && IsWindowFocused())
				EnableCursor();
		}

		float GetSpeedForAxis(CameraControls axis, float speed)
		{
			var key = ControlsKeys[axis];
			if (key == KeyboardKey.KEY_NULL)
				return 0;

			float factor = 1.0f;
			if (IsKeyDown(ControlsKeys[CameraControls.SPRINT]))
				factor = 2;

			if (IsKeyDown(ControlsKeys[axis]))
				return speed * GetFrameTime() * factor;

			return 0.0f;
		}

		float GetFOVX()
		{
			return FOV.X;
		}

		Vector3 GetCameraPosition()
		{
			return CameraPosition;
		}

		void SetCameraPosition(Vector3 pos)
		{
			CameraPosition = pos;
			Vector3 forward = Vector3.Subtract(ViewCamera.target, ViewCamera.position);
			ViewCamera.position = CameraPosition;
			ViewCamera.target = Vector3.Add(CameraPosition, forward);
		}

		Ray GetViewRay()
		{
			return new Ray(ViewCamera.position, GetForwardVector());
		}

		Vector3 GetForwardVector()
		{
			return Vector3.Normalize(Vector3.Subtract(ViewCamera.target, ViewCamera.position));
		}

		//Vector3 GetFowardGroundVector()
		//{
		//	return ViewForward;
		//}

		public void Update()
		{
			if (IsWindowResized())
				ResizeTPOrbitCameraView(this);

			bool showCursor = !UseMouse || UseMouseButton >= 0;

			if (IsWindowFocused() != Focused && !showCursor)
			{
				Focused = IsWindowFocused();
				if (Focused)
				{
					DisableCursor();
					PreviousMousePosition = GetMousePosition(); // so there is no jump on focus
				}
				else
				{
					EnableCursor();
				}
			}

			// Mouse movement detection
			Vector2 mousePositionDelta = GetMouseDelta();
			float mouseWheelMove = GetMouseWheelMove();

			// Keys input detection
			float[] direction = new float[]{ -GetSpeedForAxis(CameraControls.MOVE_FRONT,MoveSpeed.Z),
									  -GetSpeedForAxis(CameraControls.MOVE_BACK,MoveSpeed.Z),
									  GetSpeedForAxis(CameraControls.MOVE_RIGHT,MoveSpeed.X),
									  GetSpeedForAxis(CameraControls.MOVE_LEFT,MoveSpeed.X),
									  GetSpeedForAxis(CameraControls.MOVE_UP,MoveSpeed.Y),
									  GetSpeedForAxis(CameraControls.MOVE_DOWN,MoveSpeed.Y) };


			bool useMouse = UseMouse && (UseMouseButton < 0 || IsMouseButtonDown(UseMouseButton));

			float turnRotation = GetSpeedForAxis(CameraControls.TURN_RIGHT, TurnSpeed.X) - GetSpeedForAxis(CameraControls.TURN_LEFT, TurnSpeed.X);
			float tiltRotation = GetSpeedForAxis(CameraControls.TURN_UP, TurnSpeed.Y) - GetSpeedForAxis(CameraControls.TURN_DOWN, TurnSpeed.Y);

			if (turnRotation != 0)
				ViewAngles.X -= turnRotation * RayMath.DEG2RAD;
			else if (useMouse && Focused)
				ViewAngles.X -= (mousePositionDelta.X / -MouseSensitivity);

			if (tiltRotation != 0)
				ViewAngles.Y += tiltRotation * RayMath.DEG2RAD;
			else if (useMouse && Focused)
				ViewAngles.Y += (mousePositionDelta.Y / -MouseSensitivity);

			// Angle clamp
			if (ViewAngles.Y < MinimumViewY * RayMath.DEG2RAD)
				ViewAngles.Y = MinimumViewY * RayMath.DEG2RAD;
			else if (ViewAngles.Y > MaximumViewY * RayMath.DEG2RAD)
				ViewAngles.Y = MaximumViewY * RayMath.DEG2RAD;

			//movement in plane rotation space
			Vector3 moveVec = new(0, 0, 0);
			moveVec.Z = direction[(int)CameraControls.MOVE_FRONT] - direction[(int)CameraControls.MOVE_BACK];
			moveVec.X = direction[(int)CameraControls.MOVE_RIGHT] - direction[(int)CameraControls.MOVE_LEFT];

			// update zoom
			CameraPullbackDistance += GetMouseWheelMove();
			if (CameraPullbackDistance < 1)
				CameraPullbackDistance = 1;

			// vector we are going to transform to get the camera offset from the target point
			Vector3 camPos = new(0, 0, CameraPullbackDistance);

			Matrix4x4 tiltMat = MatrixRotateX(ViewAngles.Y); // a matrix for the tilt rotation
			Matrix4x4 rotMat = MatrixRotateY(ViewAngles.X); // a matrix for the plane rotation
			Matrix4x4 mat = MatrixMultiply(tiltMat, rotMat); // the combined transformation matrix for the camera position

			camPos = Vector3Transform(camPos, mat); // transform the camera position into a vector in world space
			moveVec = Vector3Transform(moveVec, rotMat); // transform the movement vector into world space, but ignore the tilt so it is in plane

			CameraPosition = Vector3Add(CameraPosition, moveVec); // move the target to the moved position

			// validate cam pos here

			// set the view camera
			ViewCamera.target = CameraPosition;
			ViewCamera.position = Vector3Add(CameraPosition, camPos); // offset the camera position by the vector from the target position
		}

		static void SetupCamera(rlTPCamera camera, float aspect)
		{
			rlDrawRenderBatchActive();          // Draw Buffers (Only OpenGL 3+ and ES2)
			rlMatrixMode(RL_PROJECTION);        // Switch to projection matrix
			rlPushMatrix();                     // Save previous matrix, which contains the settings for the 2d ortho projection
			rlLoadIdentity();                   // Reset current matrix (projection)

			if (camera.ViewCamera.projection_ == CameraProjection.CAMERA_PERSPECTIVE)
			{
				// Setup perspective projection
				double top = RL_CULL_DISTANCE_NEAR * Math.Tan(camera.ViewCamera.fovy * 0.5 * RayMath.DEG2RAD);
				double right = top * aspect;

				rlFrustum(-right, right, -top, top, camera.NearPlane, camera.FarPlane);
			}
			else if (camera.ViewCamera.projection_ == CameraProjection.CAMERA_ORTHOGRAPHIC)
			{
				// Setup orthographic projection
				double top = camera.ViewCamera.fovy / 2.0;
				double right = top * aspect;

				rlOrtho(-right, right, -top, top, camera.NearPlane, camera.FarPlane);
			}

			// NOTE: zNear and zFar values are important when computing depth buffer values

			rlMatrixMode(RL_MODELVIEW);         // Switch back to modelview matrix
			rlLoadIdentity();                   // Reset current matrix (modelview)

			// Setup Camera view
			Matrix4x4 matView = MatrixLookAt(camera.ViewCamera.position, camera.ViewCamera.target, camera.ViewCamera.up);
			rlMultMatrixf((matView));      // Multiply modelview matrix by view matrix (camera)

			rlEnableDepthTest();                // Enable DEPTH_TEST for 3D
		}

		// start drawing using the camera, with near/far plane support
		public void BeginMode3D()
		{
			float aspect = (float)GetScreenWidth() / (float)GetScreenHeight();
			SetupCamera(this, aspect);
		}

		// end drawing with the camera
		public void EndMode3D()
		{
			Raylib.EndMode3D();
		}
	}
}