using ContentData = TimeLoop.Functions.XmlContentData;
using System.Linq;
using TimeLoop.Modules;
using TimeLoop.Functions;
using Platform.Steam;
using UnityEngine;
using System;

namespace TimeLoop
{
    public class Main : IModApi
    {
        private TimeLooper timeLooper;
        private ContentData contentData;
        private bool? currentLoopState = null;

        public void InitMod(Mod _modInstance)
        {
            Log.Out("[TimeLoop] Initializing ...");

            ModEvents.GameAwake.RegisterHandler(Awake);
            ModEvents.GameUpdate.RegisterHandler(Update);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);
        }

        private void Awake(ref ModEvents.SGameAwakeData _data)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return;
            }

            if (this.contentData == null)
            {
                // General Initialization
                this.contentData = ContentData.DeserializeInstance();
                EnableTimeLoop.ContentData = contentData;

                // Modules
                this.timeLooper = new TimeLooper(contentData);
            }
        }

        private void Update(ref ModEvents.SGameUpdateData _data)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return;
            }

            if (contentData == null)
            {
                return;
            }

            contentData.CheckForUpdate();
            this.timeLooper?.Update();
        }

        private void PlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData _data)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return;
            }

            var cInfo = _data.ClientInfo;

            if (cInfo?.CrossplatformId == null)
            {
                return;
            }

            if (!this.timeLooper.clients.ContainsKey(cInfo.CrossplatformId.CombinedString))
            {
                this.timeLooper.clients.Add(cInfo.CrossplatformId.CombinedString, cInfo);
            }

            checkXpState();

            if (contentData.PlayerData?.Exists(
                x => (x.ID == cInfo.CrossplatformId.CombinedString)
                || (cInfo.PlatformId != null && cInfo.PlatformId is UserIdentifierSteam && x.ID == (cInfo.PlatformId as UserIdentifierSteam)?.SteamId.ToString()))
                == false)
            {
                contentData.PlayerData.Add(new TimeLoop.Functions.PlayerData(cInfo));
                contentData.SaveConfig();

                Log.Out($"[TimeLoop] Player added to config. {contentData.PlayerData.Last().ID}");
            }

            if (_data.RespawnType != RespawnType.JoinMultiplayer)
            {
                return;
            }

            if (this.currentLoopState == false)
            {
                return;
            }

            Message.SendPrivateChat(string.Format(Localization.Get("TimeLoopPlayerInfo"), cInfo.playerName), cInfo);
        }

        private void PlayerDisconnected(ref ModEvents.SPlayerDisconnectedData _data)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return;
            }

            var cInfo = _data.ClientInfo;

            if (cInfo?.CrossplatformId == null)
            {
                return;
            }

            this.timeLooper.clients.Remove(cInfo.CrossplatformId.CombinedString);

            checkXpState();
        }

        private void checkXpState()
        {
            bool shouldLoop = this.timeLooper.ShouldLoop();

            try
            {
                if (shouldLoop == this.currentLoopState)
                {
                    return;
                }

                if (shouldLoop == true)
                {
                    Message.SendGlobalChat(Localization.Get("TimeLoopLoopingDay"));

                    return;
                }

                Message.SendGlobalChat(Localization.Get("TimeLoopDisabled"));
            }
            finally
            {
                this.currentLoopState = shouldLoop;
                ensureExpBuffs();
            }
        }

        private void ensureExpBuffs()
        {
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                var hasBuff = player.Buffs.HasBuff("NoExp");
                if (this.currentLoopState == true)
                {
                    if (!hasBuff)
                    {
                        player.Buffs.AddBuff("NoExp");
                    }

                    continue;
                }

                if (hasBuff)
                {
                    player.Buffs.RemoveBuff("NoExp");
                }
            }
        }
    }
}
