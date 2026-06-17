using System;
using System.Collections.Generic;

namespace GameLogic
{
    public static class FrameAuthorityMessageCodec
    {
        public const string StartSyncMessageType = "startsyncmsg";
        public const string CommandMessageType = "commandmsg";
        public const string CommandComponentMessageType = "commandcomponent";
        public const string PursueMessageType = "pursuemsg";
        public const string AffirmMessageType = "affirmmsg";

        public static bool TryReadStartSync(NetWorkMessage message, out StartSyncMsg result)
        {
            result = null;
            if (!IsMessage(message, StartSyncMessageType))
            {
                return false;
            }

            Dictionary<string, object> data = message.m_data;
            result = new StartSyncMsg
            {
                frame = GetInt(data, "frame"),
                advanceCount = GetInt(data, "advancecount"),
                intervalTime = GetInt(data, "intervaltime"),
                createEntityIndex = GetInt(data, "createentityindex"),
                SyncRule = (SyncRule)GetInt(data, "syncrule"),
            };
            return true;
        }

        public static bool TryReadPursue(NetWorkMessage message, out PursueMsg result)
        {
            result = null;
            if (!IsMessage(message, PursueMessageType))
            {
                return false;
            }

            Dictionary<string, object> data = message.m_data;
            result = new PursueMsg
            {
                id = GetInt(data, "id"),
                recalcFrame = GetInt(data, "recalcframe"),
                frame = GetInt(data, "frame"),
                advanceCount = GetInt(data, "advancecount"),
                serverTime = GetInt(data, "servertime"),
            };
            return true;
        }

        public static bool TryReadAffirm(NetWorkMessage message, out AffirmMsg result)
        {
            result = null;
            if (!IsMessage(message, AffirmMessageType))
            {
                return false;
            }

            Dictionary<string, object> data = message.m_data;
            result = new AffirmMsg
            {
                frame = GetInt(data, "frame"),
                time = GetInt(data, "time"),
                id = GetInt(data, "id"),
            };
            return true;
        }

        public static bool TryReadCommands(NetWorkMessage message, out List<CommandComponent> result)
        {
            result = new List<CommandComponent>();
            if (message == null || message.m_data == null)
            {
                return false;
            }

            if (IsMessage(message, CommandComponentMessageType))
            {
                result.Add(ReadCommand(message.m_data));
                return true;
            }

            if (!IsMessage(message, CommandMessageType))
            {
                return false;
            }

            object rawList;
            if (!TryGetValue(message.m_data, "msg", out rawList))
            {
                return true;
            }

            List<Dictionary<string, object>> typedList = rawList as List<Dictionary<string, object>>;
            if (typedList != null)
            {
                for (int i = 0; i < typedList.Count; i++)
                {
                    result.Add(ReadCommand(typedList[i]));
                }
                return true;
            }

            List<object> objectList = rawList as List<object>;
            if (objectList != null)
            {
                for (int i = 0; i < objectList.Count; i++)
                {
                    Dictionary<string, object> item = objectList[i] as Dictionary<string, object>;
                    if (item != null)
                    {
                        result.Add(ReadCommand(item));
                    }
                }
            }
            return true;
        }

        public static void SendCommand(CommandComponent command)
        {
            GameModule.Network.SendMessage(CommandComponentMessageType, CreateCommandData(command));
        }

        public static Dictionary<string, object> CreateCommandData(CommandComponent command)
        {
            return new Dictionary<string, object>
            {
                { "movedir", CreateVectorData(command.moveDir) },
                { "skilldir", CreateVectorData(command.skillDir) },
                { "jump", command.jump },
                { "togglelock", command.toggleLock },
                { "platformjump", command.platformJump },
                { "speedgear", command.speedGear },
                { "element1", command.element1 },
                { "element2", command.element2 },
                { "isfire", command.isFire },
                { "id", command.id },
                { "frame", command.frame },
                { "time", command.time },
            };
        }

        public static Dictionary<string, object> CreateCommandMsgData(int frame, int serverTime, IList<CommandComponent> commands)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < commands.Count; i++)
            {
                list.Add(CreateCommandInfoData(commands[i]));
            }

            return new Dictionary<string, object>
            {
                { "frame", frame },
                { "servertime", serverTime },
                { "msg", list },
            };
        }

        public static Dictionary<string, object> CreateAffirmData(AffirmMsg msg)
        {
            return new Dictionary<string, object>
            {
                { "frame", msg.frame },
                { "time", msg.time },
                { "id", msg.id },
            };
        }

        private static Dictionary<string, object> CreateCommandInfoData(CommandComponent command)
        {
            Dictionary<string, object> data = CreateCommandData(command);
            data.Remove("time");
            return data;
        }

        private static CommandComponent ReadCommand(Dictionary<string, object> data)
        {
            CommandComponent command = new CommandComponent
            {
                id = GetInt(data, "id"),
                frame = GetInt(data, "frame"),
                time = GetInt(data, "time", 0),
                moveDir = ReadVector(data, "movedir"),
                skillDir = ReadVector(data, "skilldir"),
                jump = GetBool(data, "jump"),
                toggleLock = GetBool(data, "togglelock"),
                platformJump = GetBool(data, "platformjump"),
                speedGear = GetInt(data, "speedgear", 1),
                element1 = GetInt(data, "element1"),
                element2 = GetInt(data, "element2"),
                isFire = GetBool(data, "isfire"),
            };
            return command;
        }

        private static Dictionary<string, object> CreateVectorData(SyncVector3 value)
        {
            return new Dictionary<string, object>
            {
                { "x", value.x },
                { "y", value.y },
                { "z", value.z },
            };
        }

        private static SyncVector3 ReadVector(Dictionary<string, object> data, string key)
        {
            Dictionary<string, object> vectorData = GetDict(data, key);
            if (vectorData == null)
            {
                return SyncVector3.Zero;
            }

            return new SyncVector3
            {
                x = GetInt(vectorData, "x"),
                y = GetInt(vectorData, "y"),
                z = GetInt(vectorData, "z"),
            };
        }

        private static Dictionary<string, object> GetDict(Dictionary<string, object> data, string key)
        {
            object value;
            if (!TryGetValue(data, key, out value))
            {
                return null;
            }
            return value as Dictionary<string, object>;
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue = 0)
        {
            object value;
            if (!TryGetValue(data, key, out value) || value == null)
            {
                return defaultValue;
            }

            if (value is int)
            {
                return (int)value;
            }
            if (value is long)
            {
                return (int)(long)value;
            }
            if (value is short)
            {
                return (short)value;
            }
            if (value is byte)
            {
                return (byte)value;
            }
            return Convert.ToInt32(value);
        }

        private static bool GetBool(Dictionary<string, object> data, string key, bool defaultValue = false)
        {
            object value;
            if (!TryGetValue(data, key, out value) || value == null)
            {
                return defaultValue;
            }

            if (value is bool)
            {
                return (bool)value;
            }
            return Convert.ToBoolean(value);
        }

        private static bool TryGetValue(Dictionary<string, object> data, string key, out object value)
        {
            if (data.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (KeyValuePair<string, object> pair in data)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool IsMessage(NetWorkMessage message, string messageType)
        {
            return message != null
                && string.Equals(message.m_MessageType, messageType, StringComparison.OrdinalIgnoreCase);
        }
    }
}
