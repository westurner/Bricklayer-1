﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Bricklayer.Common.Networking;
using Bricklayer.Common.Entities;
using Bricklayer.Client.World;
using System.Collections.Generic;
using Bricklayer.Common.World;
using Map = Bricklayer.Client.World.Map;
using Bricklayer.Common.Networking.Messages;

namespace Bricklayer.Client.Entities
{
    /// <summary>
    /// Represents a player/smiley in the game map
    /// </summary>
    public class Player : Common.Entities.Player
    {
        //Physics
        private const float MoveSpeed = 35.0f; //The factor to multiply movement by
        private const float GodMoveSpeed = 10f; //The factor to multiply movement by
        private const float GroundDragFactor = 0.94f; //The amount of drag to multiply velocity by
        private const float AirDragFactor = 0.94f; //The amount of drag to multiply velocity by
        private const float MoveSlowDownFactor = .15f; //The factor to slow down the player after they have stopped
        private const float GodMoveSlowDownFactor = .1f; //The factor to slow down the player after they have stopped
        private const float MaxJumpTime = 0.23f; //The maximum amount of time a player can jump for
        private const float JumpLaunchVelocity = -3500.0f; //Velocity applied on jump to lift off
        private const float GravityAcceleration = 1000.0f;
        private const float MaxFallSpeed = 450.0f; //Maximum fall speed
        private const float JumpControlPower = 0.14f;
        private const float MaxVelocity = 700; //Maximum velocity
        private const float MaxGodVelocity = 550; //Maximum velocity

        //Input settings
        private Keys[] JumpKeys = new Keys[3] { Keys.Space, Keys.Up, Keys.W };
        private Keys[] LeftKeys = new Keys[2] { Keys.Left, Keys.A };
        private Keys[] RightKeys = new Keys[2] { Keys.Right, Keys.D };
        private Keys[] DownKeys = new Keys[2] { Keys.Down, Keys.S };

        /// <summary>
        /// Indicates if the player is the client's own
        /// </summary>
        public bool IsMine { get { return ID == Game.MyID; } }

        /// <summary>
        /// Collection of the last positions the player was, for creating a fade effect on the minimap
        /// </summary>
        public Dictionary<Point, float> LastColors = new Dictionary<Point, float>();
        private float tagAlpha = 0; //Alpha color value for nametags

