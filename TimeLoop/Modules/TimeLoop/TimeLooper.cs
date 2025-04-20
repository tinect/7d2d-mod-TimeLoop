using ContentData = TimeLoop.Functions.XmlContentData;
using System.Linq;
using System.Collections.Generic;
using TimeLoop.Functions;
using Platform.Steam;

namespace TimeLoop.Modules
{
    public class TimeLooper
    {
        ContentData contentData;
        double unscaledTimeStamp;

        public TimeLooper(ContentData contentData)
        {
            this.contentData = contentData;
        }

        public void Update()
        {
            if (ShouldLoop() == false)
            {
                return;
            }

            if (unscaledTimeStamp != UnityEngine.Time.unscaledTimeAsDouble)
            {
                ulong worldTime = GameManager.Instance.World.GetWorldTime();
                ulong dayTime = worldTime % 24000;
                if (dayTime == 0)
                {
                    Log.Out("[TimeLoop] Time Reset.");
                    Message.SendGlobalChat($"[TimeLoop] Resetting day due to not enough players available.");

                    int previousDay = GameUtils.WorldTimeToDays(worldTime) - 1;
                    GameManager.Instance.World.SetTime(GameUtils.DaysToWorldTime(previousDay) + 2);
                }

                unscaledTimeStamp = UnityEngine.Time.unscaledTimeAsDouble;
            }
        }

        public bool ShouldLoop()
        {
            switch (this.contentData.mode)
            {
                case ContentData.Mode.WHITELIST:
                    return CheckIfAuthPlayerOnline();
                case ContentData.Mode.MIN_PLAYER_COUNT:
                    return CheckIfMinPlayerCountReached();
                case ContentData.Mode.MIN_WHITELIST_PLAYER_COUNT:
                    return CheckIfMinAuthPlayerCountReached();
                default:
                    return false;
            }
        }

        private bool CheckIfAuthPlayerOnline()
        {
            List<ClientInfo> clients = GetConnectedClients();
            for (int i = 0; i < clients.Count; i++)
            {
                if (IsClientAuthorized(clients[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckIfMinPlayerCountReached()
        {
            List<ClientInfo> clients = GetConnectedClients();
            return clients.Count >= this.contentData.MinPlayers;
        }

        private bool CheckIfMinAuthPlayerCountReached()
        {
            int authorizedClientCount = 0;

            List<ClientInfo> clients = GetConnectedClients();
            for (int i = 0; i < clients.Count; i++)
            {
                if (IsClientAuthorized(clients[i]))
                {
                    authorizedClientCount++;
                }
            }
            return authorizedClientCount >= this.contentData.MinPlayers;
        }

        private List<ClientInfo> GetConnectedClients()
        {
            if (ConnectionManager.Instance.Clients != null && ConnectionManager.Instance.Clients.Count > 0)
            {
                return ConnectionManager.Instance.Clients.List.Where(x => 
                x != null &&
                x.loginDone &&
                (x.CrossplatformId != null ||
                x.PlatformId != null)).ToList();
            }
            else
            {
                return new List<ClientInfo>();
            }
        }

        private bool IsClientAuthorized(ClientInfo cInfo)
        {
            TimeLoop.Functions.PlayerData data = this.contentData.PlayerData?.Find
                (
                x => x.ID == cInfo.CrossplatformId.CombinedString ||
                (cInfo.PlatformId is UserIdentifierSteam &&
                x.ID == (cInfo.PlatformId as UserIdentifierSteam).SteamId.ToString())
                );
            if (data?.SkipTimeLoop == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public static implicit operator bool(TimeLooper instance)
        {
            return instance != null;
        }
    }
}
