﻿#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; 
#endregion

namespace Bricklayer.Client.World
{
    /// <summary>
    /// A camera object which can focus around a point and be used for drawing at a certain position, zoom, and rotation
    /// </summary>
    public class Camera
    {
        #region Properties
        /// <summary>
        /// The position of the upper left corner of the camera
        /// </summary>
        public Vector2 Position { get { return position; } set {
            position.X = (float)MathHelper.Clamp(value.X, MinBounds.X, MaxBounds.X - size.X);
            position.Y = (float)MathHelper.Clamp(value.Y, MinBounds.Y, MaxBounds.Y - size.Y);
        } }

        /// <summary>
        /// The position of the center of the camera
        /// </summary>
        public Vector2 Origin {
            get { return new Vector2(size.X / 2.0f, size.Y / 2.0f); }
            set { Position = new Vector2(value.X - size.X / 2.0f, value.Y - size.Y / 2.0f); }
        }   

        /// <summary>
        /// The current zoom factor of the camera
        /// </summary>
        public float Zoom { get; set; }

        /// <summary>
        /// The rotation, in radians, of the camera
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// The top (Y) position of the camera
        /// </summary>
        public float Top { get { return Position.Y; } }

        /// <summary>
        /// The left (X) position of the camera
        /// </summary>
        public float Left { get { return Position.X; } }

        /// <summary>
        /// The bottom bound of the camera (Y + Height)
        /// </summary>
        public float Bottom { get { return Position.Y + size.Y; } }

        /// <summary>
        /// The right bound of the camera (X + Width)
        /// </summary>
        public float Right { get { return Position.X + size.X; } }

        /// <summary>
        /// The maximum position the camera can travel to (Using the bottom right position)
        /// </summary>
        public Vector2 MaxBounds { get; set; }

        /// <summary>
        /// The minimum position the camera can travel to
        /// </summary>
        public Vector2 MinBounds { get; set; }
        #endregion

        #region Fields
        private Vector2 size;
        private Vector2 position;
        #endregion

        /// <summary>
        /// Creates a new camera with the specified size
        /// </summary>
        /// <param name="size">Ususally close to the viewport size, defines the size of the camera</param>
        public Camera(Vector2 size)
        {
            this.size = size;
            Zoom = 1.0f;
        }

        /// <summary>
        /// Get a Matrix that can be used with a spritebatch for drawing objects in the camera
        /// </summary>
        public Matrix GetViewMatrix(Vector2 parallax)
        {
            return Matrix.CreateTranslation(new Vector3(-Position * parallax, 0.0f)) *
                   Matrix.CreateTranslation(new Vector3(-Origin, 0.0f)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom, Zoom, 1) *
                   Matrix.CreateTranslation(new Vector3(Origin, 0.0f));
        }

        /// <summary>
        /// Moves the position a certain amount
        /// </summary>
        /// <param name="displacement">Amount to move</param>
        /// <param name="respectRotation">Account for the current rotation</param>
        public void Move(Vector2 displacement, bool respectRotation = false)
        {
            if (respectRotation)
            {
                displacement = Vector2.Transform(displacement, Matrix.CreateRotationZ(-Rotation));
            }

            Position += displacement;
        }

        /// <summary>
        /// Sets the position to look at a certain point, automatically factoring for the center of the camera
        /// </summary>
        public void LookAt(Vector2 position)
        {
            Position = position - Origin;
        }

        /// <summary>
        /// Transforms world coordinates to screen coordinates
        /// </summary>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, GetViewMatrix(Vector2.One));
        }

        /// <summary>
        /// Transforms screen coordinates to world coordinates
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix(Vector2.One)));
        }
    }
}
