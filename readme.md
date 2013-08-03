# N2CMS
#### The best Content Management System (CMS) for custom ASP.NET MVC and WebForm applications.

## How do I integrate it?

###MVC Projects

**Model**

```csharp
[PageDefinition("My Page")]
public class MyPage : N2.ContentItem
{
	[EditableFreeTextArea]
	public virtual string Text { get; set; }
}
```

**View**

```html
@model MyPage

<html>
	<head>
		<title>
			@model.Title
		</title>
	</head>
	<body>
		<h1>@model.Title
		
		<p>This CMS make it so easy to publish @model.Text!</p>
	</body>
</html>
```

**Controller**

```c#
using N2.Web;


/// <summary>
/// This controller will handle pages deriving from AbstractPage which are not 
/// defined by another controller [Controls(typeof(MyPage))]. The default 
/// behavior is to render a template with this pattern:
///  * "~/Views/SharedPages/{ContentTypeName}.aspx"
/// </summary>
[Controls(typeof(MyPage))]
public class MyPage : N2.ContentController<MyPage>
{
	
}
```

###WebForm Projects

**Class**

```csharp
[PageDefinition("My Page", TemplateUrl = "~/MyPage.aspx")]
[AvailableZones("Right Column", "RightColumn")]
public class MyPage : N2.ContentItem
{
	[EditableFreeTextArea]
	public virtual string Text { get; set; }
}

```

**Page**

```html
Binds a control to the current page's text property: 
<asp:Literal Text="<%$ CurrentItem: Text %>" runat="server" />

Provides create, read, update, delete access to content through ASP.NET the databinding API:
<n2:ItemDataSource ID="Level1Items" runat="server" Path="/" />
<asp:DataGrid DataSourceID="Level1Items" runat="server" />

Renders non-page items added to the "RightColumn" zone:
<n2:Zone ZoneName="RightColumn" runat="server" />

Outputs content using the default control (a literal in this case):
<n2:Display PropertyName="Text" runat="server" />
```
###Content Management User Experience

**Main**

![Management Console](https://pbs.twimg.com/media/BPziGS2CYAAqg7S.png:large)

**Page or Part Edit**

![Page / Part Edit](http://content.screencast.com/users/brianmatic/folders/Jing/media/51addd78-38b8-4eed-b948-ddbe91c9ba98/00000002.png)

##I want this in my project.  Where do I download it?

**Install the Nuget package: http://www.nuget.org/packages/N2CMS/**

##Where do I get more advanced documentation?

###Reference Documentation
**On our Confluence wiki:** https://n2cmsdocs.atlassian.net/wiki/display/N2CMS/Getting+Started

>We know... we need to move this to public site.  We are the cobblers kids.  Would love help if you are interested.  Contact us.

###Examples

**We currently post them on CodePlex: http://n2cms.codeplex.com/releases/**.  You can also find them in the source code within this repo.

>We recommend starting with the minimal examples if you are new to N2CMS.  This release has easy to download minimal examples: http://n2cms.codeplex.com/releases/view/70951.

>You may have to make certain configuration changes when moving the code to a 
>hosting provider. Common issues are addressed here:
>http://n2cms.com/wiki/Troubleshooting-site-deployment.aspx

##Clone the Source Code and Contribute to N2CMS

##What is here?

Here you will find the N2 CMS framework and a number of template projects that 
demonstrate alternative ways to use this CMS. They all share a framework that 
consists of N2.dll and the UI management files residing below the /N2/ folder.

##How do I setup my development environment?

1. Clone this repo to your PC.
2. Double-click on Prepare_AllDependencies.bat
3. Choose amount of templates (ranging from minimal example to many features in the box):
	* Examples - Minimal C#
	* Examples - Minimal Visual Basic
	* Examples - Minimal MVC
	* Src - Dinamico
	* Src - MVC Templates
	* Src - WebForm Templates
4. Choose between N2.Everything.sln to open everything, or venture down the 
   directory structure of Src or Examples and open a solution down there.
5. Find the Visual Studio solution explorer find the web project you chose (2.), 
   right-click on it and select "Set as StartUp Project".
6. Set the a web site project as startup project (N2.Templates.* or Dinamico in src)
7. Compile and run (Ctrl+F5)


> Heads up
> N2 CMS supports many databases, this code is set up to use the SQLite embedded 
database. You may want to use SQL Server or MySQL in production.

##More Resources and Documentation

* http://n2cms.codeplex.com/releases/ (Download N2 CMS 2.x Developer Documentation)
* http://n2cms.com/Documentation.aspx
* http://google.com
* http://n2cms.codeplex.com/Thread/List.aspx
* http://n2cms.com/wiki/Project-life-cycle.aspx
* http://stackoverflow.com/questions/tagged/n2cms


##Feedback

You are very welcome to let me know about your build experiences in the 
issues so I can improve things.
