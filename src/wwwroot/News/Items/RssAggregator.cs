using N2;
using N2.Details;
using N2.Integrity;
using N2.Templates.Items;

namespace N2.Templates.News.Items
{
    [Definition("Rss List", "RssAggregatorItem", "A list of news items retrieved from an rss source.", "", 165)]
    [WithEditableTitle("Title", 10, Required = false)]
    [AllowedZones(Zones.RecursiveRight, Zones.RecursiveLeft, Zones.Right, Zones.Left, Zones.Content, Zones.ColumnLeft, Zones.ColumnRight)]
    public class RssAggregator : SidebarItem
    {
        [EditableFreeTextArea("Text", 100)]
        public virtual string Text
        {
            get { return (string)(GetDetail("Text") ?? string.Empty); }
            set { SetDetail("Text", value, string.Empty); }
        }

        [N2.Details.EditableUrl("Rss Url", 120)]
        public virtual string RssUrl
        {
            get { return (string)(GetDetail("RssUrl") ?? string.Empty); }
            set { SetDetail("RssUrl", value, string.Empty); }
        }

        [N2.Details.EditableTextBox("Max Count", 130)]
        public virtual int MaxCount
        {
            get { return (int)(GetDetail("MaxCount") ?? 5); }
            set { SetDetail("MaxCount", value, 5); }
        }

        public override string TemplateUrl
        {
            get { return "~/News/UI/RssAggregator.ascx"; }
        }
    }
}
