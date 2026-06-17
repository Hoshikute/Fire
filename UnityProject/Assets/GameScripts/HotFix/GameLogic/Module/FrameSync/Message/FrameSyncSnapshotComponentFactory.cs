using System;
using Newtonsoft.Json.Linq;

namespace GameLogic
{
    public static class FrameSyncSnapshotComponentFactory
    {
        public static bool TryCreateComponent(
            ComponentInfo info,
            int entityId,
            out ComponentBase component,
            out bool isSelf,
            out bool isTheir,
            out string error)
        {
            component = null;
            isSelf = false;
            isTheir = false;
            error = null;

            if (info == null || string.IsNullOrEmpty(info.m_compName))
            {
                error = "component info missing name";
                return false;
            }

            string name = info.m_compName;
            if (string.Equals(name, nameof(SelfComponent), StringComparison.OrdinalIgnoreCase))
            {
                component = new SelfComponent();
                isSelf = true;
                return true;
            }

            if (string.Equals(name, nameof(TheirComponent), StringComparison.OrdinalIgnoreCase))
            {
                component = new TheirComponent();
                isTheir = true;
                return true;
            }

            try
            {
                JObject json = string.IsNullOrWhiteSpace(info.content)
                    ? new JObject()
                    : JObject.Parse(info.content);

                if (string.Equals(name, nameof(PlayerComponent), StringComparison.OrdinalIgnoreCase))
                {
                    component = ReadPlayerComponent(json, entityId);
                    return true;
                }

                if (string.Equals(name, nameof(PlayerMoveComponent), StringComparison.OrdinalIgnoreCase))
                {
                    component = ReadPlayerMoveComponent(json);
                    return true;
                }

                if (string.Equals(name, nameof(PlayerStateComponent), StringComparison.OrdinalIgnoreCase))
                {
                    component = ReadPlayerStateComponent(json);
                    return true;
                }

                if (string.Equals(name, nameof(CommandComponent), StringComparison.OrdinalIgnoreCase))
                {
                    component = ReadCommandComponent(json, entityId);
                    return true;
                }

                error = "unknown shared component: " + name;
                return false;
            }
            catch (Exception ex)
            {
                error = "deserialize component " + name + " failed: " + ex.Message;
                return false;
            }
        }

        private static PlayerComponent ReadPlayerComponent(JObject json, int entityId)
        {
            return new PlayerComponent
            {
                playerId = ReadInt(json, "playerId", ReadInt(json, "playerid", entityId)),
                playerName = ReadString(json, "playerName", ReadString(json, "playername", string.Empty)),
                isLocal = false,
            };
        }

        private static PlayerMoveComponent ReadPlayerMoveComponent(JObject json)
        {
            PlayerMoveComponent component = new PlayerMoveComponent();
            component.ID = ReadInt(json, "ID", ReadInt(json, "id", component.ID));
            component.Frame = ReadInt(json, "Frame", ReadInt(json, "frame", component.Frame));
            component.pos = ReadVector(json, "pos", component.pos);
            component.faceDir = ReadVector(json, "faceDir", ReadVector(json, "facedir", component.faceDir));
            component.moveIntentDir = ReadVector(json, "moveIntentDir", ReadVector(json, "moveintentdir", component.moveIntentDir));
            component.speedGear = ReadInt(json, "speedGear", ReadInt(json, "speedgear", component.speedGear));
            component.moveSpeed = ReadInt(json, "moveSpeed", ReadInt(json, "movespeed", component.moveSpeed));
            component.verticalSpeed = ReadInt(json, "verticalSpeed", ReadInt(json, "verticalspeed", component.verticalSpeed));
            component.isOnGround = ReadBool(json, "isOnGround", ReadBool(json, "isonground", component.isOnGround));
            component.capsuleRadius = ReadInt(json, "capsuleRadius", ReadInt(json, "capsuleradius", component.capsuleRadius));
            component.capsuleHeight = ReadInt(json, "capsuleHeight", ReadInt(json, "capsuleheight", component.capsuleHeight));
            component.currentSpeed = ReadInt(json, "currentSpeed", ReadInt(json, "currentspeed", component.currentSpeed));
            return component;
        }

