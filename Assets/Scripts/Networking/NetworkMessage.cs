using System;
using System.IO;
using System.Text;

namespace Impostor.Networking
{
    /// <summary>
    /// Base class for all network messages. Provides serialization functionality.
    /// </summary>
    public abstract class NetworkMessage
    {
        public enum MessageType : byte
        {
            PlayerJoined = 1,
            PlayerLeft = 2,
            GameStateUpdate = 3,
            WordAssigned = 4,
            ClueSubmitted = 5,
            VoteSubmitted = 6,
            RoundStart = 7,
            RoundEnd = 8,
            GameEnd = 9,
            ReadyState = 10,
            PlayerAction = 11,
            DraftAcknowledged = 12
        }

        public abstract MessageType Type { get; }

        public virtual byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte)Type);
                SerializeData(writer);
                return stream.ToArray();
            }
        }

        public static NetworkMessage Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                MessageType type = (MessageType)reader.ReadByte();
                return DeserializeMessage(type, reader);
            }
        }

        protected abstract void SerializeData(BinaryWriter writer);
        protected abstract void DeserializeData(BinaryReader reader);

        private static NetworkMessage DeserializeMessage(MessageType type, BinaryReader reader)
        {
            NetworkMessage message = null;

            switch (type)
            {
                case MessageType.PlayerJoined:
                    message = new PlayerJoinedMessage();
                    break;
                case MessageType.PlayerLeft:
                    message = new PlayerLeftMessage();
                    break;
                case MessageType.GameStateUpdate:
                    message = new GameStateUpdateMessage();
                    break;
                case MessageType.WordAssigned:
                    message = new WordAssignedMessage();
                    break;
                case MessageType.ClueSubmitted:
                    message = new ClueSubmittedMessage();
                    break;
                case MessageType.VoteSubmitted:
                    message = new VoteSubmittedMessage();
                    break;
                case MessageType.RoundStart:
                    message = new RoundStartMessage();
                    break;
                case MessageType.RoundEnd:
                    message = new RoundEndMessage();
                    break;
                case MessageType.GameEnd:
                    message = new GameEndMessage();
                    break;
                case MessageType.ReadyState:
                    message = new ReadyStateMessage();
                    break;
                case MessageType.PlayerAction:
                    message = new PlayerActionMessage();
                    break;
                case MessageType.DraftAcknowledged:
                    message = new DraftAcknowledgedMessage();
                    break;
            }

            if (message != null)
            {
                message.DeserializeData(reader);
            }

            return message;
        }

        protected static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length <= 0) return string.Empty;
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        protected static void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0);
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }

    // Message implementations
    public class PlayerJoinedMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerJoined;
        public ulong PlayerSteamID { get; set; }
        public string PlayerName { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
            WriteString(writer, PlayerName);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
            PlayerName = ReadString(reader);
        }
    }

    public class PlayerLeftMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerLeft;
        public ulong PlayerSteamID { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
        }
    }

    public class GameStateUpdateMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.GameStateUpdate;
        public int GameState { get; set; }
        public byte[] StateData { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(GameState);
            if (StateData != null)
            {
                writer.Write(StateData.Length);
                writer.Write(StateData);
            }
            else
            {
                writer.Write(0);
            }
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            GameState = reader.ReadInt32();
            int length = reader.ReadInt32();
            if (length > 0)
            {
                StateData = reader.ReadBytes(length);
            }
        }
    }

    public class WordAssignedMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.WordAssigned;
        public ulong PlayerSteamID { get; set; }
        public string Word { get; set; }
        public bool IsImpostor { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
            WriteString(writer, Word);
            writer.Write(IsImpostor);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
            Word = ReadString(reader);
            IsImpostor = reader.ReadBoolean();
        }
    }

    public class ClueSubmittedMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ClueSubmitted;
        public ulong PlayerSteamID { get; set; }
        public string Clue { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
            WriteString(writer, Clue);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
            Clue = ReadString(reader);
        }
    }

    public class VoteSubmittedMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.VoteSubmitted;
        public ulong VoterSteamID { get; set; }
        public ulong VotedForSteamID { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(VoterSteamID);
            writer.Write(VotedForSteamID);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            VoterSteamID = reader.ReadUInt64();
            VotedForSteamID = reader.ReadUInt64();
        }
    }

    public class RoundStartMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.RoundStart;
        public int RoundNumber { get; set; }
        public string SecretWord { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(RoundNumber);
            WriteString(writer, SecretWord);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            RoundNumber = reader.ReadInt32();
            SecretWord = ReadString(reader);
        }
    }

    public class RoundEndMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.RoundEnd;
        public ulong VotedOutSteamID { get; set; }
        public bool WasImpostor { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(VotedOutSteamID);
            writer.Write(WasImpostor);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            VotedOutSteamID = reader.ReadUInt64();
            WasImpostor = reader.ReadBoolean();
        }
    }

    public class GameEndMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.GameEnd;
        public bool ImpostorsWon { get; set; }
        public ulong[] ImpostorSteamIDs { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(ImpostorsWon);
            if (ImpostorSteamIDs != null)
            {
                writer.Write(ImpostorSteamIDs.Length);
                foreach (ulong id in ImpostorSteamIDs)
                {
                    writer.Write(id);
                }
            }
            else
            {
                writer.Write(0);
            }
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            ImpostorsWon = reader.ReadBoolean();
            int count = reader.ReadInt32();
            if (count > 0)
            {
                ImpostorSteamIDs = new ulong[count];
                for (int i = 0; i < count; i++)
                {
                    ImpostorSteamIDs[i] = reader.ReadUInt64();
                }
            }
        }
    }

    public class ReadyStateMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ReadyState;
        public ulong PlayerSteamID { get; set; }
        public bool IsReady { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
            writer.Write(IsReady);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
            IsReady = reader.ReadBoolean();
        }
    }

    public class PlayerActionMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerAction;
        public ulong PlayerSteamID { get; set; }
        public string Action { get; set; }
        public byte[] ActionData { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
            WriteString(writer, Action);
            if (ActionData != null)
            {
                writer.Write(ActionData.Length);
                writer.Write(ActionData);
            }
            else
            {
                writer.Write(0);
            }
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
            Action = ReadString(reader);
            int length = reader.ReadInt32();
            if (length > 0)
            {
                ActionData = reader.ReadBytes(length);
            }
        }
    }

    public class DraftAcknowledgedMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.DraftAcknowledged;
        public ulong PlayerSteamID { get; set; }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerSteamID);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerSteamID = reader.ReadUInt64();
        }
    }
}

