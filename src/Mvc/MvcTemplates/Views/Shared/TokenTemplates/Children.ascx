﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<string>" %>
<% var content = Html.Content(); %>
<% using (content.BeginScope(Model)) { 
	var children = content.Traverse.Children(content.CurrentItem, content.Is.Page());
	if(children.Any()) { 
		foreach(var item in children) {%>
			<span><%= content.LinkTo(item) %></span>
		<% } 
	}
} %>