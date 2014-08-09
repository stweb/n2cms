using System.Web.UI;
using System.Web.UI.WebControls;

namespace N2.Edit
{
    /// <summary>
    /// Base class for plugins in the navigation screen and toolbar.
    /// </summary>
    public abstract class LinkPluginAttribute : AdministrativePluginAttribute
    {
        private string target = Targets.Preview;

        public bool IsDivider { get; set; }

        /// <summary>The target frame for the plugn link.</summary>
        public string Target
        {
            get { return target; }
            set { target = value; }
        }

        /// <summary>The plugin's text.</summary>
        public string Title { get; set; }

        /// <summary>
        /// The plugin's url format. These magic strings are interpreted by the 
        /// client and inserted in the url before the frame is loaded: 
        /// {selected}, {memory}, {action}
        /// </summary>
        public string UrlFormat { get; set; }

        /// <summary>The plugin's tool tip.</summary>
        public string ToolTip { get; set; }

        /// <summary>Alternative text for the icon.</summary>
        public string AlternativeText { get; set; }

        /// <summary>Used for translating the plugin's texts from a global resource.</summary>
        public string GlobalResourceClassName { get; set; }

        public override Control AddTo(Control container, PluginContext context)
        {
            HyperLink a = AddAnchor(container, context);
            return a;
        }

        protected virtual HyperLink AddAnchor(Control container, PluginContext context)
        {
            string tooltip = Utility.GetResourceString(GlobalResourceClassName, Name + ".ToolTip") ?? ToolTip;
            string title = Utility.GetResourceString(GlobalResourceClassName, Name + ".Title") ?? Title;
            //not used string alternative = Utility.GetResourceString(GlobalResourceClassName, Name + ".AlternativeText") ?? AlternativeText;

            var a = new HyperLink
            {
                ID = "h" + Name,
                NavigateUrl = context.Rebase(context.Format(UrlFormat, true)),
                SkinID = "ToolBarLink_" + Name,
                Target = Target,
                ToolTip = tooltip,
                Text = title
            };

            a.Attributes["class"] = "templatedurl " + Name + " " + RequiredPermission.ToString() + (string.IsNullOrEmpty(IconUrl) ? "" : " iconed");
            a.Attributes["data-url-template"] = context.Rebase(UrlFormat);
            ApplyStyles(context, a);

            container.Controls.Add(a);
            return a;
        }
    }
}
