using System.Linq;
using N2.Management.Api;
using N2.Engine;
using System;

namespace N2.Security
{
    [Service(typeof(IProfileRepository), Replaces = typeof(InMemoryProfileRepository))]
    public class ContentProfileRepository : IProfileRepository
    {
        private readonly ItemBridge bridge;

        public ContentProfileRepository(ItemBridge bridge)
        {
            this.bridge = bridge;
        }

        public ProfileUser Get(string username)
        {
            var user = bridge.GetUser(username);
            if (user == null)
                return null;

            var profile = new ProfileUser { Name = username, Email = user.Email };

            var clientSettings = user.DetailCollections["Settings"];
            foreach (var setting in clientSettings.Details.Where(setting => setting.Meta != null))
            {
                profile.Settings[setting.Meta] = setting.Value;
            }
            return profile;
        }

        public void Save(ProfileUser profile)
        {
            var user = bridge.GetUser(profile.Name);
            if (user == null)
            {
                user = bridge.CreateUser(profile.Name, Guid.NewGuid().ToString(), profile.Email, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), false, profile.Name);
                user.IsLogin = false;
            }

            user.IsProfile = true;
            var clientSettings = user.DetailCollections["Settings"];
            clientSettings.Replace(profile.Settings);
            
            bridge.Save(user);
        }
    }
}
