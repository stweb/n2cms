using System;
using N2.Engine;
using N2.Persistence;
using N2.Web;
using N2.Persistence.Sources;

namespace N2.Edit
{
    [Service]
    public class Navigator
    {
        private readonly IPersister persister;
        private readonly IHost host;
        private readonly VirtualNodeFactory virtualNodes;
        private readonly ContentSource sources;

        public Navigator(IPersister persister, IHost host, VirtualNodeFactory nodes, ContentSource sources)
        {
            this.persister = persister;
            this.host = host;
            this.virtualNodes = nodes;
            this.sources = sources;
        }

        public virtual ContentItem Navigate(ContentItem startingPoint, string path)
        {
            return startingPoint.GetChild(path)
                ?? sources.ResolvePath(startingPoint, path).CurrentItem
                ?? virtualNodes.Get(startingPoint.Path + path.TrimStart('/'))
                ?? virtualNodes.Get(path);
        }

        public virtual ContentItem Navigate(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path.StartsWith("/")) 
                return Navigate(persister.Get(host.CurrentSite.RootItemID), path);
            
            if (path.StartsWith("~"))
            {
                return Navigate(persister.Get(host.CurrentSite.StartPageID), path.Substring(1))
                       ?? sources.ResolvePath(path).CurrentItem
                       ?? virtualNodes.Get(path);
            }

            if (path.Equals("undefined")) // typical for bad JavaScript URL rendering 
                return null;

            throw new ArgumentException("The path must start with a slash '/', was '" + path + "'", "path");
        }
    }
}
