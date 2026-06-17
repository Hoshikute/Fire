using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GameLogic
{
    public static class FrameAuthorityMessageCodec
    {
        public const string StartSyncMessageType = "startsyncmsg";
        public const string CommandMessageType = "commandmsg";
        public const string CommandComponentMessageType = "commandcomponent";
        public const string PursueMessageType = "pursuemsg";
        public const string AffirmMessageType = "affirmmsg";
        public const string SyncEntityMessageType = "syncentitymsg";
        public const string ChangeSingletonComponentMessageType = "changesingletoncomponentmsg";

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

            List<Dictionary<string, object>> typedList = ToDictionaryList(rawList);
            if (typedList != null)
            {
                for (int i = 0; i < typedList.Count; i++)
                {
                    result.Add(ReadCommand(typedList[i]));
                }
                return true;
            }

            return true;
        }

        public static bool TryReadSyncEntity(NetWorkMessage message, out SyncEntityMsg result)
        {
            result = null;
            if (!IsMessage(message, SyncEntityMessageType))
            {
                return false;
            }

            Dictionary<string, object> data = message.m_data;
            result = new SyncEntityMsg
            {
                frame = GetInt(data, "frame"),
                snapshotId = GetInt(data, "snapshotid"),
                snapshotFrame = GetInt(data, "snapshotframe"),
                selfEntityId = GetInt(data, "selfentityid"),
                createEntityIndex = GetInt(data, "createentityindex"),
                intervalTime = GetInt(data, "intervaltime"),
                advanceCount = GetInt(data, "advancecount"),
                isSnapshot = GetBool(data, "issnapshot"),
                isSnapshotComplete = GetBool(data, "issnapshotcomplete"),
                infos = ReadEntityInfos(GetValueOrNull(data, "infos")),
                destroyList = ReadIntList(GetValueOrNull(data, "destroylist")),
            };
            return true;
        }

        public static bool TryReadChangeSingleton(NetWorkMessage message, out ChangeSingletonComponentMsg result)
        {
            result = null;
            if (!IsMessage(message, ChangeSingletonComponentMessageType))
            {
                return false;
            }

            Dictionary<string, object> data = message.m_data;
            result = new ChangeSingletonComponentMsg
            {
                frame = GetInt(data, "frame"),
                info = ReadComponentInfo(GetValueOrNull(data, "info")),
            };
            return true;
        }

        public static void SendCommand(CommandComponent command)
        {
            GameModule.Network.SendMessage(CommandComponentMessageType, CreateCommandData(command));
        }

        public static void SendSnapshotAck(int snapshotFrame, int selfEntityId)
        {
            AffirmMsg msg = new AffirmMsg
            {
                frame = snapshotFrame,
                time = ClientTime.GetTime(),
                id = selfEntityId,
            };
            GameModule.Network.SendMessage(AffirmMessageType, CreateAffirmData(msg));
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

        private static List<EntityInfo> ReadEntityInfos(object raw)
        {
            List<EntityInfo> result = new List<EntityInfo>();
            List<Dictionary<string, object>> list = ToDictionaryList(raw);
            if (list == null)
            {
                return result;
            }

            for (int i = 0; i < list.Count; i++)
            {
                Dictionary<string, object> data = list[i];
                EntityInfo info = new EntityInfo
                {
                    id = GetInt(data, "id"),
                    infos = ReadComponentInfos(GetValueOrNull(data, "infos")),
                };
                result.Add(info);
            }
            return result;
        }

        private static List<ComponentInfo> ReadComponentInfos(object raw)
        {
            List<ComponentInfo> result = new List<ComponentInfo>();
            List<Dictionary<string, object>> list = ToDictionaryList(raw);
            if (list == null)
            {
                return result;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ComponentInfo info = ReadComponentInfo(list[i]);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        private static ComponentInfo ReadComponentInfo(object raw)
        {
            Dictionary<string, object> data = ToDictionary(raw);
            if (data == null)
            {
                return null;
            }

            return new ComponentInfo
            {
                m_compName = GetString(data, "m_compname", GetString(data, "m_compName")),
                content = GetString(data, "content"),
            };
        }

        private static List<int> ReadIntList(object raw)
        {
            List<int> result = new List<int>();
            if (raw == null)
            {
                return result;
            }

            JArray jArray = raw as JArray;
            if (jArray != null)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    result.Add(jArray[i].Value<int>());
                }
                return result;
            }

            List<object> objectList = raw as List<object>;
            if (objectList != null)
            {
                for (int i = 0; i < objectList.Count; i++)
                {
                    result.Add(Convert.ToInt32(UnwrapJsonValue(objectList[i])));
                }
                return result;
            }

            List<int> intList = raw as List<int>;
            if (intList != null)
            {
                result.AddRange(intList);
            }
            return result;
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
            return ToDictionary(value);
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue = 0)
        {
            object value;
            if (!TryGetValue(data, key, out value) || value == null)
            {
                return defaultValue;
            }

            value = UnwrapJsonValue(value);

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

            value = UnwrapJsonValue(value);

            if (value is bool)
            {
                return (bool)value;
            }
            return Convert.ToBoolean(value);
        }

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue = "")
        {
            object value;
            if (!TryGetValue(data, key, out value) || value == null)
            {
                return defaultValue;
            }

            value = UnwrapJsonValue(value);
            return value == null ? defaultValue : value.ToString();
        }

        private static object GetValueOrNull(Dictionary<string, object> data, string key)
        {
            object value;
            return TryGetValue(data, key, out value) ? value : null;
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

        private static Dictionary<string, object> ToDictionary(object raw)
        {
            if (raw == null)
            {
                return null;
            }

            Dictionary<string, object> dict = raw as Dictionary<string, object>;
            if (dict != null)
            {
                return dict;
            }

            JObject jObject = raw as JObject;
            if (jObject != null)
            {
                return jObject.ToObject<Dictionary<string, object>>();
            }

            return null;
        }

        private static List<Dictionary<string, object>> ToDictionaryList(object raw)
        {
            if (raw == null)
            {
                return null;
            }

            List<Dictionary<string, object>> typedList = raw as List<Dictionary<string, object>>;
            if (typedList != null)
            {
                return typedList;
            }

            JArray jArray = raw as JArray;
            if (jArray != null)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                for (int i = 0; i < jArray.Count; i++)
                {
                    JObject item = jArray[i] as JObject;
                    if (item != null)
                    {
                        result.Add(item.ToObject<Dictionary<string, object>>());
                    }
                }
                return result;
            }

            List<object> objectList = raw as List<object>;
            if (objectList != null)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                for (int i = 0; i < objectList.Count; i++)
                {
                    Dictionary<string, object> item = ToDictionary(objectList[i]);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
                return result;
            }

            return null;
        }

        private static object UnwrapJsonValue(object value)
        {
            JValue jValue = value as JValue;
            if (jValue != null)
            {
                return jValue.Value;
            }

            return value;
        }

        private static bool IsMessage(NetWorkMessage message, string messageType)
        {
            return message != null
                && string.Equals(message.m_MessageType, messageType, StringComparison.OrdinalIgnoreCase);
        }
    }
}
