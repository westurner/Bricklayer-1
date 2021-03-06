﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Bricklayer.Common.Entities;
using Cyral.Extensions;
using Bricklayer.Common.Networking.Messages;

namespace Bricklayer.Common.Networking.Messages
{
    public class ChatMessage : IMessage
    {
        public byte ID { get; set; }

        public string Message { get; set; }

        public double MessageTime { get; set; }

        public MessageTypes MessageType
        {
            get { return MessageTypes.Chat; }
        }

        public const int MaxLength = 80;

        public ChatMessage(NetIncomingMessage im)
        {
            this.Decode(im);
        }

        public ChatMessage(Player player,string message)
        {
            this.ID = player.ID;
            this.Message = message;
            this.MessageTime = NetTime.Now;
        }

        public void Decode(NetIncomingMessage im)
        {
            this.ID = im.ReadByte();
            this.Message = im.ReadString();
            if (Message.Length > Networking.Messages.ChatMessage.MaxLength)
                Message= Message.Truncate(Networking.Messages.ChatMessage.MaxLength);
        }
        public void Encode(NetOutgoingMessage om)
        {
            om.Write(this.ID);
            om.Write(this.Message);
        }
    }
}