        private static PlayerStateComponent ReadPlayerStateComponent(JObject json)
        {
            PlayerStateComponent component = new PlayerStateComponent();
            component.ID = ReadInt(json, "ID", ReadInt(json, "id", component.ID));
            component.Frame = ReadInt(json, "Frame", ReadInt(json, "frame", component.Frame));
            component.state = ReadEnum(json, "state", component.state);
            component.framesInState = ReadInt(json, "framesInState", ReadInt(json, "framesinstate", component.framesInState));
            component.prevState = ReadEnum(json, "prevState", ReadEnum(json, "prevstate", component.prevState));
            component.isLocked = ReadBool(json, "isLocked", ReadBool(json, "islocked", component.isLocked));
            component.platformJumpRequested = ReadBool(json, "platformJumpRequested", ReadBool(json, "platformjumprequested", component.platformJumpRequested));
            component.wallObstructType = ReadInt(json, "wallObstructType", ReadInt(json, "wallobstructtype", component.wallObstructType));
            component.isInPlaceJump = ReadBool(json, "isInPlaceJump", ReadBool(json, "isinplacejump", component.isInPlaceJump));
            return component;
        }

        private static CommandComponent ReadCommandComponent(JObject json, int entityId)
        {
            CommandComponent component = new CommandComponent();
            component.id = ReadInt(json, "id", entityId);
            component.frame = ReadInt(json, "frame", component.frame);
            component.time = ReadInt(json, "time", component.time);
            component.moveDir = ReadVector(json, "moveDir", ReadVector(json, "movedir", component.moveDir));
            component.skillDir = ReadVector(json, "skillDir", ReadVector(json, "skilldir", component.skillDir));
            component.jump = ReadBool(json, "jump", component.jump);
            component.toggleLock = ReadBool(json, "toggleLock", ReadBool(json, "togglelock", component.toggleLock));
            component.platformJump = ReadBool(json, "platformJump", ReadBool(json, "platformjump", component.platformJump));
            component.speedGear = ReadInt(json, "speedGear", ReadInt(json, "speedgear", component.speedGear));
            component.element1 = ReadInt(json, "element1", component.element1);
            component.element2 = ReadInt(json, "element2", component.element2);
            component.isFire = ReadBool(json, "isFire", ReadBool(json, "isfire", component.isFire));
            return component;
        }

        private static SyncVector3 ReadVector(JObject json, string field, SyncVector3 defaultValue)
        {
            JToken token = GetToken(json, field);
            JObject vector = token as JObject;
            if (vector == null)
            {
                return defaultValue;
            }

            return SyncVector3.FromRaw(
                ReadInt(vector, "x", defaultValue.x),
                ReadInt(vector, "y", defaultValue.y),
                ReadInt(vector, "z", defaultValue.z));
        }

        private static int ReadInt(JObject json, string field, int defaultValue)
        {
            JToken token = GetToken(json, field);
            if (token == null || token.Type == JTokenType.Null)
            {
                return defaultValue;
            }

            return token.Value<int>();
        }

        private static bool ReadBool(JObject json, string field, bool defaultValue)
        {
            JToken token = GetToken(json, field);
            if (token == null || token.Type == JTokenType.Null)
            {
                return defaultValue;
            }

            return token.Value<bool>();
        }

        private static string ReadString(JObject json, string field, string defaultValue)
        {
            JToken token = GetToken(json, field);
            if (token == null || token.Type == JTokenType.Null)
            {
                return defaultValue;
            }

            return token.Value<string>();
        }

        private static PlayerLogicState ReadEnum(JObject json, string field, PlayerLogicState defaultValue)
        {
            JToken token = GetToken(json, field);
            if (token == null || token.Type == JTokenType.Null)
            {
                return defaultValue;
            }

            if (token.Type == JTokenType.String)
            {
                PlayerLogicState parsed;
                return Enum.TryParse(token.Value<string>(), true, out parsed) ? parsed : defaultValue;
            }

            return (PlayerLogicState)token.Value<int>();
        }

        private static JToken GetToken(JObject json, string field)
        {
            JToken token;
            if (json.TryGetValue(field, StringComparison.OrdinalIgnoreCase, out token))
            {
                return token;
            }
            return null;
        }
    }
}
