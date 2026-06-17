using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    public class FrameAuthoritySyncSystem : SystemBase
    {
        public override void Init()
        {
            base.Init();
            GameModule.Network.MessageReceived += OnNetworkMessageReceived;
        }

        public override void Dispose()
        {
            GameModule.Network.MessageReceived -= OnNetworkMessageReceived;
            base.Dispose();
        }

        private void OnNetworkMessageReceived(NetWorkMessage message)
        {
            StartSyncMsg startSyncMsg;
            if (FrameAuthorityMessageCodec.TryReadStartSync(message, out startSyncMsg))
            {
                ApplyStartSync(startSyncMsg);
                return;
            }

            AffirmMsg affirmMsg;
            if (FrameAuthorityMessageCodec.TryReadAffirm(message, out affirmMsg))
            {
                ApplyAffirm(affirmMsg);
                return;
            }

            PursueMsg pursueMsg;
            if (FrameAuthorityMessageCodec.TryReadPursue(message, out pursueMsg))
            {
                ApplyPursue(pursueMsg);
                return;
            }

            List<CommandComponent> commands;
            if (FrameAuthorityMessageCodec.TryReadCommands(message, out commands))
            {
                ApplyAuthorityCommands(commands);
            }
        }

        private void ApplyStartSync(StartSyncMsg msg)
        {
            m_world.FrameCount = msg.frame;
            m_world.EntityIndex = msg.createEntityIndex;
            m_world.SyncRule = msg.SyncRule;

            int interval = msg.intervalTime > 0 ? msg.intervalTime : FrameConfig.LogicFrameIntervalMs;
            GameModule.FrameSync.IntervalTime = interval;

            ConnectStatusComponent connectStatus = m_world.GetSingletonComp<ConnectStatusComponent>();
            connectStatus.aheadFrame = msg.advanceCount;

            m_world.IsStart = true;
        }

        private void ApplyAffirm(AffirmMsg msg)
        {
            ConnectStatusComponent connectStatus = m_world.GetSingletonComp<ConnectStatusComponent>();
            connectStatus.unConfirmFrame.Remove(msg.frame);
            connectStatus.rtt = ClientTime.GetTime() - msg.time;
        }

        private void ApplyAuthorityCommands(List<CommandComponent> commands)
        {
            int targetFrame = m_world.FrameCount;
            int conflictFrame = int.MaxValue;

            for (int i = 0; i < commands.Count; i++)
            {
                CommandComponent command = commands[i];
                PlayerCommandRecordComponent record = GetRecord(command.id);
                if (record == null)
                {
                    continue;
                }

                record.EnsureDefaultCommand(command.id);
                PlayerCommandBase previous = record.GetInputCache(command.frame);
                bool isHistoricalFrame = command.frame <= targetFrame;
                bool isConflict = isHistoricalFrame
                    && (previous == null || !previous.EqualsCmd(command));

                record.RecordCommand(command);
                if (isConflict)
                {
                    record.m_isConflict = true;
                    if (command.frame < conflictFrame)
                    {
                        conflictFrame = command.frame;
                    }
                }
            }

            if (conflictFrame != int.MaxValue)
            {
                RecalculateFrom(conflictFrame, targetFrame);
            }
        }

        private void ApplyPursue(PursueMsg msg)
        {
            ConnectStatusComponent connectStatus = m_world.GetSingletonComp<ConnectStatusComponent>();
            connectStatus.aheadFrame = msg.advanceCount;

            SendAffirm(msg.frame, msg.serverTime, msg.id);

            int targetFrame = msg.frame + msg.advanceCount;
            RecalculateFrom(msg.recalcFrame, targetFrame);
        }

        private PlayerCommandRecordComponent GetRecord(int entityId)
        {
            if (!m_world.GetEntityIsExist(entityId))
            {
                Log.Error($"[FrameAuthoritySyncSystem] authority command entity not found: {entityId}");
                return null;
            }

            EntityBase entity = m_world.GetEntity(entityId);
            if (!entity.GetExistComp<PlayerCommandRecordComponent>())
            {
                Log.Error($"[FrameAuthoritySyncSystem] PlayerCommandRecordComponent missing: {entityId}");
                return null;
            }
            return entity.GetComp<PlayerCommandRecordComponent>();
        }

        private void RecalculateFrom(int recalcFrame, int targetFrame)
        {
            ConnectStatusComponent connectStatus = m_world.GetSingletonComp<ConnectStatusComponent>();
            if (recalcFrame <= connectStatus.ClearFrame)
            {
                Log.Error($"[FrameAuthoritySyncSystem] recalc frame {recalcFrame} before clear frame {connectStatus.ClearFrame}");
                return;
            }

            int restoreFrame = Math.Max(recalcFrame - 1, 0);
            int finalFrame = Math.Max(targetFrame, recalcFrame);
            int interval = GameModule.FrameSync.IntervalTime;

            m_world.RevertToFrame(restoreFrame);
            m_world.ClearAfter(restoreFrame);

            while (m_world.FrameCount < finalFrame)
            {
                m_world.Record(m_world.FrameCount);
                m_world.Recalc(m_world.FrameCount + 1, interval);
            }

            m_world.EndRecalc();
        }

        private void SendAffirm(int frame, int time, int id)
        {
            if (!GameModule.Network.IsConnected)
            {
                return;
            }

            AffirmMsg msg = new AffirmMsg
            {
                frame = frame,
                time = time,
                id = id,
            };
            GameModule.Network.SendMessage(
                FrameAuthorityMessageCodec.AffirmMessageType,
                FrameAuthorityMessageCodec.CreateAffirmData(msg));
        }
    }
}
