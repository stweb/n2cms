using System;
using System.Collections.Generic;
using System.Xml.XPath;
using N2.Definitions;
using N2.Engine;
using N2.Security;

namespace N2.Persistence.Serialization
{
    public interface IItemXmlReader
    {
        bool IgnoreMissingTypes { get; set; }
        IImportRecord Read(XPathNavigator navigator);
    }

    [Service]
    [Service(typeof(IItemXmlReader))]
    public class ItemXmlReader : XmlReader, IItemXmlReader
    {
        private readonly IDefinitionManager definitions;
        private readonly ContentActivator activator;
        private readonly IDictionary<string, IXmlReader> readers;

        public ItemXmlReader(IDefinitionManager definitions, ContentActivator activator)
        {
            if (definitions == null)
                throw new ArgumentNullException("definitions");

            this.definitions = definitions;
            this.activator = activator;
            this.readers = DefaultReaders();
        }

        public bool IgnoreMissingTypes { get; set; }

        private static IDictionary<string, IXmlReader> DefaultReaders()
        {
            IDictionary<string, IXmlReader> readers = new Dictionary<string, IXmlReader>();
            readers["details"] = new DetailXmlReader();
            readers["detailCollections"] = new DetailCollectionXmlReader();
            readers["authorizations"] = new AuthorizationXmlReader();
            readers["properties"] = new PersistablePropertyXmlReader();
            readers["attachments"] = new AttachmentXmlReader();
            // do via parent relation instead: readers["children"] = new ChildXmlReader();
            return readers;
        }

        public virtual IImportRecord Read(XPathNavigator navigator)
        {
            if (navigator == null) throw new ArgumentNullException("navigator");

            ReadingJournal journal = new ReadingJournal();
            foreach (XPathNavigator itemElement in EnumerateChildren(navigator))
            {
                try
                {
                    ContentItem item = ReadSingleItem(itemElement, journal);
                    journal.Report(item);
                }
                catch (DefinitionNotFoundException ex)
                {
                    journal.Error(ex);
                    if (!IgnoreMissingTypes)
                        throw;
                }
            }
            return journal;
        }

        public virtual ContentItem ReadSingleItem(XPathNavigator navigator, ReadingJournal journal)
        {
            if (navigator.LocalName != "item") throw new DeserializationException("Expected element 'item' but was '" + navigator.LocalName + "'");

            Dictionary<string, string> attributes = GetAttributes(navigator);
            ContentItem item = CreateInstance(attributes);
            ReadDefaultAttributes(attributes, item, journal);

            foreach (XPathNavigator current in EnumerateChildren(navigator))
            {
                if (readers.ContainsKey(current.LocalName))
                    readers[current.LocalName].Read(current, item, journal);
            }

            return item;
        }

        private string GetStringOrNull(Dictionary<string, string> attributes, string key)
        {
            if (attributes.ContainsKey(key))
            {
                var value = attributes[key];
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return null;
        }

        protected virtual void ReadDefaultAttributes(Dictionary<string, string> attributes, ContentItem item, ReadingJournal journal)
        {
            item.Created = ToDateTime(attributes["created"]);
            item.Expires = attributes.ContainsKey("expires") ? ToNullableDateTime(attributes["expires"]) : null;
            item.ID = Convert.ToInt32(attributes["id"]);
            item.Name = GetStringOrNull(attributes, "name");
            if (item.ID.ToString() == item.Name)
                item.Name = null;

            item.Published = attributes.ContainsKey("published") ? ToNullableDateTime(attributes["published"]) : null;
            item.SavedBy = attributes.ContainsKey("savedBy") ? attributes["savedBy"] : null;
            item.SortOrder = Convert.ToInt32(attributes["sortOrder"]);
            item.Title = attributes.ContainsKey("title") ? attributes["title"] : "";
            item.Updated = ToDateTime(attributes["updated"]);
            item.Visible = Convert.ToBoolean(attributes["visible"]);

            item.ZoneName = GetStringOrNull(attributes, "zoneName"); // empty string must get converted to null for code to work, see  PersistentContentItemList<T>.FindPages()
            item.TemplateKey = GetStringOrNull(attributes, "templateKey");
            if (attributes.ContainsKey("translationKey") && !string.IsNullOrEmpty(attributes["translationKey"]))
                item.TranslationKey = Convert.ToInt32(attributes["translationKey"]);
            if (attributes.ContainsKey("ancestralTrail"))
                item.AncestralTrail = attributes["ancestralTrail"];
            if (attributes.ContainsKey("alteredPermissions"))
                item.AlteredPermissions = (Permission)Convert.ToInt32(attributes["alteredPermissions"]);
            if (attributes.ContainsKey("childState"))
                item.ChildState = (Collections.CollectionState)Convert.ToInt32(attributes["childState"]);
            if (attributes.ContainsKey("versionIndex"))
                item.VersionIndex = Convert.ToInt32(attributes["versionIndex"]);
            if (attributes.ContainsKey("versionOf"))
            {
                item.VersionOf.ID = Convert.ToInt32(attributes["versionOf"]);
            }

            if (attributes.ContainsKey("parent"))
            {
                var parentVersionKey = attributes.ContainsKey("parentVersionKey") ? attributes["parentVersionKey"] : null;
                HandleParentRelation(item, attributes["parent"], parentVersionKey, journal);
            }

            if (attributes.ContainsKey("state") && !string.IsNullOrEmpty(attributes["state"]))
            {
                // be tolerant to read both old numeric and new textual state
                var val = attributes["state"];
                int intval;
                ContentState newstate;
                if (int.TryParse(val, out intval))
                    newstate = (ContentState)intval;
                else if (!Enum.TryParse(val, true, out newstate))
                    newstate = SerializationUtility.RecaulculateState(item);

                item.State = newstate;
            }
            else // TODO: dangerous migration code
                item.State = SerializationUtility.RecaulculateState(item);
        }

        protected virtual void HandleParentRelation(ContentItem item, string parent, string parentVersionKey, ReadingJournal journal)
        {
            int parentID = 0;
            if (int.TryParse(parent, out parentID) && parentID != 0)
            {
                ContentItem parentItem = journal.Find(parentID);
                if (parentItem != null)
                    item.AddTo(parentItem);
                else
                    journal.RegisterParentRelation(parentID, item);
            }
            if (!string.IsNullOrEmpty(parentVersionKey))
            {
                ContentItem parentItem = journal.Find(parentVersionKey);
                if (parentItem != null)
                    item.AddTo(parentItem);
                else
                    journal.Register(parentVersionKey, item.AddTo, isChild: true);
            }
        }

        private ContentItem CreateInstance(Dictionary<string, string> attributes)
        {
            ItemDefinition definition = FindDefinition(attributes);
            return activator.CreateInstance(definition.ItemType, null, null, asProxy: true, invokeBehaviors: false);
        }

        protected virtual ItemDefinition FindDefinition(Dictionary<string, string> attributes)
        {
            var discriminator = attributes.ContainsKey("discriminator") ? attributes["discriminator"] : null;
            var title = attributes.ContainsKey("title") ? attributes["title"] : null;
            var name = attributes.ContainsKey("name") ? attributes["name"] : null;
            if (discriminator != null)
            {
                var definition = definitions.GetDefinition(discriminator);
                if (definition != null)
                    return definition;
            }
            throw new DefinitionNotFoundException(string.Format("No definition found for '{0}' with name '{1}' and discriminator '{2}'", title, name, discriminator), attributes);
        }

    }
}
