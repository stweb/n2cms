using System;
using System.Collections.Generic;
using System.Text;
using N2.Integrity;
using N2.Templates.Items;

namespace N2.Templates.UI.Items
{
	[N2.Definition("Site Map", "SiteMap", "Displays all pages", "", 120)]
	[RestrictParents(typeof(IStructuralPage))]
	public class SiteMap : AbstractContentPage, IStructuralPage
	{

		public override string IconUrl
		{
			get
			{
				return "~/Img/sitemap.png";
			}
		}
		public override string TemplateUrl
		{
			get
			{
				return "~/SiteMap.aspx";
			}
		}
	}
}
