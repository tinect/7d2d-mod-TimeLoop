using ContentData = TimeLoop.Functions.XmlContentData;
using System.Collections.Generic;
using TimeLoop.Functions;
using Platform.Steam;

namespace TimeLoop.Modules
{
    public class TimeLooper
    {
        ContentData contentData;
        public Dictionary<string, ClientInfo> clients = new Dictionary<string, ClientInfo>();
        double unscaledTimeStamp;

        public TimeLooper(ContentData contentData)
        {
            this.contentData = contentData;
        }

        public void Update()
        {
            if (this.contentData.EnableTimeLooper == false)
            {
                return;
            }

            if (unscaledTimeStamp == UnityEngine.Time.unscaledTimeAsDouble)
            {
                return;
            }

            ulong worldTime = GameManager.Instance.World.GetWorldTime();
            ulong dayTime = worldTime % 24000;
            if (dayTime == 0)
            {
                if (ShouldLoop() == false)
                {
                    Log.Out("[TimeLoop] No loop.");

                    return;
                }

                Log.Out("[TimeLoop] Day loop.");
                Message.SendGlobalChat($"Looping day due to not enough players available.");

                int previousDay = GameUtils.WorldTimeToDays(worldTime) - 1;
                GameManager.Instance.World.SetTime(GameUtils.DaysToWorldTime(previousDay) + 2);
            }

            unscaledTimeStamp = UnityEngine.Time.unscaledTimeAsDouble;
        }

        public bool ShouldLoop()
        {
            if (this.contentData.EnableTimeLooper == false)
            {
                return false;
            }

            switch (this.contentData.mode)
            {
                case ContentData.Mode.WHITELIST:
                    return CheckIfAuthPlayerOnline() == false;
                case ContentData.Mode.MIN_PLAYER_COUNT:
                    return CheckIfMinPlayerCountReached() == false;
                case ContentData.Mode.MIN_WHITELIST_PLAYER_COUNT:
                    return CheckIfMinAuthPlayerCountReached() == false;
                default:
                    return false;
            }
        }

        private bool CheckIfAuthPlayerOnline()
        {
            foreach (ClientInfo client in clients.Values)
            {
                if (IsClientAuthorized(client))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckIfMinPlayerCountReached()
        {
            return clients.Count >= this.contentData.MinPlayers;
        }

        private bool CheckIfMinAuthPlayerCountReached()
        {
            int authorizedClientCount = 0;

            foreach (ClientInfo client in clients.Values)
            {
                if (IsClientAuthorized(client))
                {
                    authorizedClientCount++;
                }
            }

            return authorizedClientCount >= this.contentData.MinPlayers;
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
