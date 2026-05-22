using System;
using UnityEngine;
using GameLogic;
using GameLogic.SyncGameLogic.Component;
using GameLogic.Common.GameObjectPool;

namespace GameLogic.SyncClientLogic.System
{
    public class CreatePerfabSystem : ViewSystemBase
    {
        public override void Init()
        {
            AddEntityDestroyLisnter();
            AddEntityCreaterLisnter();
        }

        public override void Dispose()
        {
            RemoveEntityDestroyLisnter();
        }

        public override void OnEntityDestroy(EntityBase entity)
        {
            if (entity.GetExistComp<Component.PerfabComponent>())
            {
                var pc = entity.GetComp<Component.PerfabComponent>();
                if (pc.perfab != null)
                {
                    GameObjectManager.DestroyGameObjectByPool(pc.perfab);
                    pc.perfab = null;
                }
            }
        }

        public override void OnEntityCreate(EntityBase entity)
        {
            if (GetAllExistComp(new string[] { "AssetComponent", "TransformComponent" }, entity))
            {
                AddComp(entity);

                var ac = entity.GetComp<AssetComponent>();
                var tc = entity.GetComp<TransformComponent>();
                var comp = entity.GetComp<Component.PerfabComponent>();

                comp.perfab = GameObjectManager.CreateGameObject(ac.m_assetName);
                comp.hardPoint = comp.perfab.GetComponent<Component.HardPointComponent>();

                if (tc.parentID == 0)
                {
                    comp.perfab.transform.position = tc.pos.ToVector();
                    if (tc.dir.ToVector() != Vector3.zero)
                    {
                        comp.perfab.transform.forward = tc.dir.ToVector();
                    }
                }
                else
                {
                    EntityBase parent = m_world.GetEntity(tc.parentID);
                    if (parent.GetExistComp<Component.PerfabComponent>())
                    {
                        var pc = parent.GetComp<Component.PerfabComponent>();
                        comp.perfab.transform.SetParent(pc.perfab.transform);
                    }
                    comp.perfab.transform.localPosition = tc.pos.ToVector();
                    if (tc.dir.ToVector() != Vector3.zero)
                    {
                        comp.perfab.transform.forward = tc.dir.ToVector();
                    }
                }

                // 创建动画组件
                if (comp.perfab.GetComponent<Animator>() != null)
                {
                    var anc = entity.AddComp<Component.AnimComponent>();
                    anc.anim = comp.perfab.GetComponent<Animator>();
                    anc.perfab = comp.perfab;
                    anc.waistNode = comp.hardPoint?.waistNode?.gameObject;
                }
            }
        }

        void AddComp(EntityBase entity)
        {
            if (!entity.GetExistComp<Component.PerfabComponent>())
            {
                entity.AddComp<Component.PerfabComponent>();
            }
        }
    }
}
