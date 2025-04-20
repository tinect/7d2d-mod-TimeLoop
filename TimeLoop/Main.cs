using ContentData = TimeLoop.Functions.XmlContentData;
using System.Linq;
using System.Text;
using TimeLoop.Modules;
using TimeLoop.Functions;
using Platform.Steam;
using System.Collections.Generic;
using System;

namespace TimeLoop
{
    public class Main : IModApi
    {
        private TimeLooper timeLooper;
        private ContentData contentData;
        private bool currentLoopState = true;

        public void InitMod(Mod _modInstance)
        {
            Log.Out("[TimeLoop] Initializing ...");
            ModEvents.GameAwake.RegisterHandler(Awake);
            ModEvents.GameUpdate.RegisterHandler(Update);
            ModEvents.PlayerLogin.RegisterHandler(PlayerLogin);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);
            //SdtdConsole.Instance.RegisterCommands();
        }

        private void Awake()
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

        private void Update()
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
            if (contentData.EnableTimeLooper)
            {
                this.timeLooper?.Update();
            }
        }

        private bool PlayerLogin(ClientInfo cInfo, string message, StringBuilder stringBuild)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return true;
            }

            if (cInfo?.CrossplatformId == null)
            {
                return true;
            }

            if (contentData?.PlayerData == null)
            {
                return true;
            }

            if (contentData.PlayerData.Exists(
                x => (x.ID == cInfo.CrossplatformId.CombinedString)
                || (cInfo.PlatformId != null && cInfo.PlatformId is UserIdentifierSteam && x.ID == (cInfo.PlatformId as UserIdentifierSteam)?.SteamId.ToString()))
                == false)
            {
                contentData.PlayerData.Add(new TimeLoop.Functions.PlayerData(cInfo));
                contentData.SaveConfig();

                Log.Out($"[TimeLoop] Player added to config. {contentData.PlayerData.Last().ID}");
            }

            bool shouldLoop = this.timeLooper?.ShouldLoop() == true;

            if (shouldLoop == true)
            {
                GameStats.Set(EnumGameStats.XPMultiplier, 0);

                Log.Out($"[TimeLoop] The time will reset every 24 hours until enough players are online.");
            }

            if (shouldLoop != this.currentLoopState)
            {
                if (shouldLoop == false)
                {
                    Message.SendGlobalChat($"[TimeLoop] disabled. Happy farming.");
                    Message.SendGlobalChat($"[TimeLoop] The current XPMultiplier is:" + GameStats.GetInt(EnumGameStats.XPMultiplier) / 100);
                }
            }

            this.currentLoopState = shouldLoop;

            return true;
        }

        private void PlayerSpawnedInWorld(ClientInfo cInfo, RespawnType type, Vector3i i)
        {
            if (type != RespawnType.JoinMultiplayer)
            {
                return;
            }

            if (this.currentLoopState == false)
            {
                return;
            }

            Message.SendPrivateChat($"[TimeLoop] The time will reset every 24 hours until enough players are online.", cInfo);
            Message.SendPrivateChat($"[TimeLoop] The current XPMultiplier is:" + GameStats.GetInt(EnumGameStats.XPMultiplier) / 100, cInfo);
        }

        private void PlayerDisconnected(ClientInfo cInfo, bool becauseShutdown)
        {
            if (GameManager.Instance == null || !GameManager.IsDedicatedServer)
            {
                return;
            }

            if (cInfo?.CrossplatformId == null)
            {
                return;
            }

            bool shouldLoop = this.timeLooper?.ShouldLoop() == true;

            if (shouldLoop == true)
            {
                GameStats.Set(EnumGameStats.XPMultiplier, GamePrefs.GetInt(EnumGamePrefs.XPMultiplier));
                Message.SendGlobalChat($"[TimeLoop] The time will reset every 24 hours until enough players are online.");
                Message.SendPrivateChat($"[TimeLoop] The current XPMultiplier is:" + (GameStats.GetInt(EnumGameStats.XPMultiplier) / 100), cInfo);
            }

            this.currentLoopState = shouldLoop;

            // TODO: Player disconnect created new party with new leader
            if (GameManager.Instance.World.Players.dict.TryGetValue(cInfo.entityId, out EntityPlayer disconnectedPlayer) && disconnectedPlayer == disconnectedPlayer.party?.Leader)
            {
                List<int> partyMembers = new List<int>();
                foreach (EntityPlayer player in disconnectedPlayer.party.MemberList)
                {
                    if (player == disconnectedPlayer) continue;

                    partyMembers.Add(player.entityId);
                }
                PartyManager.Current.CreateClientParty(GameManager.Instance.World, PartyManager.Current.nextPartyID, 0, partyMembers.ToArray(), disconnectedPlayer.party.VoiceLobbyId);
            }
        }
    }
}
