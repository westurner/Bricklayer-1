﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bricklayer.Client.World
{
    /// <summary>
    /// Represents either a foreground or background tile in the map
    /// </summary>
    public class Tile
    {
        public const int Width = 16, Height = 16, DrawHeight = 20, DrawWidth = 20;
        public const int BackgroundIndex = 0, ForegroundIndex = 1;
        //Color that backgrounds shoud be darkened with
        private static Color backgroundTint = new Color(160, 160, 160); 

        /// <summary>
        /// The type of block occupying the tile
        /// </summary>
        public BlockType Block
        {
            get { return block; }
            set { block = value; }
        }
        private BlockType block;

        /// <summary>
        /// The layer (foreground or background) the block occupies
        /// </summary>
        public Layer Layer { get { return block.Layer; } }

        /// <summary>
        /// Sets or creates a new block
        /// </summary>
        public Tile(BlockType block)
        {
            Block = block;
        }
        /// <summary>
        /// Handles updating the tile's logic
        /// </summary>
        public virtual void Update(GameTime gameTime)
        {
            //No default behavior
        }
        /// <summary>
        /// Handles drawing of a single tile
        /// </summary>
        public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 drawPosition, int x, int y, int z)
        {
            //Foreground blocks
            if (z == 1)
            {
                drawPosition.X = (x * Tile.Width);
                drawPosition.Y = ((y * Tile.Height) - (Tile.DrawHeight - Tile.Height)) + 1;
                spriteBatch.Draw(texture, drawPosition, Block.Source, Color.White);
            }
            //Background blocks
            else if (z == 0)
            {
                if (Block.Layer == Layer.Background) //Draw background normally
                {
                    drawPosition.X = (x * Tile.Width);
                    drawPosition.Y = ((y * Tile.Height) - (Tile.DrawHeight - Tile.Height)) + 1;
                    spriteBatch.Draw(texture, drawPosition, Block.Source, Color.White);
                }
                else if (Block.Layer == Layer.All) //If block has foreground and background versions, calculate the background source
                {
                    Rectangle source = Block.Source;
                    source.Y += 4;
                    source.Width = Tile.Width;
                    source.Height = Tile.Height;
                    drawPosition.X = (x * Tile.Width) + 4;
                    drawPosition.Y = ((y * Tile.Height) - (Tile.DrawHeight - Tile.Height)) + 1;
                    spriteBatch.Draw(texture, drawPosition, source, backgroundTint);
                }
            }
        }
    }
}