        public Player(Map map, Vector2 position, string name, int id)
            : base(map, position, name, id)
        {

        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Draw godmode glow
            if (Mode == PlayerMode.God)
                spriteBatch.Draw(((Map)Map).godTexture, new Vector2((float)Math.Round(DisplayState.Position.X), (float)Math.Round(DisplayState.Position.Y) - 1) - new Vector2(((Map)Map).godTexture.Width / 2, ((Map)Map).godTexture.Height / 2) + new Vector2(Width / 2, Height / 2), Tint);
            //Draw player body
            spriteBatch.Draw(((Map)Map).bodyTexture, new Vector2((float)Math.Round(DisplayState.Position.X), (float)Math.Round(DisplayState.Position.Y) - 1), Tint);
            //Draw player smiley
            spriteBatch.Draw(((Map)Map).smileySheet, new Vector2((float)Math.Round(DisplayState.Position.X), (float)Math.Round(DisplayState.Position.Y) - 1), (Direction == FacingDirection.Left ? Smiley.LeftSource : Smiley.RightSource), Color.White);

            if (Mode == PlayerMode.Normal)
            {
                //Kinda a "hack fix", but instead of sorting tiles into layers to solve the issue of the "3D" part of the character
                //Being overlayed incorrectly, just draw the top, right, and top right tiles again
                if (((int)DisplayState.Position.Y / Tile.Height) - 1 > 0)
                    Map.Tiles[(int)DisplayState.Position.X / Tile.Width, ((int)DisplayState.Position.Y / Tile.Height) - 1, 1].Draw(spriteBatch, ((Map)Map).tileSheet, Vector2.Zero, (int)DisplayState.Position.X / Tile.Width, ((int)DisplayState.Position.Y / Tile.Height) - 1, 1, true);
                Map.Tiles[((int)DisplayState.Position.X / Tile.Width) + 1, (int)DisplayState.Position.Y / Tile.Height, 1].Draw(spriteBatch, ((Map)Map).tileSheet, Vector2.Zero, ((int)DisplayState.Position.X / Tile.Width) + 1, (int)DisplayState.Position.Y / Tile.Height, 1, true);
                if (((int)DisplayState.Position.Y / Tile.Height) - 1 > 0)
                    Map.Tiles[((int)DisplayState.Position.X / Tile.Width) + 1, ((int)DisplayState.Position.Y / Tile.Height) - 1, 1].Draw(spriteBatch, ((Map)Map).tileSheet, Vector2.Zero, ((int)DisplayState.Position.X / Tile.Width) + 1, ((int)DisplayState.Position.Y / Tile.Height) - 1, 1, true);
            }
            //Draw the player tag above them
            DrawTag(spriteBatch, elapsed);
        }
        /// <summary>
        /// Draws the players username above them
        /// </summary>
        private void DrawTag(SpriteBatch spriteBatch, float elapsed)
        {
            //Draw Tag and calculate it's color
            bool showTag = Game.Input.AnyKeysDown(Keys.LeftAlt, Keys.RightAlt);
            if (IdleTime > 1.5f)
                tagAlpha += elapsed * 2;
            else
                tagAlpha -= elapsed * 8;
            if (tagAlpha > 0 || showTag)
            {
                float alpha = showTag ? 1f : tagAlpha;
                int tagWidth = (int)Game.MainWindow.Manager.Skin.Fonts["Default8"].Resource.MeasureString(Username).X / 2;
                //Draw one white tag, and 8 shadow tags
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (!(x == 0 && y == 0))
                        {
                            spriteBatch.DrawString(Game.DefaultFont, Username, new Vector2((float)Math.Round(DisplayState.Position.X) - tagWidth + (Width / 2) + x, (float)Math.Round(DisplayState.Position.Y) - Height + y), Color.Black * .4f * alpha);
                        }
                    }
                }
                spriteBatch.DrawString(Game.DefaultFont, Username, new Vector2((float)Math.Round(DisplayState.Position.X) - tagWidth + (Width / 2), (float)Math.Round(DisplayState.Position.Y) - Height), Color.White * alpha);
            }
            tagAlpha = MathHelper.Clamp(tagAlpha, 0, 1);
        }
        /// <summary>
        /// Updates the player physics, states, etc
        /// </summary>
        public void Update(GameTime gameTime)
        {
            //Clear Input
            if (IsMine)
                IsJumping = false;
            //Make sure PreviousDisplay is one frame behind DisplayState
            PreviousState = SimulationState;
            SimulationState.Bounds = Bounds;
            //Handle Movement and Collision
            if (IsMine && ((Map)Map).Game.IsActive)
            {
                if (HandleInput())
                {
                    Game.NetManager.Send(new PlayerStateMessage(this));
                }
            }
            ApplyPhysics(gameTime);
            // Set DisplayState
            if (IsMine)
                DisplayState = SimulationState;
            else
                Interpolate(gameTime);

            DisplayState.Position.X = Math.Max(0, DisplayState.Position.X);
            DisplayState.Position.Y = Math.Max(0, DisplayState.Position.Y);
        }

        private void Interpolate(GameTime gameTime)
        {
            if (Mode == PlayerMode.Normal)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float delta = elapsed * 60;
                float difference = Math.Abs(SimulationState.Position.Y - DisplayState.Position.Y);
                if (difference <= 3)
                    DisplayState.Position.Y = SimulationState.Position.Y;
                else
                    DisplayState.Position.Y += (SimulationState.Position.Y - DisplayState.Position.Y) * delta * .5f;
                DisplayState.Position.X = SimulationState.Position.X;
            }
            else if (Mode == PlayerMode.God)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float delta = elapsed * 60;
                float difference = Vector2.Distance(SimulationState.Position, DisplayState.Position);
                if (difference <= 3)
                    DisplayState.Position = SimulationState.Position;
                else
                    DisplayState.Position += (SimulationState.Position - DisplayState.Position) * delta * .1f;
            }
        }
        /// <summary>
        /// Handles movement of the character (Jumping, Moving, etc)
        /// </summary>
        /// <returns>True if the movement has changed, false otherwise</returns>
        private bool HandleInput()
        {
            bool sendNewState = false;

            if (!(Bricklayer.Client.Interface.MainWindow.ScreenManager.Current as Bricklayer.Client.Interface.GameScreen).ChatBox.TextBox.Focused && !Game.IsMouseOnControl)
            {
                //Change smilies
                if (Game.Input.WasKeyPressed(Keys.Q))
                {
                    Smiley = SmileyType.SmileyList[((Smiley.ID == 0 ? SmileyType.SmileyList.Count : Smiley.ID) - 1) % SmileyType.SmileyList.Count];
                    Game.NetManager.Send(new PlayerSmileyMessage(this, Smiley));
                }
                else if (Game.Input.WasKeyPressed(Keys.E))
                {
                    Smiley = SmileyType.SmileyList[(Smiley.ID + 1) % SmileyType.SmileyList.Count];
                    Game.NetManager.Send(new PlayerSmileyMessage(this, Smiley));
                }

                if (Game.Input.WasKeyPressed(Keys.G))
                {
                    PlayerMode oldMode = Mode;
                    if (Mode == PlayerMode.God)
                        Mode = PlayerMode.Normal;
                    else if (Mode == PlayerMode.Normal)
                        Mode = PlayerMode.God;
                    if (oldMode != Mode)
                        Game.NetManager.Send(new PlayerModeMessage(this));
                }
            }

            if (Mode == PlayerMode.Normal)
            {
                //If player enters chat box, stop moving!
                if (Game.CurrentGameState == GameState.Game && ((Bricklayer.Client.Interface.MainWindow.ScreenManager.Current as Bricklayer.Client.Interface.GameScreen).ChatBox.TextBox.Focused || Game.IsMouseOnControl))
                {
                    if (SimulationState.Movement != Vector2.Zero)
                    {
                        SimulationState.Movement = Vector2.Zero;
                        return true;
                    }
                    if (IsJumping)
                    {
                        IsJumping = false;
                        return true;
                    }
                    return false;
                }
                //Reset
                SimulationState.Movement = Vector2.Zero;

                //If jump key pressed/released or release/pressed, send message that the velocity has changed
                //If the key is down, make sure we are jumping
                if (GravityDirection == GravityDirection.Default || GravityDirection == GravityDirection.Down || GravityDirection == GravityDirection.Up)
                    IsJumping = Game.Input.AnyKeysDown(JumpKeys);
                else if (GravityDirection == GravityDirection.Left) //Different keys for different gravity arrows
                    IsJumping = Game.Input.IsKeyDown(Keys.D) || Game.Input.IsKeyDown(Keys.Space) || Game.Input.IsKeyDown(Keys.Right);
                else if (GravityDirection == GravityDirection.Right)
                    IsJumping = Game.Input.IsKeyDown(Keys.A) || Game.Input.IsKeyDown(Keys.Space) || Game.Input.IsKeyDown(Keys.Left);

                //Move right
                if (Game.Input.AnyKeysDown(RightKeys))
                {
                    SimulationState.Movement = new Vector2(1, 0);
                    if (Game.Input.WasAllKeysUp(RightKeys))
                        sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(RightKeys))
                {
                    SimulationState.Movement = new Vector2(0, SimulationState.Movement.Y);
                    sendNewState = true;
                }


                //Move left
                if (Game.Input.AnyKeysDown(LeftKeys))
                {
                    SimulationState.Movement = new Vector2(-1, 0);
                    if (Game.Input.WasAllKeysUp(LeftKeys)) sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(LeftKeys))
                {
                    SimulationState.Movement = new Vector2(0, SimulationState.Movement.Y);
                    sendNewState = true;
                }


                if (GravityDirection == GravityDirection.Left || GravityDirection == GravityDirection.Right)
                {
                    //Move up
                    if (Game.Input.AnyKeysDown(JumpKeys))
                    {
                        SimulationState.Movement = new Vector2(0, -1);
                        if (Game.Input.WasAllKeysUp(JumpKeys)) sendNewState = true;
                    }
                    if (Game.Input.AnyKeysPressed(JumpKeys))
                    {
                        SimulationState.Movement = new Vector2(SimulationState.Movement.X, 0);
                        sendNewState = true;
                    }

                    //Move Down
                    if (Game.Input.AnyKeysDown(DownKeys))
                    {
                        SimulationState.Movement = new Vector2(0, 1);
                        if (Game.Input.WasAllKeysUp(DownKeys)) sendNewState = true;
                    }
                    if (Game.Input.AnyKeysPressed(DownKeys))
                    {
                        SimulationState.Movement = new Vector2(SimulationState.Movement.X, 0);
                        sendNewState = true;
                    }
                }

                //Fixes a bug caused by quickly switching directions
                if ((Game.Input.WasKeyDown(Keys.Left) && Game.Input.WasKeyDown(Keys.Right)) ||
                    (Game.Input.WasKeyDown(Keys.A) && Game.Input.WasKeyDown(Keys.D)))
                {
                    SimulationState.Movement = new Vector2(1, 0);
                    sendNewState = true;
                }
            }
            else if (Mode == PlayerMode.God) //Godmode flying
            {
                //If player enters chat box, stop moving!
                if (Game.CurrentGameState == GameState.Game && ((Bricklayer.Client.Interface.MainWindow.ScreenManager.Current as Bricklayer.Client.Interface.GameScreen).ChatBox.TextBox.Focused || Game.IsMouseOnControl))
                {
                    if (SimulationState.Movement != Vector2.Zero)
                    {
                        SimulationState.Movement = Vector2.Zero;
                        return true;
                    }
                    return false;
                }
                SimulationState.Movement = Vector2.Zero;

                //Move left
                if (Game.Input.AnyKeysDown(LeftKeys))
                {
                    SimulationState.Movement = new Vector2(-1, SimulationState.Movement.Y);
                    if (Game.Input.WasAllKeysUp(LeftKeys)) sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(LeftKeys))
                {
                    SimulationState.Movement = new Vector2(0, SimulationState.Movement.Y);
                    sendNewState = true;
                }
                //Move right
                if (Game.Input.AnyKeysDown(RightKeys))
                {
                    SimulationState.Movement = new Vector2(1, SimulationState.Movement.Y);
                    if (Game.Input.WasAllKeysUp(RightKeys)) sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(RightKeys))
                {
                    SimulationState.Movement = new Vector2(0, SimulationState.Movement.Y);
                    sendNewState = true;
                }
                //Move Up
                if (Game.Input.AnyKeysDown(JumpKeys))
                {
                    SimulationState.Movement = new Vector2(SimulationState.Movement.X, -1);
                    if (Game.Input.WasAllKeysUp(JumpKeys)) sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(JumpKeys))
                {
                    SimulationState.Movement = new Vector2(SimulationState.Movement.X, 0);
                    sendNewState = true;
                }
                //Move Down
                if (Game.Input.AnyKeysDown(DownKeys))
                {
                    SimulationState.Movement = new Vector2(SimulationState.Movement.X, 1);
                    if (Game.Input.WasAllKeysUp(DownKeys)) sendNewState = true;
                }
                if (Game.Input.AnyKeysPressed(DownKeys))
                {
                    SimulationState.Movement = new Vector2(SimulationState.Movement.X, 0);
                    sendNewState = true;
                }
            }
            return sendNewState;
        }
        /// <summary>
        /// Apply client-side physics to the character, calculate velocity from movement and gravity, handle collisions
        /// </summary>
        /// <param name="gameTime"></param>
        private void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds; //Time in seconds, since last frame
            float delta = elapsed * 60;

            //Face the direction the player is moving
            if (SimulationState.Movement.X > 0)
                Direction = FacingDirection.Right;
            else if (SimulationState.Movement.X < 0)
                Direction = FacingDirection.Left;

            if (Mode == PlayerMode.Normal)
            {
                if (!IsMine)
                    IsJumping = VirtualJump;
                //The next area applies the movement and gravity, based on the current direction the player is being pushed by arrows (if so)

                //If gravity is up or down
                if (GravityDirection == GravityDirection.Default || GravityDirection == GravityDirection.Down || GravityDirection == GravityDirection.Up)
                {
                    //Apply horizontal movement
                    //If not movement detected from keys, apply acceleration to move, if not, slowly stop
                    if (SimulationState.Movement.X != 0)
                        SimulationState.Velocity.X += SimulationState.Movement.X * MoveSpeed;
                    else
                        SimulationState.Velocity.X = MathHelper.Lerp(SimulationState.Velocity.X, 0, MoveSlowDownFactor);
                    SimulationState.Velocity.X = MathHelper.Clamp(SimulationState.Velocity.X, -MaxVelocity, MaxVelocity);

                    //Apply vertical movement (jump, gravity)
                    if (GravityDirection == GravityDirection.Up)
                    {
                        SimulationState.Velocity.Y = MathHelper.Clamp((SimulationState.Velocity.Y - GravityAcceleration * .0166f) - GravityAcceleration * .0166f, -MaxFallSpeed, MaxFallSpeed);
                        SimulationState.Velocity.Y = DoJump(SimulationState.Velocity.Y, gameTime);
                        SimulationState.Velocity.Y = MathHelper.Clamp((SimulationState.Velocity.Y - GravityAcceleration * .0166f), -MaxFallSpeed, MaxFallSpeed);
                    }
                    else if (GravityDirection == GravityDirection.Down)
                    {
                        SimulationState.Velocity.Y = MathHelper.Clamp(((SimulationState.Velocity.Y + (GravityAcceleration * 2.5f) * .0166f) + (GravityAcceleration * 2.5f) * .0166f), -MaxFallSpeed, MaxFallSpeed);
                        SimulationState.Velocity.Y = DoJump(SimulationState.Velocity.Y, gameTime);
                        SimulationState.Velocity.Y = MathHelper.Clamp(((SimulationState.Velocity.Y + (GravityAcceleration * 2.5f) * .0166f)), -MaxFallSpeed, MaxFallSpeed);
                    }
                    else
                    {
                        SimulationState.Velocity.Y = MathHelper.Clamp((SimulationState.Velocity.Y + GravityAcceleration * .0166f) + GravityAcceleration * .0166f, -MaxFallSpeed, MaxFallSpeed);
                        SimulationState.Velocity.Y = DoJump(SimulationState.Velocity.Y, gameTime);
                        SimulationState.Velocity.Y = MathHelper.Clamp(SimulationState.Velocity.Y + GravityAcceleration * .0166f, -MaxFallSpeed, MaxFallSpeed);
                    }
                }
                //If gravity is left or right
                if (GravityDirection == GravityDirection.Left || GravityDirection == GravityDirection.Right)
                {
                    //Apply vertical movement
                    //If not movement detected from keys, apply acceleration to move, if not, slowly stop
                    if (SimulationState.Movement.Y != 0)
                        SimulationState.Velocity.Y += SimulationState.Movement.Y * MoveSpeed;
                    else
                        SimulationState.Velocity.Y = MathHelper.Lerp(SimulationState.Velocity.Y, 0, MoveSlowDownFactor);
                    SimulationState.Velocity.Y = MathHelper.Clamp(SimulationState.Velocity.Y, -MaxVelocity, MaxVelocity);

                    //Apply vertical movement (jump, gravity)
                    if (GravityDirection == GravityDirection.Left)
                    {
                        SimulationState.Velocity.X = MathHelper.Clamp((SimulationState.Velocity.X - GravityAcceleration * .0166f) - GravityAcceleration * .0166f, -MaxFallSpeed, MaxFallSpeed);
                        SimulationState.Velocity.X = DoJump(SimulationState.Velocity.X, gameTime);
                        SimulationState.Velocity.X = MathHelper.Clamp((SimulationState.Velocity.X - GravityAcceleration * .0166f), -MaxFallSpeed, MaxFallSpeed);
                    }
                    else if (GravityDirection == GravityDirection.Right)
                    {
                        SimulationState.Velocity.X = MathHelper.Clamp(((SimulationState.Velocity.X + GravityAcceleration * .0166f) + GravityAcceleration * .0166f), -MaxFallSpeed, MaxFallSpeed);
                        SimulationState.Velocity.X = DoJump(SimulationState.Velocity.X, gameTime);
                        SimulationState.Velocity.X = MathHelper.Clamp(((SimulationState.Velocity.X + GravityAcceleration * .0166f)), -MaxFallSpeed, MaxFallSpeed);
                    }
                }


                //Apply pseudo-drag horizontally.
                if (IsOnGround)
                    SimulationState.Velocity.X *= GroundDragFactor;
                else
                    SimulationState.Velocity.X *= AirDragFactor;

                 //If gravity is up or down, apply horizontal collision, then vertical
                if (GravityDirection == GravityDirection.Default || GravityDirection == GravityDirection.Down || GravityDirection == GravityDirection.Up)
                {
                    GravityDirection = GravityDirection.Default;
                    HorizontalCollision(gameTime, elapsed); //Horizontal Collison, X Axis
                    VerticalCollision(gameTime, elapsed); //Vertical Collision, Y Axis
                }
                //If gravity is left or right, apply vertical collision, then horizontal
                else if (GravityDirection == GravityDirection.Left || GravityDirection == GravityDirection.Right)
                {
                    GravityDirection = GravityDirection.Default;
                    VerticalCollision(gameTime, elapsed); //Vertical Collision, Y Axis
                    HorizontalCollision(gameTime, elapsed); //Horizontal Collison, X Axis
                }


                //If the collision stopped us from moving, reset the velocity to zero.
                if (SimulationState.Position.X == PreviousState.Position.X)
                    SimulationState.Velocity.X = 0;
                if (SimulationState.Position.Y == PreviousState.Position.Y)
                    SimulationState.Velocity.Y = 0;
            }
            else if (Mode == PlayerMode.God)
            {
                //If not movement detected from keys, apply acceleration to move, if not, slowly stop
                if (SimulationState.Movement.X != 0)
                    SimulationState.Velocity.X += SimulationState.Movement.X * GodMoveSpeed;
                else
                    SimulationState.Velocity.X = MathHelper.Lerp(SimulationState.Velocity.X, 0, GodMoveSlowDownFactor);

                if (SimulationState.Movement.Y != 0)
                    SimulationState.Velocity.Y += SimulationState.Movement.Y * GodMoveSpeed;
                else
                    SimulationState.Velocity.Y = MathHelper.Lerp(SimulationState.Velocity.Y, 0, GodMoveSlowDownFactor);

                SimulationState.Velocity.X = MathHelper.Clamp(SimulationState.Velocity.X, -MaxGodVelocity, MaxGodVelocity);
                SimulationState.Velocity.Y = MathHelper.Clamp(SimulationState.Velocity.Y, -MaxGodVelocity, MaxGodVelocity);
                //X Axis
                SimulationState.Velocity.X *= delta;
                Vector2 change = SimulationState.Velocity.X * Vector2.UnitX * .0166f;
                change.X = MathHelper.Clamp(change.X, -10, 10);
                SimulationState.Position += change;
                SimulationState.Position = new Vector2((float)Math.Round(SimulationState.Position.X), SimulationState.Position.Y);

                //Y Axis
                SimulationState.Velocity.Y *= delta;
                change = SimulationState.Velocity.Y * Vector2.UnitY * .0166f;
                change.Y = MathHelper.Clamp(change.Y, -10, 10);
                SimulationState.Position += change;
                SimulationState.Position = new Vector2(SimulationState.Position.X, (float)Math.Round(SimulationState.Position.Y));

                //Stop velocity if player hits edge
                if (SimulationState.Position.X < Tile.Width || SimulationState.Position.X > (Map.Width * Tile.Width) - (Tile.Width * 2))
                    SimulationState.Velocity.X = 0;
                if (SimulationState.Position.Y < Tile.Height || SimulationState.Position.Y > (Map.Height * Tile.Height) - (Tile.Height * 2))
                    SimulationState.Velocity.Y = 0;
                //Clamp position in bounds
                SimulationState.Position.X = MathHelper.Clamp(SimulationState.Position.X, Tile.Width, (Map.Width * Tile.Width) - (Tile.Width * 2));
                SimulationState.Position.Y = MathHelper.Clamp(SimulationState.Position.Y, Tile.Height, (Map.Height * Tile.Height) - (Tile.Height * 2));
            }

            //Set idle states
            if (SimulationState.Position == PreviousState.Position)
                IdleTime += elapsed;
            else
                IdleTime = 0;
        }
        private void VerticalCollision(GameTime gameTime, float elapsed)
        {
            Vector2 change = SimulationState.Velocity.Y * Vector2.UnitY * elapsed;
            change.Y = MathHelper.Clamp(change.Y, -(Tile.Height), Tile.Height);
            SimulationState.Position += change;
            SimulationState.Position = new Vector2(SimulationState.Position.X, (float)Math.Round(SimulationState.Position.Y));
            HandleCollisions(CollisionDirection.Vertical, gameTime);
        }
        private void HorizontalCollision(GameTime gameTime, float elapsed)
        {
            Vector2 change = SimulationState.Velocity.X * Vector2.UnitX * elapsed;
            change.X = MathHelper.Clamp(change.X, -(Tile.Width), Tile.Width);
            SimulationState.Position += change;
            SimulationState.Position = new Vector2((float)Math.Round(SimulationState.Position.X), SimulationState.Position.Y);
            HandleCollisions(CollisionDirection.Horizontal, gameTime);
        }
        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        protected void HandleCollisions(CollisionDirection direction, GameTime gameTime)
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = Bounds;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            //Reset flag to search for ground collision.
            IsOnGround = false;


            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    Rectangle tileBounds = ((Map)Map).GetTileBounds(x, y);
                    BlockCollision collision = ((Map)Map).GetCollision(x, y);
                    Vector2 depth;
                    bool intersects = TileIntersectsPlayer(Bounds, tileBounds, direction, out depth);
                    HandleCollisions(gameTime, direction, collision, tileBounds, depth, intersects, x, y);
                }
            }
            // Save the new bounds bottom.
            PreviousState.Bounds = bounds;
        }
        /// <summary>
        /// Handles collisions for a given block
        /// </summary>
        private bool HandleCollisions(GameTime gameTime, CollisionDirection direction, BlockCollision collision, Rectangle tileBounds, Vector2 depth, bool intersects, int x, int y)
        {
            if (collision != BlockCollision.Passable && intersects)
            {
                // If we crossed the top of a tile, we are on the ground.
                if (PreviousState.Bounds.Bottom <= tileBounds.Top)
                {
                    if (collision == BlockCollision.Platform)
                    {
                        IsOnGround = true;
                    }
                }
                if (collision == BlockCollision.Gravity)
                {
                    Tile tile = Map.Tiles[x, y, 1];
                    if (tile.Block == BlockType.UpArrow)
                        GravityDirection = GravityDirection.Up;
                    else if (tile.Block == BlockType.DownArrow)
                        GravityDirection = GravityDirection.Down;
                    else if (tile.Block == BlockType.RightArrow)
                        GravityDirection = GravityDirection.Right;
                    else if (tile.Block == BlockType.LeftArrow)
                        GravityDirection = GravityDirection.Left;
                    return true;
                }
                if (collision == BlockCollision.Impassable || IsOnGround)
                {
                    //Now that we know we hit something, resolve the collison
                    if (direction == CollisionDirection.Horizontal)
                    {
                        SimulationState.Position.X += depth.X;
                        IsOnGround = true;
                    }
                    if (direction == CollisionDirection.Vertical)
                    {
                        //Cancel jump if hit something (Ie, when you jump and hit the roof)
                        IsJumping = false;
                        JumpTime = 0;
                        //Obviously hit ground or roof
                        IsOnGround = true;

                        SimulationState.Position.Y += depth.Y;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        protected float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (IsJumping)
            {
                // Begin or continue a jump
                if ((!WasJumping && IsOnGround) || JumpTime > 0.0f)
                {
                    if (JumpTime == 0)
                        JumpDirection = GravityDirection; //Set the direction this jump started from
                    if (JumpTime == 0 && IsMine)
                    {
                        //Send message we are now jumping
                        PlayerStateMessage msg = new PlayerStateMessage(this);
                        msg.IsJumping = true;
                        Game.NetManager.Send(msg);
                    }
                    JumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds; //Incriment jump timer
                }

                //If we are in the ascent of the jump
                if (0.0f < JumpTime && JumpTime <= MaxJumpTime)
                {
                    //Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = (JumpDirection == GravityDirection.Up || JumpDirection == GravityDirection.Left ? -1 : 1) * (JumpLaunchVelocity * (1.0f - (float)Math.Pow(JumpTime / MaxJumpTime, JumpControlPower)));
                }
                else
                {
                    if (JumpTime > 0 && IsMine)
                    {
                        //Tell others we are falling now
                        PlayerStateMessage msg = new PlayerStateMessage(this);
                        msg.IsJumping = false;
                        Game.NetManager.Send(msg);
                    }
                    JumpTime = 0.0f;   // Reached the apex of the jump
                    IsJumping = false;
                }
            }
            else
            {
                //Tell others we have landed
                if (JumpTime > 0 && IsMine)
                    Game.NetManager.Send(new PlayerStateMessage(this));
                // Continues not jumping or cancels a jump in progress
                JumpTime = 0.0f;
            }
            WasJumping = IsJumping;

            return velocityY;
        }
        /// <summary>
        /// Checks for tile intersections/collision depth between a player and a tile
        /// </summary>
        /// <param name="player">A player's bounding rectangle</param>
        /// <param name="block">A block's brounding rectangle</param>
        /// <param name="direction">Collision direction</param>
        /// <param name="depth">Returned depth of the collision</param>
        /// <returns>If the tile intersects the player</returns>
        public static bool TileIntersectsPlayer(Rectangle player, Rectangle block, CollisionDirection direction, out Vector2 depth)
        {
            depth = direction == CollisionDirection.Vertical ? new Vector2(0, player.GetVerticalIntersectionDepth(block)) : new Vector2(player.GetHorizontalIntersectionDepth(block), 0);
            return depth.Y != 0 || depth.X != 0;
        }
    }
}
